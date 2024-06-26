﻿using L2Dn.AuthServer.Model;
using L2Dn.AuthServer.Network.OutgoingPackets;
using L2Dn.AuthServer.NetworkGameServer.OutgoingPacket;
using L2Dn.Network;
using L2Dn.Packets;

namespace L2Dn.AuthServer.Network.IncomingPackets;

internal struct RequestServerLoginPacket: IIncomingPacket<AuthSession>
{
    private int _loginKey1;
    private int _loginKey2;
    private byte _serverId;

    public void ReadContent(PacketBitReader reader)
    {
        _loginKey1 = reader.ReadInt32();
        _loginKey2 = reader.ReadInt32();
        _serverId = reader.ReadByte();
    }

    public async ValueTask ProcessAsync(Connection connection, AuthSession session)
    {
        AccountInfo? accountInfo = session.AccountInfo;
        
        byte serverId = _serverId;
        GameServerInfo? serverInfo = GameServerManager.Instance.GetServerInfo(serverId);
        if (_loginKey1 != session.LoginKey1 || _loginKey2 != session.LoginKey2 || accountInfo is null || 
            serverInfo is null || !serverInfo.IsOnline)
        {
            PlayFailPacket loginFailPacket = new(PlayFailReason.AccessFailed);
            connection.Send(ref loginFailPacket, SendPacketOptions.CloseAfterSending);
            return;
        }

        if (serverId != accountInfo.LastServerId)
        {
            accountInfo.LastServerId = serverInfo.ServerId;
            await AccountManager.Instance.UpdateSelectedGameServerAsync(accountInfo.AccountId, serverId);
        }
        
        if (serverInfo.PlayerCount >= serverInfo.MaxPlayerCount)
        {
            PlayFailPacket loginFailPacket = new(PlayFailReason.TooManyPlayers);
            connection.Send(ref loginFailPacket, SendPacketOptions.CloseAfterSending);
            return;
        }
        
        Connection? serverConnection = GameServerManager.Instance.GetServerInfo(serverId)?.Connection;
        if (serverConnection is null || serverConnection.Closed)
        {
            PlayFailPacket loginFailPacket = new(PlayFailReason.AccessFailed);
            connection.Send(ref loginFailPacket, SendPacketOptions.CloseAfterSending);
            return;
        }

        // Sending login data to the game server
        LoginRequestPacket loginRequestPacket = new(accountInfo.AccountId, accountInfo.Login, session.LoginKey1,
            session.LoginKey2, session.PlayKey1, session.PlayKey2);
        
        serverConnection.Send(ref loginRequestPacket);

        PlayOkPacket playOkPacket = new(session.PlayKey1, session.PlayKey2);
        connection.Send(ref playOkPacket, SendPacketOptions.CloseAfterSending);
    }
}
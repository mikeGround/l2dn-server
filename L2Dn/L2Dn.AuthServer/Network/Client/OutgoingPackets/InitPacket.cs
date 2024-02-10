﻿using L2Dn.Packets;

namespace L2Dn.AuthServer.Network.Client.OutgoingPackets;

/// <summary>
/// 0x00 - Init
/// </summary>
internal readonly struct InitPacket(int sessionId, byte[] scrambledModulus, byte[] blowfishKey): IOutgoingPacket
{
    public void WriteContent(PacketBitWriter writer)
    {
        //const uint protocolRevision = 0x785a;
        const uint protocolRevision = 0xc621;

        writer.WriteByte(0x00); // packet code
        writer.WriteInt32(sessionId);
        writer.WriteUInt32(protocolRevision);
        writer.WriteBytes(scrambledModulus); // RSA Public Key

        // unk GG related?
        writer.WriteUInt32(0x29DD954E);
        writer.WriteUInt32(0x77C39CFC);
        writer.WriteUInt32(0x97ADB620);
        writer.WriteUInt32(0x07BDE0F7);

        writer.WriteBytes(blowfishKey); // BlowFish key
        writer.WriteByte(0x00); // null termination ;)
    }
}

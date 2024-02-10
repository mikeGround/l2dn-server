﻿using L2Dn.GameServer.Model.Actor;

namespace L2Dn.GameServer.Model.Clans.Entries;

public class PledgeWaitingInfo
{
    private int _playerId;
    private int _playerClassId;
    private int _playerLvl;
    private readonly int _karma;
    private String _playerName;

    public PledgeWaitingInfo(int playerId, int playerLvl, int karma, int classId, String playerName)
    {
        _playerId = playerId;
        _playerClassId = classId;
        _playerLvl = playerLvl;
        _karma = karma;
        _playerName = playerName;
    }

    public int getPlayerId()
    {
        return _playerId;
    }

    public void setPlayerId(int playerId)
    {
        _playerId = playerId;
    }

    public int getPlayerClassId()
    {
        if (isOnline() && (getPlayer().getBaseClass() != _playerClassId))
        {
            _playerClassId = getPlayer().getClassId().getId();
        }

        return _playerClassId;
    }

    public int getPlayerLvl()
    {
        if (isOnline() && (getPlayer().getLevel() != _playerLvl))
        {
            _playerLvl = getPlayer().getLevel();
        }

        return _playerLvl;
    }

    public int getKarma()
    {
        return _karma;
    }

    public String getPlayerName()
    {
        if (isOnline() && !getPlayer().getName().equalsIgnoreCase(_playerName))
        {
            _playerName = getPlayer().getName();
        }

        return _playerName;
    }

    public Player getPlayer()
    {
        return World.getInstance().getPlayer(_playerId);
    }

    public bool isOnline()
    {
        return (getPlayer() != null) && (getPlayer().isOnlineInt() > 0);
    }
}

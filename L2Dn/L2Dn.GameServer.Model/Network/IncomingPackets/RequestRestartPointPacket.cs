﻿using System.Collections.Immutable;
using L2Dn.GameServer.Data.Xml;
using L2Dn.GameServer.Enums;
using L2Dn.GameServer.InstanceManagers;
using L2Dn.GameServer.Model;
using L2Dn.GameServer.Model.Actor;
using L2Dn.GameServer.Model.Clans;
using L2Dn.GameServer.Model.Events.Impl.Players;
using L2Dn.GameServer.Model.Holders;
using L2Dn.GameServer.Model.InstanceZones;
using L2Dn.GameServer.Model.Items.Instances;
using L2Dn.GameServer.Model.Residences;
using L2Dn.GameServer.Model.Sieges;
using L2Dn.GameServer.Model.Skills;
using L2Dn.GameServer.Model.Stats;
using L2Dn.GameServer.Model.Variables;
using L2Dn.GameServer.Network.Enums;
using L2Dn.GameServer.Utilities;
using L2Dn.Geometry;
using L2Dn.Model.Enums;
using L2Dn.Network;
using L2Dn.Packets;
using ThreadPool = L2Dn.GameServer.Utilities.ThreadPool;

namespace L2Dn.GameServer.Network.IncomingPackets;

public struct RequestRestartPointPacket: IIncomingPacket<GameSession>
{
    private int _requestedPointType;
    private bool _continuation;
    private int _resItemID;
    private int _resCount;

    public void ReadContent(PacketBitReader reader)
    {
        _requestedPointType = reader.ReadInt32();
        if (reader.Length >= 8)
        {
            _resItemID = reader.ReadInt32();
            _resCount = reader.ReadInt32();
        }
    }

    public ValueTask ProcessAsync(Connection connection, GameSession session)
    {
		Player? player = session.Player;
		if (player == null)
			return ValueTask.CompletedTask;
		
		if (!player.canRevive())
			return ValueTask.CompletedTask;
		
		if (player.isFakeDeath())
		{
			player.stopFakeDeath(true);
			return ValueTask.CompletedTask;
		}
		
		if (!player.isDead())
			return ValueTask.CompletedTask;
		
		// Custom event resurrection management.
		if (player.isOnEvent() && player.Events.HasSubscribers<OnPlayerResurrectRequest>())
		{
			if (player.Events.Notify(new OnPlayerResurrectRequest(player)))
				return ValueTask.CompletedTask;
		}
		
		Castle castle = CastleManager.getInstance().getCastle(player.getX(), player.getY(), player.getZ());
		if ((castle != null) && castle.getSiege().isInProgress() && (player.getClan() != null) && castle.getSiege().checkIsAttacker(player.getClan()))
		{
			// Schedule respawn delay for attacker
			ThreadPool.schedule(new DeathTask(player, _requestedPointType, _resItemID), castle.getSiege().getAttackerRespawnDelay());
			if (castle.getSiege().getAttackerRespawnDelay() > 0)
			{
				player.sendMessage("You will be re-spawned in " + (castle.getSiege().getAttackerRespawnDelay() / 1000) + " seconds");
			}
			return ValueTask.CompletedTask;
		}
		
		portPlayer(player, _requestedPointType, _resItemID);
		return ValueTask.CompletedTask;
	}
	
	private static void portPlayer(Player player, int requestedPointType, int resItemID)
	{
		Location? loc = null;
		Instance instance = null;
		
		// force jail
		if (player.isJailed())
		{
			requestedPointType = 27;
		}
		
		switch (requestedPointType)
		{
			case 1: // to clanhall
			{
				if ((player.getClan() == null) || (player.getClan().getHideoutId() == 0))
				{
					PacketLogger.Instance.Warn("Player [" + player.getName() + "] called RestartPointPacket - To Clanhall and he doesn't have Clanhall!");
					return;
				}
                
				loc = MapRegionManager.getInstance().getTeleToLocation(player, TeleportWhereType.CLANHALL);
				ClanHall residence = ClanHallData.getInstance().getClanHallByClan(player.getClan());
				if ((residence != null) && (residence.hasFunction(ResidenceFunctionType.EXP_RESTORE)))
				{
					player.restoreExp(residence.getFunction(ResidenceFunctionType.EXP_RESTORE).getValue());
				}
				
				break;
			}
			case 2: // to castle
			{
				Clan clan = player.getClan();
				Castle castle = CastleManager.getInstance().getCastle(player);
				if ((castle != null) && castle.getSiege().isInProgress())
				{
					// Siege in progress
					if (castle.getSiege().checkIsDefender(clan))
					{
						loc = MapRegionManager.getInstance().getTeleToLocation(player, TeleportWhereType.CASTLE);
					}
					else if (castle.getSiege().checkIsAttacker(clan))
					{
						loc = MapRegionManager.getInstance().getTeleToLocation(player, TeleportWhereType.TOWN);
					}
					else
					{
						PacketLogger.Instance.Warn("Player [" + player.getName() +
						                           "] called RestartPointPacket - To Castle and he doesn't have Castle!");
						return;
					}
				}
				else
				{
					if ((clan == null) || (clan.getCastleId() == 0))
					{
						return;
					}
					loc = MapRegionManager.getInstance().getTeleToLocation(player, TeleportWhereType.CASTLE);
				}
				
				if (clan != null)
				{
					castle = CastleManager.getInstance().getCastleByOwner(clan);
					if (castle != null)
					{
						Castle.CastleFunction castleFunction = castle.getCastleFunction(Castle.FUNC_RESTORE_EXP);
						if (castleFunction != null)
						{
							player.restoreExp(castleFunction.getLvl());
						}
					}
				}
				
				break;
			}
			case 3: // to fortress
			{
				Clan clan = player.getClan();
				if ((clan == null) || (clan.getFortId() == 0))
				{
					PacketLogger.Instance.Warn("Player [" + player.getName() + "] called RestartPointPacket - To Fortress and he doesn't have Fortress!");
					return;
				}
				
				loc = MapRegionManager.getInstance().getTeleToLocation(player, TeleportWhereType.FORTRESS);
				
				Fort fort = FortManager.getInstance().getFortByOwner(clan);
				if (fort != null)
				{
					Fort.FortFunction fortFunction = fort.getFortFunction(Fort.FUNC_RESTORE_EXP);
					if (fortFunction != null)
					{
						player.restoreExp(fortFunction.getLevel());
					}
				}
				break;
			}
			case 4: // to siege HQ
			{
				SiegeClan siegeClan = null;
				Castle castle = CastleManager.getInstance().getCastle(player);
				Fort fort = FortManager.getInstance().getFort(player);
				if ((castle != null) && castle.getSiege().isInProgress())
				{
					siegeClan = castle.getSiege().getAttackerClan(player.getClan());
				}
				else if ((fort != null) && fort.getSiege().isInProgress())
				{
					siegeClan = fort.getSiege().getAttackerClan(player.getClan());
				}
				
				if (((siegeClan == null) || siegeClan.getFlag().isEmpty()))
				{
					PacketLogger.Instance.Warn("Player [" + player.getName() + "] called RestartPointPacket - To Siege HQ and he doesn't have Siege HQ!");
					return;
				}
				loc = MapRegionManager.getInstance().getTeleToLocation(player, TeleportWhereType.SIEGEFLAG);
				break;
			}
			case 5: // Fixed or Player is a festival participant
			{
				if (!player.isGM() && !player.getInventory().haveItemForSelfResurrection())
				{
					PacketLogger.Instance.Warn("Player [" + player.getName() + "] called RestartPointPacket - Fixed and he isn't festival participant!");
					return;
				}
				
				if (player.isGM())
				{
					player.doRevive(100);
				}
				else if (player.destroyItemByItemId("Feather", 10649, 1, player, false) /* || player.destroyItemByItemId("Feather", 13300, 1, player, false) || player.destroyItemByItemId("Feather", 13128, 1, player, false) */)
				{
					player.doRevive(100);
					CommonSkill.FEATHER_OF_BLESSING.getSkill().applyEffects(player, player);
				}
				else
				{
					instance = player.getInstanceWorld();
					loc = player.Location;
				}
				break;
			}
			case 6: // TODO: Agathion resurrection
			{
				break;
			}
			case 7: // TODO: Adventurer's Song
			{
				break;
			}
			case 9:
			{
				if (Config.RESURRECT_BY_PAYMENT_ENABLED)
				{
					if (!player.isDead())
					{
						break;
					}
					
					int originalValue = player.getVariables().getInt(PlayerVariables.RESURRECT_BY_PAYMENT_COUNT, 0);
					if (originalValue < Config.RESURRECT_BY_PAYMENT_MAX_FREE_TIMES)
					{
						player.getVariables().set(PlayerVariables.RESURRECT_BY_PAYMENT_COUNT, originalValue + 1);
						player.doRevive(100.0);
						loc = MapRegionManager.getInstance().getTeleToLocation(player, TeleportWhereType.TOWN);
						player.teleToLocation(loc.Value, true, instance);
						break;
					}
					
					int firstID = Config.RESURRECT_BY_PAYMENT_ENABLED ? Config.RESURRECT_BY_PAYMENT_FIRST_RESURRECT_ITEM : 91663;
					int secondID = Config.RESURRECT_BY_PAYMENT_ENABLED ? Config.RESURRECT_BY_PAYMENT_SECOND_RESURRECT_ITEM : 57;
					ImmutableDictionary<int, ImmutableDictionary<int, ResurrectByPaymentHolder>> resMAP = null;
					Item item = null;
					if (resItemID == firstID)
					{
						resMAP = Config.RESURRECT_BY_PAYMENT_FIRST_RESURRECT_VALUES;
						item = player.getInventory().getItemByItemId(Config.RESURRECT_BY_PAYMENT_FIRST_RESURRECT_ITEM);
					}
					else if (resItemID == secondID)
					{
						resMAP = Config.RESURRECT_BY_PAYMENT_SECOND_RESURRECT_VALUES;
						item = player.getInventory().getItemByItemId(Config.RESURRECT_BY_PAYMENT_SECOND_RESURRECT_ITEM);
					}
					
					if ((resMAP == null) || (item == null))
					{
						break;
					}
					
					List<int> levelList = new(resMAP.Keys);
					foreach (int level in levelList)
					{
						if ((player.getLevel() >= level) && (levelList.LastIndexOf(level) != (levelList.size() - 1)))
						{
							continue;
						}
						
						int maxResTime;
						try
						{
							maxResTime = resMAP[level].Keys.Max();
						}
						catch (Exception e)
						{
							player.sendPacket(SystemMessageId.NOT_ENOUGH_ITEMS);
							return;
						}
						
						int getValue = maxResTime <= originalValue ? maxResTime : originalValue + 1;
						ResurrectByPaymentHolder rbph = resMAP[level][getValue];
						long fee = (int) (rbph.getAmount() * player.getStat().getValue(Stat.RESURRECTION_FEE_MODIFIER, 1));
						if (item.getCount() < fee)
						{
							return;
						}
						
						player.getVariables().set(PlayerVariables.RESURRECT_BY_PAYMENT_COUNT, originalValue + 1);
						player.destroyItem("item revive", item, fee, player, true);
						player.doRevive(rbph.getResurrectPercent());
						loc = MapRegionManager.getInstance().getTeleToLocation(player, TeleportWhereType.TOWN);
						player.teleToLocation(loc.Value, true, instance);
						break;
					}
				}
				
				break;
			}
				
			case 27: // to jail
			{
				if (!player.isJailed())
				{
					return;
				}
				
				loc = new Location(-114356, -249645, -2984, 0);
				break;
			}
			default:
			{
				if (player.isInTimedHuntingZone())
				{
					instance = player.getInstanceWorld();
					loc = new Location(player.getActingPlayer().getTimedHuntingZone().getEnterLocation(), 0);
				}
				else
				{
					loc = MapRegionManager.getInstance().getTeleToLocation(player, TeleportWhereType.TOWN);
				}
				break;
			}
		}
		
		// Teleport and revive
		if (loc != null)
		{
			player.setIsPendingRevive(true);
			player.teleToLocation(loc.Value, true, instance);
		}
	}

	private class DeathTask(Player player, int requestedPointType, int resItemID): Runnable
	{
		public void run() => portPlayer(player, requestedPointType, resItemID);
	}
}
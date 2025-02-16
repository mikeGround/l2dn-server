using L2Dn.Extensions;
using L2Dn.GameServer.Data.Sql;
using L2Dn.GameServer.Db;
using L2Dn.GameServer.Handlers;
using L2Dn.GameServer.Model;
using L2Dn.GameServer.Model.Actor;
using L2Dn.GameServer.Model.Events;
using L2Dn.GameServer.Model.Events.Impl.Sieges;
using L2Dn.GameServer.Utilities;
using Clan = L2Dn.GameServer.Model.Clans.Clan;
using SiegeClan = L2Dn.GameServer.Model.SiegeClan;

namespace L2Dn.GameServer.Scripts.Handlers.DailyMissionHandlers;

/**
 * @author UnAfraid
 */
public class SiegeDailyMissionHandler: AbstractDailyMissionHandler
{
	private readonly int _minLevel;
	private readonly int _maxLevel;

	public SiegeDailyMissionHandler(DailyMissionDataHolder holder): base(holder)
	{
		_minLevel = holder.getParams().getInt("minLevel", 0);
		_maxLevel = holder.getParams().getInt("maxLevel", int.MaxValue);
	}

	public override void init()
	{
		GlobalEvents.Global.Subscribe<OnCastleSiegeStart>(this, onSiegeStart);
	}

	public override bool isAvailable(Player player)
	{
		DailyMissionPlayerEntry? entry = player.getDailyMissions().getEntry(getHolder().getId());
		return entry != null && entry.getStatus() == DailyMissionStatus.AVAILABLE;
	}

	private void onSiegeStart(OnCastleSiegeStart @event)
	{
		@event.getSiege().getAttackerClans().ForEach(processSiegeClan);
		@event.getSiege().getDefenderClans().ForEach(processSiegeClan);
	}

	private void processSiegeClan(SiegeClan siegeClan)
	{
		Clan? clan = ClanTable.getInstance().getClan(siegeClan.getClanId());
		if (clan != null)
		{
			clan.getOnlineMembers(0).ForEach(player =>
			{
				if (player.getLevel() < _minLevel || player.getLevel() > _maxLevel)
				{
					return;
				}

				DailyMissionPlayerEntry entry = player.getDailyMissions().getOrCreateEntry(getHolder().getId());
				entry.setStatus(DailyMissionStatus.AVAILABLE);
				player.getDailyMissions().storeEntry(entry);
			});
		}
	}
}
using L2Dn.GameServer.Db;
using L2Dn.GameServer.Enums;
using L2Dn.GameServer.Handlers;
using L2Dn.GameServer.Model;
using L2Dn.GameServer.Model.Actor;
using L2Dn.GameServer.Model.Events;
using L2Dn.GameServer.Model.Events.Impl.Creatures;
using L2Dn.GameServer.Model.Events.Impl.Creatures.Players;
using L2Dn.GameServer.Model.Events.Listeners;

namespace L2Dn.GameServer.Scripts.Handlers.DailyMissionHandlers;

/**
 * @author JoeAlisson
 */
public class SpiritDailyMissionHandler: AbstractDailyMissionHandler
{
	private readonly int _amount;
	private readonly ElementalType _type;
	
	public SpiritDailyMissionHandler(DailyMissionDataHolder holder): base(holder)
	{
		_type = getHolder().getParams().getEnum("element", ElementalType.NONE);
		_amount = holder.getRequiredCompletions();
	}
	
	public override void init()
	{
		MissionKind kind = getHolder().getParams().getEnum("kind", MissionKind.UNKNOWN);
		if (MissionKind.EVOLVE == kind)
		{
			Containers.Players().addListener(new ConsumerEventListener(this, EventType.ON_ELEMENTAL_SPIRIT_UPGRADE,
				ev => onElementalSpiritUpgrade((OnElementalSpiritUpgrade)ev), this));
		}
		else if (MissionKind.LEARN == kind)
		{
			Containers.Players().addListener(new ConsumerEventListener(this, EventType.ON_ELEMENTAL_SPIRIT_LEARN,
				ev => onElementalSpiritLearn((OnElementalSpiritLearn)ev), this));
		}
	}
	
	public override bool isAvailable(Player player)
	{
		DailyMissionPlayerEntry entry = getPlayerEntry(player.getObjectId(), false);
		return (entry != null) && (entry.getStatus() == DailyMissionStatus.AVAILABLE);
	}
	
	private void onElementalSpiritLearn(OnElementalSpiritLearn @event)
	{
		DailyMissionPlayerEntry missionData = getPlayerEntry(@event.getPlayer().getObjectId(), true);
		missionData.setProgress(1);
		missionData.setStatus(DailyMissionStatus.AVAILABLE);
		storePlayerEntry(missionData);
	}
	
	private void onElementalSpiritUpgrade(OnElementalSpiritUpgrade @event)
	{
		ElementalSpirit spirit = @event.getSpirit();
		if (spirit.getType() != _type)
		{
			return;
		}
		
		DailyMissionPlayerEntry missionData = getPlayerEntry(@event.getPlayer().getObjectId(), true);
		missionData.setProgress(spirit.getStage());
		if (missionData.getProgress() >= _amount)
		{
			missionData.setStatus(DailyMissionStatus.AVAILABLE);
		}
		storePlayerEntry(missionData);
	}
	
	private enum MissionKind
	{
		LEARN,
		EVOLVE,
		
		UNKNOWN
	}
}
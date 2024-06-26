using L2Dn.GameServer.Model;
using L2Dn.GameServer.Model.Actor;
using L2Dn.GameServer.Model.Effects;
using L2Dn.GameServer.Model.Items.Instances;
using L2Dn.GameServer.Model.Skills;
using L2Dn.GameServer.Model.Stats;
using L2Dn.GameServer.Network.Enums;
using L2Dn.GameServer.Network.OutgoingPackets;
using L2Dn.Model.Enums;

namespace L2Dn.GameServer.Scripts.Handlers.EffectHandlers;

/**
 * Focus Energy effect implementation.
 * @author DS
 */
public class FocusMomentum: AbstractEffect
{
	private readonly int _amount;
	private readonly int _maxCharges;
	
	public FocusMomentum(StatSet @params)
	{
		_amount = @params.getInt("amount", 1);
		_maxCharges = @params.getInt("maxCharges", 0);
	}
	
	public override bool isInstant()
	{
		return true;
	}
	
	public override void instant(Creature effector, Creature effected, Skill skill, Item item)
	{
		if (!effected.isPlayer())
		{
			return;
		}
		
		Player player = effected.getActingPlayer();
		int currentCharges = player.getCharges();
		int maxCharges = Math.Min(_maxCharges, (int) effected.getStat().getValue(Stat.MAX_MOMENTUM, 1));
		
		if (currentCharges >= maxCharges)
		{
			if (!skill.isTriggeredSkill())
			{
				player.sendPacket(SystemMessageId.YOUR_FORCE_HAS_REACHED_MAXIMUM_CAPACITY);
			}
			return;
		}
		
		int newCharge = Math.Min(currentCharges + _amount, maxCharges);
		
		player.setCharges(newCharge);
		
		if (newCharge == maxCharges)
		{
			player.sendPacket(SystemMessageId.YOUR_FORCE_HAS_REACHED_MAXIMUM_CAPACITY);
		}
		else
		{
			SystemMessagePacket sm = new SystemMessagePacket(SystemMessageId.YOUR_FORCE_HAS_INCREASED_TO_LEVEL_S1);
			sm.Params.addInt(newCharge);
			player.sendPacket(sm);
		}
		
		player.sendPacket(new EtcStatusUpdatePacket(player));
	}
}
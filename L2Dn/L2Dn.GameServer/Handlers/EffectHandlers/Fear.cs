using L2Dn.GameServer.AI;
using L2Dn.GameServer.Enums;
using L2Dn.GameServer.Geo;
using L2Dn.GameServer.Model;
using L2Dn.GameServer.Model.Actor;
using L2Dn.GameServer.Model.Actor.Instances;
using L2Dn.GameServer.Model.Effects;
using L2Dn.GameServer.Model.Items.Instances;
using L2Dn.GameServer.Model.Skills;
using L2Dn.GameServer.Utilities;

namespace L2Dn.GameServer.Handlers.EffectHandlers;

/**
 * Fear effect implementation.
 * @author littlecrow
 */
public class Fear: AbstractEffect
{
	private static readonly int FEAR_RANGE = 500;
	
	public Fear(StatSet @params)
	{
	}
	
	public override long getEffectFlags()
	{
		return EffectFlag.FEAR.getMask();
	}
	
	public override bool canStart(Creature effector, Creature effected, Skill skill)
	{
		if ((effected == null) || effected.isRaid())
		{
			return false;
		}
		
		return effected.isPlayer() || effected.isSummon() || (effected.isAttackable() //
			&& !((effected is Defender) || (effected is FortCommander) //
				|| (effected is SiegeFlag) || (effected.getTemplate().getRace() == Race.SIEGE_WEAPON)));
	}
	
	public override int getTicks()
	{
		return 5;
	}
	
	public override bool onActionTime(Creature effector, Creature effected, Skill skill, Item item)
	{
		fearAction(null, effected);
		return false;
	}
	
	public override void onStart(Creature effector, Creature effected, Skill skill, Item item)
	{
		effected.getAI().notifyEvent(CtrlEvent.EVT_AFRAID);
		fearAction(effector, effected);
	}
	
	public override void onExit(Creature effector, Creature effected, Skill skill)
	{
		if (!effected.isPlayer())
		{
			effected.getAI().notifyEvent(CtrlEvent.EVT_THINK);
		}
	}
	
	private void fearAction(Creature effector, Creature effected)
	{
		double radians = MathUtil.toRadians((effector != null) ? Util.calculateAngleFrom(effector, effected) : Util.convertHeadingToDegree(effected.getHeading()));
		
		int posX = (int) (effected.getX() + (FEAR_RANGE * Math.Cos(radians)));
		int posY = (int) (effected.getY() + (FEAR_RANGE * Math.Sin(radians)));
		int posZ = effected.getZ();
		
		Location destination = GeoEngine.getInstance().getValidLocation(effected.getX(), effected.getY(), effected.getZ(), posX, posY, posZ, effected.getInstanceWorld());
		effected.getAI().setIntention(CtrlIntention.AI_INTENTION_MOVE_TO, destination);
	}
}
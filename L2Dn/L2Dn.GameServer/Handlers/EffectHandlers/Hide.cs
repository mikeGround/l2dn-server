using L2Dn.GameServer.AI;
using L2Dn.GameServer.Model;
using L2Dn.GameServer.Model.Actor;
using L2Dn.GameServer.Model.Effects;
using L2Dn.GameServer.Model.Items.Instances;
using L2Dn.GameServer.Model.Skills;

namespace L2Dn.GameServer.Handlers.EffectHandlers;

/**
 * Hide effect implementation.
 * @author ZaKaX, nBd
 */
public class Hide: AbstractEffect
{
	public Hide(StatSet @params)
	{
	}
	
	public override void onStart(Creature effector, Creature effected, Skill skill, Item item)
	{
		if (effected.isPlayer())
		{
			effected.setInvisible(true);
			
			if ((effected.getAI().getNextIntention() != null) && (effected.getAI().getNextIntention().getCtrlIntention() == CtrlIntention.AI_INTENTION_ATTACK))
			{
				effected.getAI().setIntention(CtrlIntention.AI_INTENTION_IDLE);
			}
			
			World.getInstance().forEachVisibleObject<Creature>(effected, target =>
			{
				if ((target.getTarget() == effected))
				{
					target.setTarget(null);
					target.abortAttack();
					target.abortCast();
					target.getAI().setIntention(CtrlIntention.AI_INTENTION_IDLE);
				}
			});
		}
	}
	
	public override void onExit(Creature effector, Creature effected, Skill skill)
	{
		if (effected.isPlayer())
		{
			Player player = effected.getActingPlayer();
			if (!player.inObserverMode())
			{
				player.setInvisible(false);
			}
		}
	}
}
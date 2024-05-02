using L2Dn.GameServer.Geo;
using L2Dn.GameServer.Handlers;
using L2Dn.GameServer.Model;
using L2Dn.GameServer.Model.Actor;
using L2Dn.GameServer.Model.Skills;
using L2Dn.GameServer.Model.Skills.Targets;
using L2Dn.GameServer.Utilities;

namespace L2Dn.GameServer.Scripts.Handlers.TargetHandlers.AffectScopes;

/**
 * Range affect scope implementation. Gathers objects in area of target origin (including origin itself).
 * @author Nik
 */
public class Range: IAffectScopeHandler
{
	public void forEachAffected<T>(Creature creature, WorldObject target, Skill skill, Action<T> action)
		where T: WorldObject
	{
		IAffectObjectHandler affectObject = AffectObjectHandler.getInstance().getHandler(skill.getAffectObject());
		int affectRange = skill.getAffectRange();
		int affectLimit = skill.getAffectLimit();
		
		// Target checks.
		TargetType targetType = skill.getTargetType();
		AtomicInteger affected = new AtomicInteger(0);
		Predicate<Creature> filter = c =>
		{
			if ((affectLimit > 0) && (affected.get() >= affectLimit))
			{
				return false;
			}
			if (c.isDead() && (targetType != TargetType.NPC_BODY) && (targetType != TargetType.PC_BODY))
			{
				return false;
			}
			if ((c == creature) && (target != creature)) // Range skills appear to not affect you unless you are the main target.
			{
				return false;
			}
			if ((c != target) && (affectObject != null) && !affectObject.checkAffectedObject(creature, c))
			{
				return false;
			}
			if (!GeoEngine.getInstance().canSeeTarget(target, c))
			{
				return false;
			}
			
			affected.incrementAndGet();
			return true;
		};
		
		// Check and add targets.
		if (targetType == TargetType.GROUND)
		{
			if (creature.isPlayable())
			{
				Location worldPosition = creature.getActingPlayer().getCurrentSkillWorldPosition();
				if (worldPosition != null)
				{
					World.getInstance().forEachVisibleObjectInRange<Creature>(creature, (int) (affectRange + creature.calculateDistance2D(worldPosition.ToLocation2D())), c =>
					{
						if (!c.isInsideRadius3D(worldPosition.ToLocation3D(), affectRange))
						{
							return;
						}
						if (filter(c))
						{
							action((T)(WorldObject)c);
						}
					});
				}
			}
		}
		else
		{
			// Add object of origin since its skipped in the forEachVisibleObjectInRange method.
			if (target.isCreature() && filter((Creature) target))
			{
				action((T)(WorldObject)target);
			}
			
			World.getInstance().forEachVisibleObjectInRange<Creature>(target, affectRange, c =>
			{
				if (filter(c))
				{
					action((T)(WorldObject)c);
				}
			});
		}
	}
	
	public AffectScope getAffectScopeType()
	{
		return AffectScope.RANGE;
	}
}

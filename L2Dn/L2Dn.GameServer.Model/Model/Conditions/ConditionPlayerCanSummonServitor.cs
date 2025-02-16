using L2Dn.GameServer.Model.Actor;
using L2Dn.GameServer.Model.Items;
using L2Dn.GameServer.Model.Skills;

namespace L2Dn.GameServer.Model.Conditions;

/**
 * Player Can Summon condition implementation.
 * @author Sdw
 */
public class ConditionPlayerCanSummonServitor: Condition
{
	private readonly bool _value;
	
	public ConditionPlayerCanSummonServitor(bool value)
	{
		_value = value;
	}
	
	public override bool testImpl(Creature effector, Creature effected, Skill skill, ItemTemplate item)
	{
		Player player = effector.getActingPlayer();
		if (player == null)
		{
			return false;
		}
		
		bool canSummon = true;
		if (player.isFlyingMounted() || player.isMounted() || player.inObserverMode() || player.isTeleporting())
		{
			canSummon = false;
		}
		else if (player.getServitors().Count >= 4)
		{
			canSummon = false;
		}
		return canSummon == _value;
	}
}
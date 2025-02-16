using L2Dn.GameServer.Model.Actor;
using L2Dn.GameServer.Model.Conditions;
using L2Dn.GameServer.Model.Skills;
using L2Dn.Model.Enums;

namespace L2Dn.GameServer.Model.Stats.Functions;

/**
 * Returns the initial value plus the function value, if the condition are met.
 * @author Zoey76
 */
public class FuncMul: AbstractFunction
{
	public FuncMul(Stat stat, int order, object owner, double value, Condition applayCond)
		: base(stat, order, owner, value, applayCond)
	{
	}

	public override double calc(Creature effector, Creature effected, Skill skill, double initVal)
	{
		if ((getApplayCond() == null) || getApplayCond().test(effector, effected, skill))
		{
			return initVal * getValue();
		}

		return initVal;
	}
}
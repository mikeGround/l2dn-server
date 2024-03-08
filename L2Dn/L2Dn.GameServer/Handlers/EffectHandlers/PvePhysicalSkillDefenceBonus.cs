using L2Dn.GameServer.Model;
using L2Dn.GameServer.Model.Stats;

namespace L2Dn.GameServer.Handlers.EffectHandlers;

/**
 * @author Sdw
 */
public class PvePhysicalSkillDefenceBonus: AbstractStatPercentEffect
{
	public PvePhysicalSkillDefenceBonus(StatSet @params): base(@params, Stat.PVE_PHYSICAL_SKILL_DEFENCE)
	{
	}
}
using L2Dn.GameServer.Model;
using L2Dn.GameServer.Model.Actor;
using L2Dn.GameServer.Model.Effects;
using L2Dn.GameServer.Model.Skills;
using L2Dn.GameServer.Model.Stats;
using L2Dn.Model.Enums;

namespace L2Dn.GameServer.Scripts.Handlers.EffectHandlers;

/**
 * @author Sdw
 */
public class SkillMastery: AbstractEffect
{
	private readonly double _stat;
	
	public SkillMastery(StatSet @params)
	{
		_stat = (int)@params.getEnum("stat", BaseStat.STR);
	}
	
	public override void pump(Creature effected, Skill skill)
	{
		effected.getStat().mergeAdd(Stat.SKILL_MASTERY, _stat);
	}
}
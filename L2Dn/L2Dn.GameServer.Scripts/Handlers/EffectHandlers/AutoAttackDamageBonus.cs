using L2Dn.GameServer.Model;
using L2Dn.GameServer.Model.Stats;
using L2Dn.Model.Enums;

namespace L2Dn.GameServer.Scripts.Handlers.EffectHandlers;

/**
 * @author NasSeKa
 */
public class AutoAttackDamageBonus: AbstractStatPercentEffect
{
	public AutoAttackDamageBonus(StatSet @params): base(@params, Stat.AUTO_ATTACK_DAMAGE_BONUS)
	{
	}
}
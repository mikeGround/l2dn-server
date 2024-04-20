using L2Dn.GameServer.Model;
using L2Dn.GameServer.Model.Stats;
using L2Dn.Model.Enums;

namespace L2Dn.GameServer.Scripts.Handlers.EffectHandlers;

/**
 * @author JoeAlisson
 */
public class SpiritExpModify: AbstractStatEffect
{
	public SpiritExpModify(StatSet @params): base(@params, Stat.ELEMENTAL_SPIRIT_BONUS_EXP)
	{
	}
}
using L2Dn.GameServer.Model;
using L2Dn.GameServer.Model.Stats;

namespace L2Dn.GameServer.Handlers.EffectHandlers;

/**
 * @author Mobius
 */
public class SoulshotResistance: AbstractStatPercentEffect
{
	public SoulshotResistance(StatSet @params): base(@params, Stat.SOULSHOT_RESISTANCE)
	{
	}
}
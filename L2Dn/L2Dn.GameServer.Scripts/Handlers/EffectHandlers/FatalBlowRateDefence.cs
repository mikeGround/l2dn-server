using L2Dn.GameServer.Model;
using L2Dn.GameServer.Model.Stats;
using L2Dn.Model.Enums;

namespace L2Dn.GameServer.Scripts.Handlers.EffectHandlers;

/**
 * @author Sahar
 */
public class FatalBlowRateDefence: AbstractStatEffect
{
	public FatalBlowRateDefence(StatSet @params): base(@params, Stat.BLOW_RATE_DEFENCE)
	{
	}
}
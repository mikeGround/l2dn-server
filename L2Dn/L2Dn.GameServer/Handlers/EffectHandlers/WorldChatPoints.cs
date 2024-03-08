using L2Dn.GameServer.Model;
using L2Dn.GameServer.Model.Stats;

namespace L2Dn.GameServer.Handlers.EffectHandlers;

/**
 * @author Nik
 */
public class WorldChatPoints: AbstractStatEffect
{
	public WorldChatPoints(StatSet @params): base(@params, Stat.WORLD_CHAT_POINTS)
	{
	}
}
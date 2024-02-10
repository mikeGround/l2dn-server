using L2Dn.GameServer.Model.Actor;

namespace L2Dn.GameServer.Handlers;

/**
 * Community Board interface.
 * @author Zoey76
 */
public interface IWriteBoardHandler: IParseBoardHandler
{
	/**
	 * Writes a community board command into the client.
	 * @param player the player
	 * @param arg1 the first argument
	 * @param arg2 the second argument
	 * @param arg3 the third argument
	 * @param arg4 the fourth argument
	 * @param arg5 the fifth argument
	 * @return
	 */
	bool writeCommunityBoardCommand(Player player, String arg1, String arg2, String arg3, String arg4, String arg5);
}
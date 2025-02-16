using L2Dn.GameServer.Model.Actor;
using L2Dn.GameServer.Model.Events.Impl.Base;

namespace L2Dn.GameServer.Model.Events.Impl.Players;

/**
 * @author UnAfraid
 */
public class OnPlayerBypass: TerminateEventBase
{
	private readonly Player _player;
	private readonly string _command;
	
	public OnPlayerBypass(Player player, string command)
	{
		_player = player;
		_command = command;
	}
	
	public Player getPlayer()
	{
		return _player;
	}
	
	public string getCommand()
	{
		return _command;
	}
}
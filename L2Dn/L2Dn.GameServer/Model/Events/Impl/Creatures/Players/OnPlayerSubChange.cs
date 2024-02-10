using L2Dn.GameServer.Model.Actor;

namespace L2Dn.GameServer.Model.Events.Impl.Creatures.Players;

/**
 * @author malyelfik
 */
public class OnPlayerSubChange: IBaseEvent
{
	private readonly Player _player;
	
	public OnPlayerSubChange(Player player)
	{
		_player = player;
	}
	
	public Player getPlayer()
	{
		return _player;
	}
	
	public EventType getType()
	{
		return EventType.ON_PLAYER_SUB_CHANGE;
	}
}
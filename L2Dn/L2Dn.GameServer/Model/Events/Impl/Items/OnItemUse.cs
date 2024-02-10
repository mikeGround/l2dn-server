using L2Dn.GameServer.Model.Actor;
using L2Dn.GameServer.Model.Items.Instances;

namespace L2Dn.GameServer.Model.Events.Impl.Items;

/**
 * @author Mobius
 */
public class OnItemUse: IBaseEvent
{
	private readonly Player _player;
	private readonly Item _item;
	
	public OnItemUse(Player player, Item item)
	{
		_player = player;
		_item = item;
	}
	
	public Player getPlayer()
	{
		return _player;
	}
	
	public Item getItem()
	{
		return _item;
	}
	
	public EventType getType()
	{
		return EventType.ON_ITEM_USE;
	}
}
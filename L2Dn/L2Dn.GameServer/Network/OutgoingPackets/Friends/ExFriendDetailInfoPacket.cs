using L2Dn.Extensions;
using L2Dn.GameServer.Data.Sql;
using L2Dn.GameServer.Model;
using L2Dn.GameServer.Model.Actor;
using L2Dn.GameServer.Model.Clans;
using L2Dn.Packets;

namespace L2Dn.GameServer.Network.OutgoingPackets.Friends;

/**
 * @author Sdw
 */
public readonly struct ExFriendDetailInfoPacket: IOutgoingPacket
{
	private readonly int _objectId;
	private readonly Player _friend;
	private readonly String _name;
	private readonly int _lastAccess;
	
	public ExFriendDetailInfoPacket(Player player, String name)
	{
		_objectId = player.getObjectId();
		_name = name;
		_friend = World.getInstance().getPlayer(_name);
		
		DateTime now = DateTime.UtcNow;
		_lastAccess = (_friend == null) || _friend.isBlocked(player) ? 0 :
			_friend.isOnline() ? now.getEpochSecond() * 1000 :
			(int)(now - _friend.getLastAccess()).TotalSeconds;
	}
	
	public void WriteContent(PacketBitWriter writer)
	{
		writer.WritePacketCode(OutgoingPacketCodes.EX_FRIEND_DETAIL_INFO);
		
		writer.WriteInt32(_objectId);
		if (_friend == null)
		{
			int charId = CharInfoTable.getInstance().getIdByName(_name);
			writer.WriteString(_name);
			writer.WriteInt32(0); // isonline = 0
			writer.WriteInt32(charId);
			writer.WriteInt16((short)CharInfoTable.getInstance().getLevelById(charId));
			writer.WriteInt16((short)CharInfoTable.getInstance().getClassIdById(charId));
			Clan clan = ClanTable.getInstance().getClan(CharInfoTable.getInstance().getClanIdById(charId));
			if (clan != null)
			{
				writer.WriteInt32(clan.getId());
				writer.WriteInt32(clan.getCrestId());
				writer.WriteString(clan.getName());
				writer.WriteInt32(clan.getAllyId());
				writer.WriteInt32(clan.getAllyCrestId());
				writer.WriteString(clan.getAllyName());
			}
			else
			{
				writer.WriteInt32(0);
				writer.WriteInt32(0);
				writer.WriteString("");
				writer.WriteInt32(0);
				writer.WriteInt32(0);
				writer.WriteString("");
			}
			
			DateOnly? createDate = CharInfoTable.getInstance().getCharacterCreationDate(charId);
			writer.WriteByte((byte)createDate.Value.Month);
			writer.WriteByte((byte)createDate.Value.Day);
			writer.WriteInt32((int)CharInfoTable.getInstance().getLastAccessDelay(charId).TotalSeconds); // TODO may be incorrect
			writer.WriteString(CharInfoTable.getInstance().getFriendMemo(_objectId, charId));
		}
		else
		{
			writer.WriteString(_friend.getName());
			writer.WriteInt32(_friend.isOnlineInt());
			writer.WriteInt32(_friend.getObjectId());
			writer.WriteInt16((short)_friend.getLevel());
			writer.WriteInt16((short)_friend.getClassId());
			writer.WriteInt32(_friend.getClanId());
			writer.WriteInt32(_friend.getClanCrestId());
			writer.WriteString(_friend.getClan() != null ? _friend.getClan().getName() : "");
			writer.WriteInt32(_friend.getAllyId());
			writer.WriteInt32(_friend.getAllyCrestId());
			writer.WriteString(_friend.getClan() != null ? _friend.getClan().getAllyName() : "");
			
			DateTime createDate = _friend.getCreateDate();
			writer.WriteByte((byte)createDate.Month);
			writer.WriteByte((byte)createDate.Day);
			writer.WriteInt32(_lastAccess);
			writer.WriteString(CharInfoTable.getInstance().getFriendMemo(_objectId, _friend.getObjectId()));
		}
	}
}
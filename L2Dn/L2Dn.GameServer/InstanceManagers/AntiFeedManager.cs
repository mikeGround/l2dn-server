using L2Dn.GameServer.Model.Actor;
using L2Dn.GameServer.Model.Olympiads;
using L2Dn.GameServer.Utilities;
using NLog;

namespace L2Dn.GameServer.InstanceManagers;

public class AntiFeedManager
{
	protected static readonly Logger LOGGER_ACCOUNTING = LogManager.GetLogger(nameof(AntiFeedManager));
	
	public const int GAME_ID = 0;
	public const int OLYMPIAD_ID = 1;
	public const int TVT_ID = 2;
	public const int L2EVENT_ID = 3;
	
	private readonly Map<int, long> _lastDeathTimes = new();
	private readonly Map<int, Map<int, AtomicInteger>> _eventIPs = new();
	
	protected AntiFeedManager()
	{
	}
	
	/**
	 * Set time of the last player's death to current
	 * @param objectId Player's objectId
	 */
	public void setLastDeathTime(int objectId)
	{
		_lastDeathTimes.put(objectId, System.currentTimeMillis());
	}
	
	/**
	 * Check if current kill should be counted as non-feeded.
	 * @param attacker Attacker character
	 * @param target Target character
	 * @return True if kill is non-feeded.
	 */
	public bool check(Creature attacker, Creature target)
	{
		if (!Config.ANTIFEED_ENABLE)
		{
			return true;
		}
		
		if (target == null)
		{
			return false;
		}
		
		Player targetPlayer = target.getActingPlayer();
		if (targetPlayer == null)
		{
			return false;
		}
		
		// Players in offline mode should't be valid targets.
		if (targetPlayer.getClient().isDetached())
		{
			return false;
		}
		
		if ((Config.ANTIFEED_INTERVAL > 0) && _lastDeathTimes.containsKey(targetPlayer.getObjectId()) && ((System.currentTimeMillis() - _lastDeathTimes.get(targetPlayer.getObjectId())) < Config.ANTIFEED_INTERVAL))
		{
			return false;
		}
		
		if (Config.ANTIFEED_DUALBOX && (attacker != null))
		{
			Player attackerPlayer = attacker.getActingPlayer();
			if (attackerPlayer == null)
			{
				return false;
			}
			
			GameClient targetClient = targetPlayer.getClient();
			GameClient attackerClient = attackerPlayer.getClient();
			if ((targetClient == null) || (attackerClient == null) || targetClient.isDetached() || attackerClient.isDetached())
			{
				// unable to check ip address
				return !Config.ANTIFEED_DISCONNECTED_AS_DUALBOX;
			}
			
			return !targetClient.getIp().equals(attackerClient.getIp());
		}
		
		return true;
	}
	
	/**
	 * Clears all timestamps
	 */
	public void clear()
	{
		_lastDeathTimes.clear();
	}
	
	/**
	 * Register new event for dualbox check. Should be called only once.
	 * @param eventId
	 */
	public void registerEvent(int eventId)
	{
		_eventIPs.putIfAbsent(eventId, new());
	}
	
	/**
	 * @param eventId
	 * @param player
	 * @param max
	 * @return If number of all simultaneous connections from player's IP address lower than max then increment connection count and return true.<br>
	 *         False if number of all simultaneous connections from player's IP address higher than max.
	 */
	public bool tryAddPlayer(int eventId, Player player, int max)
	{
		return tryAddClient(eventId, player.getClient(), max);
	}
	
	/**
	 * @param eventId
	 * @param client
	 * @param max
	 * @return If number of all simultaneous connections from player's IP address lower than max then increment connection count and return true.<br>
	 *         False if number of all simultaneous connections from player's IP address higher than max.
	 */
	public bool tryAddClient(int eventId, GameClient client, int max)
	{
		if (client == null)
		{
			return false; // unable to determine IP address
		}
		
		Map<int, AtomicInteger> @event = _eventIPs.get(eventId);
		if (@event == null)
		{
			return false; // no such event registered
		}
		
		int addrHash = client.getIp().hashCode();
		AtomicInteger connectionCount = @event.computeIfAbsent(addrHash, k => new AtomicInteger());
		if ((connectionCount.get() + 1) <= (max + Config.DUALBOX_CHECK_WHITELIST.getOrDefault(addrHash, 0)))
		{
			connectionCount.incrementAndGet();
			return true;
		}
		
		return false;
	}
	
	/**
	 * Decreasing number of active connection from player's IP address
	 * @param eventId
	 * @param player
	 * @return true if success and false if any problem detected.
	 */
	public bool removePlayer(int eventId, Player player)
	{
		return removeClient(eventId, player.getClient());
	}
	
	/**
	 * Decreasing number of active connection from player's IP address
	 * @param eventId
	 * @param client
	 * @return true if success and false if any problem detected.
	 */
	public bool removeClient(int eventId, GameClient client)
	{
		if (client == null)
		{
			return false; // unable to determine IP address
		}
		
		Map<int, AtomicInteger> @event = _eventIPs.get(eventId);
		if (@event == null)
		{
			return false; // no such event registered
		}
		
		int addrHash = client.getIp().hashCode();
		return @event.computeIfPresent(addrHash, (k, v) =>
		{
			if ((v == null) || (v.decrementAndGet() == 0))
			{
				return null;
			}
			return v;
		}) != null;
	}
	
	/**
	 * Remove player connection IP address from all registered events lists.
	 * @param client
	 */
	public void onDisconnect(GameClient client)
	{
		if (client == null)
		{
			return;
		}
		
		Player player = client.getPlayer();
		if (player == null)
		{
			return;
		}
		
		if (player.isInOfflineMode())
		{
			return;
		}
		
		String clientIp = client.getIp();
		if (clientIp == null)
		{
			return;
		}
		
		foreach (var entry in _eventIPs)
		{
			int eventId = entry.Key;
			if (eventId == OLYMPIAD_ID)
			{
				AtomicInteger count = entry.Value.get(clientIp.GetHashCode());
				if ((count != null) && (OlympiadManager.getInstance().isRegistered(player) || (player.getOlympiadGameId() != -1)))
				{
					count.decrementAndGet();
				}
			}
			else
			{
				removeClient(eventId, client);
			}
		}
	}
	
	/**
	 * Clear all entries for this eventId.
	 * @param eventId
	 */
	public void clear(int eventId)
	{
		Map<int, AtomicInteger> @event = _eventIPs.get(eventId);
		if (@event != null)
		{
			@event.clear();
		}
	}
	
	/**
	 * @param player
	 * @param max
	 * @return maximum number of allowed connections (whitelist + max)
	 */
	public int getLimit(Player player, int max)
	{
		return getLimit(player.getClient(), max);
	}
	
	/**
	 * @param client
	 * @param max
	 * @return maximum number of allowed connections (whitelist + max)
	 */
	public int getLimit(GameClient client, int max)
	{
		if (client == null)
		{
			return max;
		}
		
		int addrHash = client.getIp().hashCode();
		int limit = max;
		if (Config.DUALBOX_CHECK_WHITELIST.containsKey(addrHash))
		{
			limit += Config.DUALBOX_CHECK_WHITELIST.get(addrHash);
		}
		return limit;
	}
	
	public static AntiFeedManager getInstance()
	{
		return SingletonHolder.INSTANCE;
	}
	
	private static class SingletonHolder
	{
		public static readonly AntiFeedManager INSTANCE = new AntiFeedManager();
	}
}
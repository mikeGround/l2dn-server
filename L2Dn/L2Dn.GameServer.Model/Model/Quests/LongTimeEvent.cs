using System.Xml.Linq;
using L2Dn.GameServer.Data.Sql;
using L2Dn.GameServer.Data.Xml;
using L2Dn.GameServer.Db;
using L2Dn.GameServer.InstanceManagers.Events;
using L2Dn.GameServer.Model.Actor;
using L2Dn.GameServer.Model.Announcements;
using L2Dn.GameServer.Model.Events;
using L2Dn.GameServer.Model.Events.Impl;
using L2Dn.GameServer.Model.Holders;
using L2Dn.GameServer.Scripts;
using L2Dn.GameServer.Utilities;
using L2Dn.Utilities;
using Microsoft.EntityFrameworkCore;
using ThreadPool = L2Dn.GameServer.Utilities.ThreadPool;

namespace L2Dn.GameServer.Model.Quests;

/**
 * Parent class for long time events.<br>
 * Maintains config reading, spawn of NPCs, adding of event's drop.
 * @author GKR
 */
public class LongTimeEvent: Quest
{
	protected String _eventName;
	protected DateRange _eventPeriod = null;
	protected bool _active = false;
	protected bool _enableShrines = false;
	
	// Messages
	protected String _onEnterMsg = "";
	protected String _endMsg = "";
	protected int _enterAnnounceId = -1;

	// NPCs to spawn and their spawn points
	protected readonly List<NpcSpawn> _spawnList = new();
	
	// Drop data for event
	protected readonly List<EventDropHolder> _dropList = new();
	
	// Items to destroy when event ends
	protected readonly List<int> _destroyItemsOnEnd = new();
	
	protected class NpcSpawn
	{
		public readonly  int npcId;
		public readonly  Location loc;
		public readonly  TimeSpan respawnTime;
		
		public NpcSpawn(int spawnNpcId, Location spawnLoc, TimeSpan spawnRespawnTime)
		{
			npcId = spawnNpcId;
			loc = spawnLoc;
			respawnTime = spawnRespawnTime;
		}
	}
	
	public LongTimeEvent(): base(-1)
	{
		loadConfig();

		if (_eventPeriod != null)
		{
			DateTime today = DateTime.Now;
			if (_eventPeriod.isWithinRange(today))
			{
				startEvent();
				LOGGER.Info("Event " + _eventName + " active till " + _eventPeriod.getEndDate());
			}
			else if (_eventPeriod.getStartDate() > today)
			{
				TimeSpan delay = _eventPeriod.getStartDate() - DateTime.Now;
				ThreadPool.schedule(new ScheduleStart(this), delay);
				LOGGER.Info("Event " + _eventName + " will be started at " + _eventPeriod.getStartDate());
			}
			else
			{
				// Destroy items that must exist only on event period.
				destroyItemsOnEnd();
				LOGGER.Info("Event " + _eventName + " has passed... Ignored ");
			}
		}
	}

	private void SpawnNpcs(OnServerStart serverStart)
	{
		TimeSpan millisToEventEnd = _eventPeriod.getEndDate() - DateTime.Now;
		foreach (NpcSpawn npcSpawn in _spawnList)
		{
			Npc npc = addSpawn(npcSpawn.npcId, npcSpawn.loc.getX(), npcSpawn.loc.getY(), npcSpawn.loc.getZ(),
				npcSpawn.loc.getHeading(), false, millisToEventEnd, false);
			TimeSpan respawnDelay = npcSpawn.respawnTime;
			if (respawnDelay > TimeSpan.Zero)
			{
				Spawn spawn = npc.getSpawn();
				spawn.setRespawnDelay(respawnDelay);
				spawn.startRespawn();
				ThreadPool.schedule(() => spawn.stopRespawn(), millisToEventEnd - respawnDelay);
			}
		}

		GlobalEvents.Global.Subscribe(this, (Action<OnServerStart>)SpawnNpcs);
	}
    
	/**
	 * Load event configuration file
	 */
	private void loadConfig()
	{
		string filePath = Path.Combine(Config.DATAPACK_ROOT_PATH, "scripts/events", getScriptName(), "config.xml");
		using FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
		XDocument document = XDocument.Load(stream);

		XElement? root = document.Element("event");
		if (root == null)
		{
			throw new InvalidOperationException("WARNING!!! " + getScriptName() + " event: bad config file!");
		}

		_eventName = root.GetAttributeValueAsString("name");
		_enableShrines = root.Attribute("enableShrines")?.GetBoolean() ?? false;
		string period = root.GetAttributeValueAsString("active");
		if (period.Length == 21)
		{
			// dd MM yyyy-dd MM yyyy
			_eventPeriod = DateRange.parse(period, "dd MM yyyy");
		}
		else if (period.Length == 11)
		{
			// dd MM-dd MM
			string currentYear = DateTime.Today.Year.ToString();
			String start = period.Split("-")[0] + " " + currentYear;
			String end = period.Split("-")[1] + " " + currentYear;
			String activePeriod = start + "-" + end;
			_eventPeriod = DateRange.parse(activePeriod, "dd MM yyyy");
		}

		if (_eventPeriod == null)
		{
			throw new InvalidOperationException("WARNING!!! " + getName() + " event: illegal event period");
		}

		DateTime today = DateTime.Today;
		if (_eventPeriod.getStartDate() > today || _eventPeriod.isWithinRange(today))
		{
			foreach (XElement n in root.Elements())
			{
				// Loading droplist
				if (n.Name.LocalName.equalsIgnoreCase("droplist"))
				{
					foreach (XElement d in n.Elements())
					{
						if (d.Name.LocalName.equalsIgnoreCase("add"))
						{
							try
							{
								int itemId = d.GetAttributeValueAsInt32("item");
								int minCount = d.GetAttributeValueAsInt32("min");
								int maxCount = d.GetAttributeValueAsInt32("max");
								string chance = d.GetAttributeValueAsString("chance");
								double finalChance = !chance.isEmpty() && chance.endsWith("%")
									? double.Parse(chance.Substring(0, chance.Length - 1))
									: 0;
								int minLevel = d.Attribute("minLevel")?.GetInt32() ?? 1;
								int maxLevel = d.Attribute("maxLevel")?.GetInt32() ?? int.MaxValue;
								string monsterIdsValue = d.Attribute("monsterIds")?.GetString();
								Set<int> monsterIds = new();
								if (monsterIdsValue != null)
								{
									foreach (string id in monsterIdsValue.Split(","))
									{
										monsterIds.add(int.Parse(id));
									}
								}

								if (ItemData.getInstance().getTemplate(itemId) == null)
								{
									LOGGER.Warn(getName() + " event: " + itemId +
									            " is wrong item id, item was not added in droplist");
									continue;
								}

								if (minCount > maxCount)
								{
									LOGGER.Warn(getName() + " event: item " + itemId +
									            " - min greater than max, item was not added in droplist");
									continue;
								}

								if ((finalChance < 0) || (finalChance > 100))
								{
									LOGGER.Warn(getName() + " event: item " + itemId +
									            " - incorrect drop chance, item was not added in droplist");
									continue;
								}

								_dropList.add(new EventDropHolder(itemId, minCount, maxCount, finalChance, minLevel,
									maxLevel, monsterIds));
							}
							catch (FormatException nfe)
							{
								LOGGER.Warn("Wrong number format in config.xml droplist block for " + getName() +
								            " event");
							}
						}
					}
				}
				else if (n.Name.LocalName.equalsIgnoreCase("spawnlist"))
				{
					// Loading spawnlist
					foreach (XElement d in n.Elements())
					{
						if (d.Name.LocalName.equalsIgnoreCase("add"))
						{
							try
							{
								int npcId = d.GetAttributeValueAsInt32("npc");
								int xPos = d.GetAttributeValueAsInt32("x");
								int yPos = d.GetAttributeValueAsInt32("y");
								int zPos = d.GetAttributeValueAsInt32("z");
								int heading = d.Attribute("heading")?.GetInt32() ?? 0;
								TimeSpan respawnTime =
									d.Attribute("respawnTime")?.GetTimeSpan() ?? TimeSpan.Zero;

								if (NpcData.getInstance().getTemplate(npcId) == null)
								{
									LOGGER.Warn(getName() + " event: " + npcId +
									            " is wrong NPC id, NPC was not added in spawnlist");
									continue;
								}

								_spawnList.add(
									new NpcSpawn(npcId, new Location(xPos, yPos, zPos, heading), respawnTime));
							}
							catch (FormatException nfe)
							{
								LOGGER.Warn("Wrong number format in config.xml spawnlist block for " + getName() +
								            " event");
							}
						}
					}
				}
				else if (n.Name.LocalName.equalsIgnoreCase("messages"))
				{
					// Loading Messages
					foreach (XElement d in n.Elements())
					{
						if (d.Name.LocalName.equalsIgnoreCase("add"))
						{
							String? msgType = d.Attribute("type")?.GetString();
							String? msgText = d.Attribute("text")?.GetString();
							if ((msgType != null) && (msgText != null))
							{
								if (msgType.equalsIgnoreCase("onEnd"))
								{
									_endMsg = msgText;
								}
								else if (msgType.equalsIgnoreCase("onEnter"))
								{
									_onEnterMsg = msgText;
								}
							}
						}
					}
				}
			}
		}

		// Load destroy item list at all times.
		foreach (XElement n in root.Elements("destroyItemsOnEnd"))
		{
			DateTime endtime = _eventPeriod.getEndDate();
			foreach (XElement d in n.Elements("item"))
			{
				try
				{
					int itemId = d.GetAttributeValueAsInt32("id");
					if (ItemData.getInstance().getTemplate(itemId) == null)
					{
						LOGGER.Warn(getScriptName() + " event: Item " + itemId + " does not exist.");
						continue;
					}

					_destroyItemsOnEnd.add(itemId);

					// Add item deletion info to manager.
					if (endtime > DateTime.UtcNow)
					{
						ItemDeletionInfoManager.getInstance().addItemDate(itemId, endtime);
					}
				}
				catch (FormatException nfe)
				{
					LOGGER.Warn("Wrong number format in config.xml destroyItemsOnEnd block for " + getScriptName() +
					            " event");
				}
			}
		}
	}

	protected class ScheduleStart: Runnable
	{
		private readonly LongTimeEvent _longTimeEvent;

		public ScheduleStart(LongTimeEvent longTimeEvent)
		{
			_longTimeEvent = longTimeEvent;
		}

		public void run()
		{
			_longTimeEvent.startEvent();
		}
	}
	
	protected void startEvent()
	{
		// Set Active.
		_active = true;
		
		// Add event drops.
		EventDropManager.getInstance().addDrops(this, _dropList);
		
		// Add spawns on server start.
		if (!_spawnList.isEmpty())
		{
			GlobalEvents.Global.Subscribe(this, (Action<OnServerStart>)SpawnNpcs);
		}
		
		// Enable town shrines.
		if (_enableShrines)
		{
			EventShrineManager.getInstance().setEnabled(true);
		}
		
		// Event enter announcement.
		if (!_onEnterMsg.isEmpty())
		{
			// Send message on begin.
			Broadcast.toAllOnlinePlayers(_onEnterMsg);
			
			// Add announce for entering players.
			EventAnnouncement announce = new EventAnnouncement(_eventPeriod, _onEnterMsg);
			AnnouncementsTable.getInstance().addAnnouncement(announce);
			_enterAnnounceId = announce.getId();
		}
		
		// Schedule event end.
		TimeSpan millisToEventEnd = _eventPeriod.getEndDate() - DateTime.Now;
		ThreadPool.schedule(new ScheduleEnd(this), millisToEventEnd);
	}
	
	protected class ScheduleEnd: Runnable
	{
		private readonly LongTimeEvent _longTimeEvent;

		public ScheduleEnd(LongTimeEvent longTimeEvent)
		{
			_longTimeEvent = longTimeEvent;
		}
		
		public void run()
		{
			_longTimeEvent.stopEvent();
		}
	}
	
	protected void stopEvent()
	{
		// Set Active.
		_active = false;
		
		// Stop event drops.
		EventDropManager.getInstance().removeDrops(this);
		
		// Disable town shrines.
		if (_enableShrines)
		{
			EventShrineManager.getInstance().setEnabled(false);
		}
		
		// Destroy items that must exist only on event period.
		destroyItemsOnEnd();
		
		// Send message on end.
		if (!_endMsg.isEmpty())
		{
			Broadcast.toAllOnlinePlayers(_endMsg);
		}
		
		// Remove announce for entering players.
		if (_enterAnnounceId != -1)
		{
			AnnouncementsTable.getInstance().deleteAnnouncement(_enterAnnounceId);
		}
	}
	
	protected void destroyItemsOnEnd()
	{
		if (!_destroyItemsOnEnd.isEmpty())
		{
			foreach (int itemId in _destroyItemsOnEnd)
			{
				// Remove item from online players.
				foreach (Player player in World.getInstance().getPlayers())
				{
					if (player != null)
					{
						player.destroyItemByItemId(_eventName, itemId, -1, player, true);
					}
				}
				
				// Update database.
				try
				{
					using GameServerDbContext ctx = DbFactory.Instance.CreateDbContext();
					ctx.Items.Where(r => r.ItemId == itemId).ExecuteDelete();
				}
				catch (Exception e)
				{
					LOGGER.Error(e);
				}
			}
		}
	}
	
	public DateRange getEventPeriod()
	{
		return _eventPeriod;
	}
	
	/**
	 * @return {@code true} if now is event period
	 */
	public bool isEventPeriod()
	{
		return _active;
	}
}
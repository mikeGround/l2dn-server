﻿using L2Dn.GameServer.Data.Xml;
using L2Dn.GameServer.Model.Actor;
using L2Dn.GameServer.Model.Actor.Templates;
using L2Dn.GameServer.Model.Geo;
using L2Dn.GameServer.Model.Interfaces;
using L2Dn.GameServer.Model.Spawns;
using L2Dn.GameServer.TaskManagers;
using L2Dn.GameServer.Utilities;
using NLog;

namespace L2Dn.GameServer.Model;

/**
 * This class manages the spawn and respawn of a group of Npcs that are in the same are and have the same type.<br>
 * <b><u>Concept</u>:</b><br>
 * Npc can be spawned either in a random position into a location area (if Lox=0 and Locy=0), either at an exact position.<br>
 * The heading of the Npc can be a random heading if not defined (value= -1) or an exact heading (ex : merchant...).
 * @author Nightmare
 */
public class Spawn : Location, IIdentifiable, INamable
{
	protected static readonly Logger LOGGER = LogManager.GetLogger(nameof(Spawn));
	
	/** String identifier of this spawn */
	private String _name;
	/** The link on the NpcTemplate object containing generic and static properties of this spawn (ex : RewardExp, RewardSP, AggroRange...) */
	private NpcTemplate _template;
	/** The maximum number of Npc that can manage this Spawn */
	private int _maximumCount;
	/** The current number of Npc managed by this Spawn */
	private int _currentCount;
	/** The current number of SpawnTask in progress or stand by of this Spawn */
	public int _scheduledCount;
	/** The identifier of the location area where Npc can be spawned */
	private int _locationId;
	/** The spawn instance id */
	private int _instanceId = 0;
	/** Minimum respawn delay */
	private int _respawnMinDelay;
	/** Maximum respawn delay */
	private int _respawnMaxDelay;
	/** Respawn Pattern **/
	private SchedulingPattern _respawnPattern;
	/** The generic constructor of Npc managed by this Spawn */
	private Constructor<? extends Npc> _constructor;
	/** If True an Npc is respawned each time that another is killed */
	private bool _doRespawn = true;
	private readonly Deque<Npc> _spawnedNpcs = new ConcurrentLinkedDeque<>();
	private bool _randomWalk = false; // Is no random walk
	private NpcSpawnTemplate _spawnTemplate;
	
	/**
	 * Constructor of Spawn.<br>
	 * <br>
	 * <b><u>Concept</u>:</b><br>
	 * <br>
	 * Each Spawn owns generic and static properties (ex : RewardExp, RewardSP, AggroRange...).<br>
	 * All of those properties are stored in a different NpcTemplate for each type of Spawn. Each template is loaded once in the server cache memory (reduce memory use).<br>
	 * When a new instance of Spawn is created, server just create a link between the instance and the template.<br>
	 * This link is stored in <b>_template</b> Each Npc is linked to a Spawn that manages its spawn and respawn (delay, location...).<br>
	 * This link is stored in <b>_spawn</b> of the Npc.<br>
	 * <br>
	 * <b><u>Actions</u>:</b><br>
	 * <ul>
	 * <li>Set the _template of the Spawn</li>
	 * <li>Calculate the implementationName used to generate the generic constructor of Npc managed by this Spawn</li>
	 * <li>Create the generic constructor of Npc managed by this Spawn</li>
	 * </ul>
	 * @param template The NpcTemplate to link to this Spawn
	 * @throws ClassNotFoundException
	 * @throws NoSuchMethodException
	 * @throws ClassCastException when template type is not subclass of Npc
	 */
	public Spawn(NpcTemplate template): base(0, 0, -10000)
	{
		// Set the _template of the Spawn
		_template = template;
		
		if (_template == null)
		{
			return;
		}
		
		String className = "org.l2jmobius.gameserver.model.actor.instance." + _template.getType();
		
		// Create the generic constructor of Npc managed by this Spawn
		_constructor = Class.forName(className).asSubclass(Npc.class).getConstructor(NpcTemplate.class);
	}
	
	/**
	 * Creates a new spawn.
	 * @param npcId the NPC ID
	 * @throws ClassNotFoundException
	 * @throws NoSuchMethodException
	 * @throws ClassCastException
	 */
	public Spawn(int npcId) : base(0, 0, -10000)
	{
		_template = Objects.requireNonNull(NpcData.getInstance().getTemplate(npcId), "NpcTemplate not found for NPC ID: " + npcId);
		
		string className = "org.l2jmobius.gameserver.model.actor.instance." + _template.getType();
		
		// Create the generic constructor of Npc managed by this Spawn
		_constructor = Class.forName(className).asSubclass(Npc.class).getConstructor(NpcTemplate.class);
	}
	
	/**
	 * @return the maximum number of Npc that this Spawn can manage.
	 */
	public int getAmount()
	{
		return _maximumCount;
	}
	
	/**
	 * @return the String Identifier of this spawn.
	 */
	public String getName()
	{
		return _name;
	}
	
	/**
	 * Set the String Identifier of this spawn.
	 * @param name
	 */
	public void setName(String name)
	{
		_name = name;
	}
	
	/**
	 * @return the Identifier of the location area where Npc can be spawned.
	 */
	public int getLocationId()
	{
		return _locationId;
	}
	
	/**
	 * Gets the NPC ID.
	 * @return the NPC ID
	 */
	public int getId()
	{
		return _template.getId();
	}
	
	/**
	 * @return min respawn delay.
	 */
	public int getRespawnMinDelay()
	{
		return _respawnMinDelay;
	}
	
	/**
	 * @return max respawn delay.
	 */
	public int getRespawnMaxDelay()
	{
		return _respawnMaxDelay;
	}
	
	/**
	 * @return respawn pattern
	 */
	public SchedulingPattern getRespawnPattern()
	{
		return _respawnPattern;
	}
	
	/**
	 * Set the maximum number of Npc that this Spawn can manage.
	 * @param amount
	 */
	public void setAmount(int amount)
	{
		_maximumCount = amount;
	}
	
	/**
	 * Set the Identifier of the location area where Npc can be spawned.
	 * @param id
	 */
	public void setLocationId(int id)
	{
		_locationId = id;
	}
	
	/**
	 * Set Minimum Respawn Delay.
	 * @param date
	 */
	public void setRespawnMinDelay(int date)
	{
		_respawnMinDelay = date;
	}
	
	/**
	 * Set Maximum Respawn Delay.
	 * @param date
	 */
	public void setRespawnMaxDelay(int date)
	{
		_respawnMaxDelay = date;
	}
	
	/**
	 * Decrease the current number of Npc of this Spawn and if necessary create a SpawnTask to launch after the respawn Delay. <b><u>Actions</u>:</b>
	 * <li>Decrease the current number of Npc of this Spawn</li>
	 * <li>Check if respawn is possible to prevent multiple respawning caused by lag</li>
	 * <li>Update the current number of SpawnTask in progress or stand by of this Spawn</li>
	 * <li>Create a new SpawnTask to launch after the respawn Delay</li> <font color=#FF0000><b><u>Caution</u>: A respawn is possible ONLY if _doRespawn=True and _scheduledCount + _currentCount < _maximumCount</b></font>
	 * @param oldNpc
	 */
	public void decreaseCount(Npc oldNpc)
	{
		// sanity check
		if (_currentCount <= 0)
		{
			return;
		}
		
		// Decrease the current number of Npc of this Spawn
		_currentCount--;
		
		// Remove this NPC from list of spawned
		_spawnedNpcs.remove(oldNpc);
		
		// Check if respawn is possible to prevent multiple respawning caused by lag
		if (_doRespawn && ((_scheduledCount + _currentCount) < _maximumCount))
		{
			// Update the current number of SpawnTask in progress or stand by of this Spawn
			_scheduledCount++;
			
			// Schedule the next respawn.
			RespawnTaskManager.getInstance().add(oldNpc, System.currentTimeMillis() + (hasRespawnRandom() ? Rnd.get(_respawnMinDelay, _respawnMaxDelay) : _respawnMinDelay));
		}
	}
	
	/**
	 * Create the initial spawning and set _doRespawn to False, if respawn time set to 0, or set it to True otherwise.
	 * @return The number of Npc that were spawned
	 */
	public int init()
	{
		while (_currentCount < _maximumCount)
		{
			doSpawn();
		}
		_doRespawn = _respawnMinDelay > 0;
		
		return _currentCount;
	}
	
	/**
	 * @return true if respawn enabled
	 */
	public bool isRespawnEnabled()
	{
		return _doRespawn;
	}
	
	/**
	 * Set _doRespawn to False to stop respawn in this Spawn.
	 */
	public void stopRespawn()
	{
		_doRespawn = false;
	}
	
	/**
	 * Set _doRespawn to True to start or restart respawn in this Spawn.
	 */
	public void startRespawn()
	{
		_doRespawn = true;
	}
	
	public Npc doSpawn()
	{
		return _doRespawn ? doSpawn(false) : null;
	}
	
	/**
	 * Create the Npc, add it to the world and lauch its OnSpawn action.<br>
	 * <br>
	 * <b><u>Concept</u>:</b><br>
	 * <br>
	 * Npc can be spawned either in a random position into a location area (if Lox=0 and Locy=0), either at an exact position.<br>
	 * The heading of the Npc can be a random heading if not defined (value= -1) or an exact heading (ex : merchant...).<br>
	 * <br>
	 * <b><u>Actions for an random spawn into location area</u>:<i> (if Locx=0 and Locy=0)</i></b>
	 * <ul>
	 * <li>Get Npc Init parameters and its generate an Identifier</li>
	 * <li>Call the constructor of the Npc</li>
	 * <li>Calculate the random position in the location area (if Locx=0 and Locy=0) or get its exact position from the Spawn</li>
	 * <li>Set the position of the Npc</li>
	 * <li>Set the HP and MP of the Npc to the max</li>
	 * <li>Set the heading of the Npc (random heading if not defined : value=-1)</li>
	 * <li>Link the Npc to this Spawn</li>
	 * <li>Init other values of the Npc (ex : from its CreatureTemplate for INT, STR, DEX...) and add it in the world</li>
	 * <li>Launch the action OnSpawn fo the Npc</li>
	 * <li>Increase the current number of Npcs managed by this Spawn</li>
	 * </ul>
	 * @param isSummonSpawn
	 * @return
	 */
	public Npc doSpawn(bool isSummonSpawn)
	{
		try
		{
			// Check if the spawn is not a Pet, Decoy or Trap spawn.
			if (_template.isType("Pet") || _template.isType("Decoy") || _template.isType("Trap"))
			{
				_currentCount++;
				return null;
			}
			
			// Call the constructor of the Npc
			Npc npc = _constructor.newInstance(_template);
			npc.setInstanceById(_instanceId); // Must be done before object is spawned into visible world
			if (isSummonSpawn)
			{
				npc.setShowSummonAnimation(isSummonSpawn);
			}
			
			return initializeNpc(npc);
		}
		catch (Exception e)
		{
			LOGGER.log(Level.WARNING, "Error while spawning " + _template.getId(), e);
		}
		return null;
	}
	
	/**
	 * @param npc
	 * @return
	 */
	private Npc initializeNpc(Npc npc)
	{
		int newlocx = 0;
		int newlocy = 0;
		int newlocz = -10000;
		
		// If Locx and Locy are not defined, the Npc must be spawned in an area defined by location or spawn territory.
		if (_spawnTemplate != null)
		{
			Location loc = _spawnTemplate.getSpawnLocation();
			newlocx = loc.getX();
			newlocy = loc.getY();
			newlocz = loc.getZ();
			setLocation(loc);
		}
		else if ((getX() == 0) && (getY() == 0))
		{
			LOGGER.Warn("NPC " + npc + " doesn't have spawn location!");
			return null;
		}
		else
		{
			// The Npc is spawned at the exact position (Lox, Locy, Locz)
			newlocx = getX();
			newlocy = getY();
			newlocz = getZ();
		}
		
		// Check if npc is in water.
		WaterZone water = ZoneManager.getInstance().getZone(newlocx, newlocy, newlocz, WaterZone.class);
		
		// If random spawn system is enabled.
		if (Config.ENABLE_RANDOM_MONSTER_SPAWNS && (getHeading() != -1) && npc.isMonster() && !npc.isQuestMonster() && !WalkingManager.getInstance().isTargeted(npc) && (getInstanceId() == 0) && !getTemplate().isUndying() && !npc.isRaid() && !npc.isRaidMinion() && !npc.isFlying() && (water == null) && !Config.MOBS_LIST_NOT_RANDOM.contains(npc.getId()))
		{
			int randX = newlocx + Rnd.get(Config.MOB_MIN_SPAWN_RANGE, Config.MOB_MAX_SPAWN_RANGE);
			int randY = newlocy + Rnd.get(Config.MOB_MIN_SPAWN_RANGE, Config.MOB_MAX_SPAWN_RANGE);
			if (GeoEngine.getInstance().canMoveToTarget(newlocx, newlocy, newlocz, randX, randY, newlocz, npc.getInstanceWorld()) //
				&& GeoEngine.getInstance().canSeeTarget(newlocx, newlocy, newlocz, randX, randY, newlocz, npc.getInstanceWorld()))
			{
				newlocx = randX;
				newlocy = randY;
				setXYZ(newlocx, newlocy, newlocz);
				setHeading(-1);
			}
		}
		
		// Correct Z of monsters.
		if (npc.isMonster() && !npc.isFlying() && (water == null))
		{
			// Do not correct Z distances greater than 300.
			int geoZ = GeoEngine.getInstance().getHeight(newlocx, newlocy, newlocz);
			if (Util.calculateDistance(newlocx, newlocy, newlocz, newlocx, newlocy, geoZ, true, true) < 300)
			{
				newlocz = geoZ;
			}
		}
		
		// Set is not random walk default value
		npc.setRandomWalking(_randomWalk);
		
		// Set the heading of the Npc (random heading if not defined)
		if (getHeading() == -1)
		{
			npc.setHeading(Rnd.get(61794));
		}
		else
		{
			npc.setHeading(getHeading());
		}
		
		// Set custom Npc server side name and title
		if (npc.getTemplate().isUsingServerSideName())
		{
			npc.setName(npc.getTemplate().getName());
		}
		if (npc.getTemplate().isUsingServerSideTitle())
		{
			npc.setTitle(npc.getTemplate().getTitle());
		}
		
		// Reset some variables
		npc.onRespawn();
		
		// Link the Npc to this Spawn
		npc.setSpawn(this);
		
		// Spawn NPC
		npc.spawnMe(newlocx, newlocy, newlocz);
		
		// Make sure info is broadcasted in instances
		if (npc.getInstanceId() > 0)
		{
			npc.broadcastInfo();
		}
		
		if (_spawnTemplate != null)
		{
			_spawnTemplate.notifySpawnNpc(npc);
		}
		
		_spawnedNpcs.add(npc);
		
		// Increase the current number of Npcs managed by this Spawn
		_currentCount++;
		
		// Minions
		if (npc.isMonster() && NpcData.getMasterMonsterIDs().contains(npc.getId()))
		{
			((Monster) npc).getMinionList().spawnMinions(npc.getParameters().getMinionList("Privates"));
		}
		
		return npc;
	}
	
	/**
	 * Set bounds for random calculation and delay for respawn
	 * @param delay delay in seconds
	 * @param randomInterval random interval in seconds
	 */
	public void setRespawnDelay(TimeSpan delay, TimeSpan randomInterval)
	{
		if (delay != TimeSpan.Zero)
		{
			if (delay < TimeSpan.Zero)
			{
				LOGGER.Error("respawn delay is negative for spawn:" + this);
			}
			
			TimeSpan minDelay = delay - randomInterval;
			TimeSpan maxDelay = delay + randomInterval;
			
			_respawnMinDelay = Math.Max(10, minDelay) * 1000;
			_respawnMaxDelay = Math.Max(10, maxDelay) * 1000;
		}
		else
		{
			_respawnMinDelay = 0;
			_respawnMaxDelay = 0;
		}
	}
	
	public void setRespawnPattern(SchedulingPattern respawnPattern)
	{
		_respawnPattern = respawnPattern;
	}
	
	public void setRespawnDelay(TimeSpan delay)
	{
		setRespawnDelay(delay, 0);
	}
	
	public int getRespawnDelay()
	{
		return (_respawnMinDelay + _respawnMaxDelay) / 2;
	}
	
	public bool hasRespawnRandom()
	{
		return _respawnMinDelay != _respawnMaxDelay;
	}
	
	public int getChaseRange()
	{
		if (_spawnTemplate == null)
		{
			return 0;
		}
		return _spawnTemplate.getChaseRange();
	}
	
	public Npc getLastSpawn()
	{
		if (!_spawnedNpcs.isEmpty())
		{
			return _spawnedNpcs.peekLast();
		}
		
		return null;
	}
	
	public bool deleteLastNpc()
	{
		return !_spawnedNpcs.isEmpty() && _spawnedNpcs.getLast().deleteMe();
	}
	
	public Deque<Npc> getSpawnedNpcs()
	{
		return _spawnedNpcs;
	}
	
	public void respawnNpc(Npc oldNpc)
	{
		if (_doRespawn)
		{
			// oldNpc.refreshID();
			initializeNpc(oldNpc);
			
			// Register NPC back to instance world.
			Instance instance = oldNpc.getInstanceWorld();
			if (instance != null)
			{
				instance.addNpc(oldNpc);
			}
		}
	}
	
	public NpcTemplate getTemplate()
	{
		return _template;
	}
	
	public int getInstanceId()
	{
		return _instanceId;
	}
	
	public void setInstanceId(int instanceId)
	{
		_instanceId = instanceId;
	}
	
	public bool getRandomWalking()
	{
		return _randomWalk;
	}
	
	public void setRandomWalking(bool value)
	{
		_randomWalk = value;
	}
	
	public void setSpawnTemplate(NpcSpawnTemplate npcSpawnTemplate)
	{
		_spawnTemplate = npcSpawnTemplate;
	}
	
	public NpcSpawnTemplate getNpcSpawnTemplate()
	{
		return _spawnTemplate;
	}
	
	public override string ToString()
	{
		return "Spawn ID: " + _template.getId() + " X: " + getX() + " Y: " + getY() + " Z: " + getZ() + " Heading: " + getHeading();
	}
}

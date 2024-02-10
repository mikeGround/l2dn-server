﻿using System.Globalization;
using System.Text;
using L2Dn.GameServer.Cache;
using L2Dn.GameServer.Data.Xml;
using L2Dn.GameServer.Enums;
using L2Dn.GameServer.Handlers;
using L2Dn.GameServer.InstanceManagers;
using L2Dn.GameServer.Model.Actor.Instances;
using L2Dn.GameServer.Model.Actor.Stats;
using L2Dn.GameServer.Model.Actor.Status;
using L2Dn.GameServer.Model.Actor.Tasks.NpcTasks;
using L2Dn.GameServer.Model.Actor.Templates;
using L2Dn.GameServer.Model.Events;
using L2Dn.GameServer.Model.Events.Impl.Creatures.Npcs;
using L2Dn.GameServer.Model.Events.Returns;
using L2Dn.GameServer.Model.Events.Timers;
using L2Dn.GameServer.Model.Holders;
using L2Dn.GameServer.Model.InstanceZones;
using L2Dn.GameServer.Model.Items;
using L2Dn.GameServer.Model.Items.Instances;
using L2Dn.GameServer.Model.Olympiads;
using L2Dn.GameServer.Model.Quests;
using L2Dn.GameServer.Model.Residences;
using L2Dn.GameServer.Model.Sieges;
using L2Dn.GameServer.Model.Skills;
using L2Dn.GameServer.Model.Spawns;
using L2Dn.GameServer.Model.Stats;
using L2Dn.GameServer.Model.Variables;
using L2Dn.GameServer.Model.Zones;
using L2Dn.GameServer.Model.Zones.Types;
using L2Dn.GameServer.Network.Enums;
using L2Dn.GameServer.Network.OutgoingPackets;
using L2Dn.GameServer.TaskManagers;
using L2Dn.GameServer.Utilities;
using FortManager = L2Dn.GameServer.InstanceManagers.FortManager;
using ThreadPool = System.Threading.ThreadPool;

namespace L2Dn.GameServer.Model.Actor;

/**
 * This class represents a Non-Player-Creature in the world.<br>
 * It can be a monster or a friendly creature.<br>
 * It uses a template to fetch some static values.
 */
public class Npc: Creature
{
	/** The interaction distance of the Npc(is used as offset in MovetoLocation method) */
	public const int INTERACTION_DISTANCE = 250;
	/** Maximum distance where the drop may appear given this NPC position. */
	public const int RANDOM_ITEM_DROP_LIMIT = 70;
	/** Ids of NPCs that see creatures through the OnCreatureSee event. */
	private static readonly Set<int> CREATURE_SEE_IDS = new();
	/** The Spawn object that manage this Npc */
	private Spawn _spawn;
	/** The flag to specify if this Npc is busy */
	private bool _isBusy = false;
	/** True if endDecayTask has already been called */
	private volatile bool _isDecayed = false;
	/** True if this Npc is autoattackable **/
	private bool _isAutoAttackable = false;
	/** Time of last social packet broadcast */
	private long _lastSocialBroadcast = 0;
	/** Minimum interval between social packets */
	private const int MINIMUM_SOCIAL_INTERVAL = 6000;
	/** Support for random animation switching */
	private bool _isRandomAnimationEnabled = true;
	private bool _isRandomWalkingEnabled = true;
	private bool _isWalker = false;
	private bool _isTalkable = getTemplate().isTalkable();
	private readonly bool _isQuestMonster = getTemplate().isQuestMonster();
	private readonly bool _isFakePlayer = getTemplate().isFakePlayer();
	
	private int _currentLHandId; // normally this shouldn't change from the template, but there exist exceptions
	private int _currentRHandId; // normally this shouldn't change from the template, but there exist exceptions
	private int _currentEnchant; // normally this shouldn't change from the template, but there exist exceptions
	private float _currentCollisionHeight; // used for npc grow effect skills
	private float _currentCollisionRadius; // used for npc grow effect skills
	
	private int _soulshotamount = 0;
	private int _spiritshotamount = 0;
	private int _displayEffect = 0;
	
	private int _killingBlowWeaponId;
	
	private int _cloneObjId; // Used in NpcInfo packet to clone the specified player.
	private int _clanId; // Used in NpcInfo packet to show the specified clan.
	
	private NpcStringId _titleString;
	private NpcStringId _nameString;
	
	private StatSet _params;
	private volatile int _scriptValue = 0;
	private RaidBossStatus _raidStatus;
	
	/** Contains information about local tax payments. */
	private TaxZone _taxZone = null;
	
	private readonly List<QuestTimer> _questTimers = new();
	private readonly List<TimerHolder<?>> _timerHolders = new();
	
	/**
	 * Constructor of Npc (use Creature constructor).<br>
	 * <br>
	 * <b><u>Actions</u>:</b>
	 * <ul>
	 * <li>Call the Creature constructor to set the _template of the Creature (copy skills from template to object and link _calculators to NPC_STD_CALCULATOR)</li>
	 * <li>Set the name of the Creature</li>
	 * <li>Create a RandomAnimation Task that will be launched after the calculated delay if the server allow it</li>
	 * </ul>
	 * @param template The NpcTemplate to apply to the NPC
	 */
	public Npc(NpcTemplate template): base(template)
	{
		// Call the Creature constructor to set the _template of the Creature, copy skills from template to object
		// and link _calculators to NPC_STD_CALCULATOR
		setInstanceType(InstanceType.Npc);
		initCharStatusUpdateValues();
		setTargetable(getTemplate().isTargetable());
		
		// initialize the "current" equipment
		_currentLHandId = getTemplate().getLHandId();
		_currentRHandId = getTemplate().getRHandId();
		_currentEnchant = Config.ENABLE_RANDOM_ENCHANT_EFFECT ? Rnd.get(4, 21) : getTemplate().getWeaponEnchant();
		
		// initialize the "current" collisions
		_currentCollisionHeight = getTemplate().getFCollisionHeight();
		_currentCollisionRadius = getTemplate().getFCollisionRadius();
		setFlying(template.isFlying());
		initStatusUpdateCache();
	}
	
	/**
	 * Send a packet SocialAction to all Player in the _KnownPlayers of the Npc and create a new RandomAnimation Task.
	 * @param animationId
	 */
	public void onRandomAnimation(int animationId)
	{
		// Send a packet SocialAction to all Player in the _KnownPlayers of the Npc
		long now = System.currentTimeMillis();
		if ((now - _lastSocialBroadcast) > MINIMUM_SOCIAL_INTERVAL)
		{
			_lastSocialBroadcast = now;
			broadcastSocialAction(animationId);
		}
	}
	
	/**
	 * @return true if the server allows Random Animation.
	 */
	public virtual bool hasRandomAnimation()
	{
		return ((Config.MAX_NPC_ANIMATION > 0) && _isRandomAnimationEnabled && (getAiType() != AIType.CORPSE));
	}
	
	/**
	 * Switches random Animation state into val.
	 * @param value needed state of random animation
	 */
	public void setRandomAnimation(bool value)
	{
		_isRandomAnimationEnabled = value;
	}
	
	/**
	 * @return {@code true}, if random animation is enabled, {@code false} otherwise.
	 */
	public bool isRandomAnimationEnabled()
	{
		return _isRandomAnimationEnabled;
	}
	
	public void setRandomWalking(bool enabled)
	{
		_isRandomWalkingEnabled = enabled;
	}
	
	public bool isRandomWalkingEnabled()
	{
		return _isRandomWalkingEnabled;
	}
	
	public override NpcStat getStat()
	{
		return (NpcStat)base.getStat();
	}
	
	public override void initCharStat()
	{
		setStat(new NpcStat(this));
	}
	
	public override NpcStatus getStatus()
	{
		return (NpcStatus) base.getStatus();
	}
	
	public override void initCharStatus()
	{
		setStatus(new NpcStatus(this));
	}
	
	/** Return the NpcTemplate of the Npc. */
	public override NpcTemplate getTemplate()
	{
		return (NpcTemplate)base.getTemplate();
	}
	
	/**
	 * Gets the NPC ID.
	 * @return the NPC ID
	 */
	public override int getId()
	{
		return getTemplate().getId();
	}
	
	public override bool canBeAttacked()
	{
		return Config.ALT_ATTACKABLE_NPCS;
	}
	
	/**
	 * Return the Level of this Npc contained in the NpcTemplate.
	 */
	public override int getLevel()
	{
		return getTemplate().getLevel();
	}
	
	/**
	 * @return false.
	 */
	public bool isAggressive()
	{
		return false;
	}
	
	/**
	 * @return the Aggro Range of this Npc either contained in the NpcTemplate, or overriden by spawnlist AI value.
	 */
	public virtual int getAggroRange()
	{
		return getTemplate().getAggroRange();
	}
	
	/**
	 * @param npc
	 * @return if both npcs have the same clan by template.
	 */
	public bool isInMyClan(Npc npc)
	{
		return getTemplate().isClan(npc.getTemplate().getClans());
	}
	
	/**
	 * Return True if this Npc is undead in function of the NpcTemplate.
	 */
	public override bool isUndead()
	{
		return getTemplate().getRace() == Race.UNDEAD;
	}
	
	/**
	 * Send a packet NpcInfo with state of abnormal effect to all Player in the _KnownPlayers of the Npc.
	 */
	public override void updateAbnormalVisualEffects()
	{
		World.getInstance().forEachVisibleObject<Player>(this, player =>
		{
			if (!isVisibleFor(player))
			{
				return;
			}
			
			if (_isFakePlayer)
			{
				player.sendPacket(new FakePlayerInfo(this));
			}
			else if (getRunSpeed() == 0)
			{
				player.sendPacket(new ServerObjectInfo(this, player));
			}
			else
			{
				player.sendPacket(new NpcInfoAbnormalVisualEffect(this));
			}
		});
	}
	
	public override bool isAutoAttackable(Creature attacker)
	{
		if (attacker == null)
		{
			return false;
		}
		
		// Summons can attack NPCs.
		if (attacker.isSummon())
		{
			return true;
		}
		
		if (!isTargetable())
		{
			return false;
		}
		
		if (attacker.isAttackable())
		{
			if (isInMyClan((Npc) attacker))
			{
				return false;
			}
			
			// Chaos NPCs attack everything except clan.
			if (((NpcTemplate) attacker.getTemplate()).isChaos())
			{
				return true;
			}
			
			// Usually attackables attack everything they hate.
			return ((Attackable) attacker).getHating(this) > 0;
		}
		
		return _isAutoAttackable;
	}
	
	public void setAutoAttackable(bool flag)
	{
		_isAutoAttackable = flag;
	}
	
	/**
	 * @return the Identifier of the item in the left hand of this Npc contained in the NpcTemplate.
	 */
	public int getLeftHandItem()
	{
		return _currentLHandId;
	}
	
	/**
	 * @return the Identifier of the item in the right hand of this Npc contained in the NpcTemplate.
	 */
	public int getRightHandItem()
	{
		return _currentRHandId;
	}
	
	public int getEnchantEffect()
	{
		return _currentEnchant;
	}
	
	/**
	 * @return the busy status of this Npc.
	 */
	public bool isBusy()
	{
		return _isBusy;
	}
	
	/**
	 * @param isBusy the busy status of this Npc
	 */
	public void setBusy(bool isBusy)
	{
		_isBusy = isBusy;
	}
	
	/**
	 * @return true if this Npc instance can be warehouse manager.
	 */
	public virtual bool isWarehouse()
	{
		return false;
	}
	
	public bool canTarget(Player player)
	{
		if (player.isControlBlocked())
		{
			player.sendPacket(ActionFailedPacket.STATIC_PACKET);
			return false;
		}
		if (player.isLockedTarget() && (player.getLockedTarget() != this))
		{
			player.sendPacket(SystemMessageId.FAILED_TO_CHANGE_ENMITY);
			player.sendPacket(ActionFailedPacket.STATIC_PACKET);
			return false;
		}
		return true;
	}
	
	public bool canInteract(Player player)
	{
		if (player.isCastingNow())
		{
			return false;
		}
		else if (player.isDead() || player.isFakeDeath())
		{
			return false;
		}
		else if (player.isSitting())
		{
			return false;
		}
		else if (player.getPrivateStoreType() != PrivateStoreType.NONE)
		{
			return false;
		}
		else if (!isInsideRadius3D(player, INTERACTION_DISTANCE))
		{
			return false;
		}
		else if (player.getInstanceWorld() != getInstanceWorld())
		{
			return false;
		}
		else if (_isBusy)
		{
			return false;
		}
		return true;
	}
	
	/**
	 * Set another tax zone which will be used for tax payments.
	 * @param zone newly entered tax zone
	 */
	public void setTaxZone(TaxZone zone)
	{
		_taxZone = ((zone != null) && !isInInstance()) ? zone : null;
	}
	
	/**
	 * Gets castle for tax payments.
	 * @return instance of {@link Castle} when NPC is inside {@link TaxZone} otherwise {@code null}
	 */
	public Castle getTaxCastle()
	{
		return (_taxZone != null) ? _taxZone.getCastle() : null;
	}
	
	/**
	 * Gets castle tax rate
	 * @param type type of tax
	 * @return tax rate when NPC is inside tax zone otherwise {@code 0}
	 */
	public double getCastleTaxRate(TaxType type)
	{
		Castle castle = getTaxCastle();
		return (castle != null) ? (castle.getTaxPercent(type) / 100.0) : 0;
	}
	
	/**
	 * Increase castle vault by specified tax amount.
	 * @param amount tax amount
	 */
	public void handleTaxPayment(long amount)
	{
		Castle taxCastle = getTaxCastle();
		if (taxCastle != null)
		{
			taxCastle.addToTreasury(amount);
		}
	}
	
	/**
	 * @return the nearest Castle this Npc belongs to. Otherwise null.
	 */
	public Castle getCastle()
	{
		return CastleManager.getInstance().findNearestCastle(this);
	}
	
	public ClanHall getClanHall()
	{
		if (getId() == 33360) // Provisional Hall Manager
		{
			foreach (ZoneType zone in ZoneManager.getInstance().getZones(this))
			{
				if (zone is ClanHallZone)
				{
					ClanHall clanHall = ClanHallData.getInstance().getClanHallById(((ClanHallZone) zone).getResidenceId());
					if (clanHall != null)
					{
						return clanHall;
					}
				}
			}
		}
		return ClanHallData.getInstance().getClanHallByNpcId(getId());
	}
	
	/**
	 * Return closest castle in defined distance
	 * @param maxDistance long
	 * @return Castle
	 */
	public Castle getCastle(long maxDistance)
	{
		return CastleManager.getInstance().findNearestCastle(this, maxDistance);
	}
	
	/**
	 * @return the nearest Fort this Npc belongs to. Otherwise null.
	 */
	public Fort getFort()
	{
		return FortManager.getInstance().findNearestFort(this);
	}
	
	/**
	 * Return closest Fort in defined distance
	 * @param maxDistance long
	 * @return Fort
	 */
	public Fort getFort(long maxDistance)
	{
		return FortManager.getInstance().findNearestFort(this, maxDistance);
	}
	
	/**
	 * Open a quest or chat window on client with the text of the Npc in function of the command.<br>
	 * <br>
	 * <b><u>Example of use</u>:</b>
	 * <ul>
	 * <li>Client packet : RequestBypassToServer</li>
	 * </ul>
	 * @param player
	 * @param command The command string received from client
	 */
	public virtual void onBypassFeedback(Player player, String command)
	{
		if (canInteract(player))
		{
			IBypassHandler handler = BypassHandler.getInstance().getHandler(command);
			if (handler != null)
			{
				handler.useBypass(command, player, this);
			}
			else
			{
				LOGGER.Info(GetType().Name + ": Unknown NPC bypass: \"" + command + "\" NpcId: " + getId());
			}
		}
	}
	
	/**
	 * Return null (regular NPCs don't have weapons instances).
	 */
	public override Item getActiveWeaponInstance()
	{
		return null;
	}
	
	/**
	 * Return the weapon item equipped in the right hand of the Npc or null.
	 */
	public override Weapon getActiveWeaponItem()
	{
		return null;
	}
	
	/**
	 * Return null (regular NPCs don't have weapons instances).
	 */
	public override Item getSecondaryWeaponInstance()
	{
		return null;
	}
	
	/**
	 * Return the weapon item equipped in the left hand of the Npc or null.
	 */
	public override Weapon getSecondaryWeaponItem()
	{
		return null;
	}
	
	/**
	 * <b><U Format of the pathfile</u>:</b>
	 * <ul>
	 * <li>if the file exists on the server (page number = 0) : <b>data/html/default/12006.htm</b> (npcId-page number)</li>
	 * <li>if the file exists on the server (page number > 0) : <b>data/html/default/12006-1.htm</b> (npcId-page number)</li>
	 * <li>if the file doesn't exist on the server : <b>data/html/npcdefault.htm</b> (message : "I have nothing to say to you")</li>
	 * </ul>
	 * @param npcId The Identifier of the Npc whose text must be display
	 * @param value The number of the page to display
	 * @param player The player that speaks to this NPC
	 * @return the pathfile of the selected HTML file in function of the npcId and of the page number.
	 */
	public virtual String getHtmlPath(int npcId, int value, Player player)
	{
		String pom = "";
		if (value == 0)
		{
			pom = npcId.ToString(CultureInfo.InvariantCulture);
		}
		else
		{
			pom = npcId + "-" + value;
		}
		
		String temp = "data/html/default/" + pom + ".htm";
		if (Config.HTM_CACHE)
		{
			// If not running lazy cache the file must be in the cache or it does not exist
			if (HtmCache.getInstance().contains(temp))
			{
				return temp;
			}
		}
		else if (HtmCache.getInstance().isLoadable(temp))
		{
			return temp;
		}
		
		// If the file is not found, the standard message "I have nothing to say to you" is returned
		return "data/html/npcdefault.htm";
	}
	
	public void showChatWindow(Player player)
	{
		showChatWindow(player, 0);
	}
	
	/**
	 * Returns true if html exists
	 * @param player
	 * @param type
	 * @return bool
	 */
	private bool showPkDenyChatWindow(Player player, String type)
	{
		String html = HtmCache.getInstance().getHtm(player, "data/html/" + type + "/" + getId() + "-pk.htm");
		if (html != null)
		{
			html = html.Replace("%objectId%", getObjectId().ToString(CultureInfo.InvariantCulture));
			player.sendPacket(new NpcHtmlMessage(getObjectId(), html));
			player.sendPacket(ActionFailedPacket.STATIC_PACKET);
			return true;
		}
		return false;
	}
	
	/**
	 * Open a chat window on client with the text of the Npc.<br>
	 * <br>
	 * <b><u>Actions</u>:</b>
	 * <ul>
	 * <li>Get the text of the selected HTML file in function of the npcId and of the page number</li>
	 * <li>Send a Server->Client NpcHtmlMessage containing the text of the Npc to the Player</li>
	 * <li>Send a Server->Client ActionFailed to the Player in order to avoid that the client wait another packet</li>
	 * </ul>
	 * @param player The Player that talk with the Npc
	 * @param value The number of the page of the Npc to display
	 */
	public void showChatWindow(Player player, int value)
	{
		if (!_isTalkable)
		{
			player.sendPacket(ActionFailedPacket.STATIC_PACKET);
			return;
		}
		
		if (player.getReputation() < 0)
		{
			if (!Config.ALT_GAME_KARMA_PLAYER_CAN_SHOP && (this is Merchant))
			{
				if (showPkDenyChatWindow(player, "merchant"))
				{
					return;
				}
			}
			else if (!Config.ALT_GAME_KARMA_PLAYER_CAN_USE_GK && (this is Teleporter))
			{
				if (showPkDenyChatWindow(player, "teleporter"))
				{
					return;
				}
			}
			else if (!Config.ALT_GAME_KARMA_PLAYER_CAN_USE_WAREHOUSE && (this is Warehouse))
			{
				if (showPkDenyChatWindow(player, "warehouse"))
				{
					return;
				}
			}
			else if (!Config.ALT_GAME_KARMA_PLAYER_CAN_SHOP && (this is Fisherman))
			{
				if (showPkDenyChatWindow(player, "fisherman"))
				{
					return;
				}
			}
		}
		
		if (getTemplate().isType("Auctioneer") && (value == 0))
		{
			return;
		}
		
		int npcId = getTemplate().getId();
		String filename;
		switch (npcId)
		{
			case 31690:
			case 31769:
			case 31770:
			case 31771:
			case 31772:
			{
				if (player.isHero() || player.isNoble())
				{
					filename = Olympiad.OLYMPIAD_HTML_PATH + "hero_main.htm";
				}
				else
				{
					filename = (getHtmlPath(npcId, value, player));
				}
				break;
			}
			case 30298: // Blacksmith Pinter
			{
				if (player.isAcademyMember())
				{
					filename = (getHtmlPath(npcId, 1, player));
				}
				else
				{
					filename = (getHtmlPath(npcId, value, player));
				}
				break;
			}
			default:
			{
				if (((npcId >= 31093) && (npcId <= 31094)) || ((npcId >= 31172) && (npcId <= 31201)) || ((npcId >= 31239) && (npcId <= 31254)))
				{
					return;
				}
				// Get the text of the selected HTML file in function of the npcId and of the page number
				filename = (getHtmlPath(npcId, value, player));
				break;
			}
		}
		
		// Send a Server->Client NpcHtmlMessage containing the text of the Npc to the Player
		NpcHtmlMessage html = new NpcHtmlMessage(getObjectId());
		html.setFile(player, filename);
		html.replace("%npcname%", getName());
		html.replace("%objectId%", String.valueOf(getObjectId()));
		player.sendPacket(html);
		
		// Send a Server->Client ActionFailed to the Player in order to avoid that the client wait another packet
		player.sendPacket(ActionFailedPacket.STATIC_PACKET);
	}
	
	/**
	 * Open a chat window on client with the text specified by the given file name and path, relative to the datapack root.
	 * @param player The Player that talk with the Npc
	 * @param filename The filename that contains the text to send
	 */
	public void showChatWindow(Player player, String filename)
	{
		// Send a Server->Client NpcHtmlMessage containing the text of the Npc to the Player
		NpcHtmlMessage html = new NpcHtmlMessage(getObjectId());
		html.setFile(player, filename);
		html.replace("%objectId%", String.valueOf(getObjectId()));
		player.sendPacket(html);
		
		// Send a Server->Client ActionFailed to the Player in order to avoid that the client wait another packet
		player.sendPacket(ActionFailedPacket.STATIC_PACKET);
	}
	
	/**
	 * @return the Exp Reward of this Npc (modified by RATE_XP).
	 */
	public double getExpReward()
	{
		Instance instance = getInstanceWorld();
		float rateMul = instance != null ? instance.getExpRate() : Config.RATE_XP;
		return getTemplate().getExp() * rateMul;
	}
	
	/**
	 * @return the SP Reward of this Npc (modified by RATE_SP).
	 */
	public double getSpReward()
	{
		Instance instance = getInstanceWorld();
		float rateMul = instance != null ? instance.getSPRate() : Config.RATE_SP;
		return getTemplate().getSP() * rateMul;
	}
	
	public long getAttributeExp()
	{
		return getTemplate().getAttributeExp();
	}
	
	public override ElementalType getElementalSpiritType()
	{
		return getTemplate().getElementalType();
	}
	
	/**
	 * Kill the Npc (the corpse disappeared after 7 seconds).<br>
	 * <br>
	 * <b><u>Actions</u>:</b>
	 * <ul>
	 * <li>Create a DecayTask to remove the corpse of the Npc after 7 seconds</li>
	 * <li>Set target to null and cancel Attack or Cast</li>
	 * <li>Stop movement</li>
	 * <li>Stop HP/MP/CP Regeneration task</li>
	 * <li>Stop all active skills effects in progress on the Creature</li>
	 * <li>Send the Server->Client packet StatusUpdate with current HP and MP to all other Player to inform</li>
	 * <li>Notify Creature AI</li>
	 * </ul>
	 * @param killer The Creature who killed it
	 */
	public override bool doDie(Creature killer)
	{
		if (!base.doDie(killer))
		{
			return false;
		}
		
		// normally this wouldn't really be needed, but for those few exceptions,
		// we do need to reset the weapons back to the initial template weapon.
		_currentLHandId = getTemplate().getLHandId();
		_currentRHandId = getTemplate().getRHandId();
		_currentCollisionHeight = getTemplate().getFCollisionHeight();
		_currentCollisionRadius = getTemplate().getFCollisionRadius();
		
		Weapon weapon = (killer != null) ? killer.getActiveWeaponItem() : null;
		_killingBlowWeaponId = (weapon != null) ? weapon.getId() : 0;
		if (_isFakePlayer && (killer != null) && killer.isPlayable())
		{
			Player player = killer.getActingPlayer();
			if (isScriptValue(0) && (getReputation() >= 0))
			{
				if (Config.FAKE_PLAYER_KILL_KARMA)
				{
					player.setReputation(player.getReputation() - Formulas.calculateKarmaGain(player.getPkKills(), killer.isSummon()));
					player.setPkKills(player.getPkKills() + 1);
					player.broadcastUserInfo(UserInfoType.SOCIAL);
					player.checkItemRestriction();
					// pk item rewards
					if (Config.REWARD_PK_ITEM)
					{
						if (!(Config.DISABLE_REWARDS_IN_INSTANCES && (getInstanceId() != 0)) && //
							!(Config.DISABLE_REWARDS_IN_PVP_ZONES && isInsideZone(ZoneId.PVP)))
						{
							player.addItem("PK Item Reward", Config.REWARD_PK_ITEM_ID, Config.REWARD_PK_ITEM_AMOUNT, this, Config.REWARD_PK_ITEM_MESSAGE);
						}
					}
					// announce pk
					if (Config.ANNOUNCE_PK_PVP && !player.isGM())
					{
						String msg = Config.ANNOUNCE_PK_MSG.Replace("$killer", player.getName()).Replace("$target", getName());
						if (Config.ANNOUNCE_PK_PVP_NORMAL_MESSAGE)
						{
							SystemMessage sm = new SystemMessage(SystemMessageId.S1_3);
							sm.addString(msg);
							Broadcast.toAllOnlinePlayers(sm);
						}
						else
						{
							Broadcast.toAllOnlinePlayers(msg, false);
						}
					}
				}
			}
			else if (Config.FAKE_PLAYER_KILL_PVP)
			{
				player.setPvpKills(player.getPvpKills() + 1);
				player.setTotalKills(player.getTotalKills() + 1);
				player.broadcastUserInfo(UserInfoType.SOCIAL);
				// pvp item rewards
				if (Config.REWARD_PVP_ITEM)
				{
					if (!(Config.DISABLE_REWARDS_IN_INSTANCES && (getInstanceId() != 0)) && //
						!(Config.DISABLE_REWARDS_IN_PVP_ZONES && isInsideZone(ZoneId.PVP)))
					{
						player.addItem("PvP Item Reward", Config.REWARD_PVP_ITEM_ID, Config.REWARD_PVP_ITEM_AMOUNT, this, Config.REWARD_PVP_ITEM_MESSAGE);
					}
				}
				// announce pvp
				if (Config.ANNOUNCE_PK_PVP && !player.isGM())
				{
					String msg = Config.ANNOUNCE_PVP_MSG.Replace("$killer", player.getName()).Replace("$target", getName());
					if (Config.ANNOUNCE_PK_PVP_NORMAL_MESSAGE)
					{
						SystemMessage sm = new SystemMessage(SystemMessageId.S1_3);
						sm.addString(msg);
						Broadcast.toAllOnlinePlayers(sm);
					}
					else
					{
						Broadcast.toAllOnlinePlayers(msg, false);
					}
				}
			}
		}
		
		DecayTaskManager.getInstance().add(this);
		
		if (_spawn != null)
		{
			NpcSpawnTemplate npcTemplate = _spawn.getNpcSpawnTemplate();
			if (npcTemplate != null)
			{
				npcTemplate.notifyNpcDeath(this, killer);
			}
		}
		
		// Apply Mp Rewards
		if ((getTemplate().getMpRewardValue() > 0) && (killer != null) && killer.isPlayable())
		{
			Player killerPlayer = killer.getActingPlayer();
			new MpRewardTask(killerPlayer, this);
			foreach (Summon summon in killerPlayer.getServitors().values())
			{
				new MpRewardTask(summon, this);
			}
			if (getTemplate().getMpRewardAffectType() == MpRewardAffectType.PARTY)
			{
				Party party = killerPlayer.getParty();
				if (party != null)
				{
					foreach (Player member in party.getMembers())
					{
						if ((member != killerPlayer) && (member.calculateDistance3D(getX(), getY(), getZ()) <= Config.ALT_PARTY_RANGE))
						{
							new MpRewardTask(member, this);
							foreach (Summon summon in member.getServitors().values())
							{
								new MpRewardTask(summon, this);
							}
						}
					}
				}
			}
		}
		
		DBSpawnManager.getInstance().updateStatus(this, true);
		return true;
	}
	
	/**
	 * Set the spawn of the Npc.
	 * @param spawn The Spawn that manage the Npc
	 */
	public void setSpawn(Spawn spawn)
	{
		_spawn = spawn;
	}
	
	public override void onSpawn()
	{
		base.onSpawn();
		
		// Recharge shots
		_soulshotamount = getTemplate().getSoulShot();
		_spiritshotamount = getTemplate().getSpiritShot();
		_killingBlowWeaponId = 0;
		_isRandomAnimationEnabled = getTemplate().isRandomAnimationEnabled();
		_isRandomWalkingEnabled = !WalkingManager.getInstance().isTargeted(this) && getTemplate().isRandomWalkEnabled();
		if (isTeleporting())
		{
			if (EventDispatcher.getInstance().hasListener(EventType.ON_NPC_TELEPORT, this))
			{
				EventDispatcher.getInstance().notifyEventAsync(new OnNpcTeleport(this), this);
			}
		}
		else if (EventDispatcher.getInstance().hasListener(EventType.ON_NPC_SPAWN, this))
		{
			EventDispatcher.getInstance().notifyEventAsync(new OnNpcSpawn(this), this);
		}
		
		if (!isTeleporting())
		{
			WalkingManager.getInstance().onSpawn(this);
		}
		
		if (isInsideZone(ZoneId.TAX) && (getCastle() != null) && (Config.SHOW_CREST_WITHOUT_QUEST || getCastle().getShowNpcCrest()) && (getCastle().getOwnerId() != 0))
		{
			setClanId(getCastle().getOwnerId());
		}
		
		if (CREATURE_SEE_IDS.Contains(getId()))
		{
			initSeenCreatures();
		}
	}
	
	public static void addCreatureSeeId(int id)
	{
		CREATURE_SEE_IDS.Add(id);
	}
	
	/**
	 * Invoked when the NPC is re-spawned to reset the instance variables
	 */
	public void onRespawn()
	{
		// Make it alive
		setDead(false);
		
		// Stop all effects and recalculate stats without broadcasting.
		getEffectList().stopAllEffects(false);
		
		// Reset decay info
		setDecayed(false);
		
		// Fully heal npc and don't broadcast packet.
		setCurrentHp(getMaxHp(), false);
		setCurrentMp(getMaxMp(), false);
		
		// Clear script variables
		if (hasVariables())
		{
			getVariables().getSet().clear();
		}
		
		// Reset targetable state
		setTargetable(getTemplate().isTargetable());
		
		// Reset summoner
		setSummoner(null);
		
		// Reset summoned list
		resetSummonedNpcs();
		
		// Reset NpcStringId for name
		_nameString = null;
		
		// Reset NpcStringId for title
		_titleString = null;
		
		// Reset parameters
		_params = null;
	}
	
	/**
	 * Remove the Npc from the world and update its spawn object (for a complete removal use the deleteMe method).<br>
	 * <br>
	 * <b><u>Actions</u>:</b>
	 * <ul>
	 * <li>Remove the Npc from the world when the decay task is launched</li>
	 * <li>Decrease its spawn counter</li>
	 * <li>Manage Siege task (killFlag, killCT)</li>
	 * </ul>
	 * <font color=#FF0000><b><u>Caution</u>: This method DOESN'T REMOVE the object from _allObjects of World </b></font><br>
	 * <font color=#FF0000><b><u>Caution</u>: This method DOESN'T SEND Server->Client packets to players</b></font>
	 */
	public override void onDecay()
	{
		if (_isDecayed)
		{
			return;
		}
		setDecayed(true);
		
		// Remove the Npc from the world when the decay task is launched
		base.onDecay();
		
		// Decrease its spawn counter
		if ((_spawn != null) && !DBSpawnManager.getInstance().isDefined(getId()))
		{
			_spawn.decreaseCount(this);
		}
		
		// Notify Walking Manager
		WalkingManager.getInstance().onDeath(this);
		
		// Notify DP scripts
		if (EventDispatcher.getInstance().hasListener(EventType.ON_NPC_DESPAWN, this))
		{
			EventDispatcher.getInstance().notifyEventAsync(new OnNpcDespawn(this), this);
		}
		
		// Remove from instance world
		Instance instance = getInstanceWorld();
		if (instance != null)
		{
			instance.removeNpc(this);
		}
		
		// Stop all timers
		stopQuestTimers();
		stopTimerHolders();
		
		// Clear script value
		_scriptValue = 0;
	}
	
	/**
	 * Remove PROPERLY the Npc from the world.<br>
	 * <br>
	 * <b><u>Actions</u>:</b>
	 * <ul>
	 * <li>Remove the Npc from the world and update its spawn object</li>
	 * <li>Remove all WorldObject from _knownObjects and _knownPlayer of the Npc then cancel Attack or Cast and notify AI</li>
	 * <li>Remove WorldObject object from _allObjects of World</li>
	 * </ul>
	 * <font color=#FF0000><b><u>Caution</u>: This method DOESN'T SEND Server->Client packets to players</b></font><br>
	 * UnAfraid: TODO: Add Listener here
	 */
	public override bool deleteMe()
	{
		try
		{
			onDecay();
		}
		catch (Exception e)
		{
			LOGGER.Error("Failed decayMe()." + e);
		}
		
		if (isChannelized())
		{
			getSkillChannelized().abortChannelization();
		}
		
		ZoneManager.getInstance().getRegion(this).removeFromZones(this);
		
		return base.deleteMe();
	}
	
	/**
	 * @return the Spawn object that manage this Npc.
	 */
	public Spawn getSpawn()
	{
		return _spawn;
	}
	
	public bool isDecayed()
	{
		return _isDecayed;
	}
	
	public void setDecayed(bool decayed)
	{
		_isDecayed = decayed;
	}
	
	public void endDecayTask()
	{
		if (!_isDecayed)
		{
			DecayTaskManager.getInstance().cancel(this);
			onDecay();
		}
	}
	
	// Two functions to change the appearance of the equipped weapons on the NPC
	// This is only useful for a few NPCs and is most likely going to be called from AI
	public void setLHandId(int newWeaponId)
	{
		_currentLHandId = newWeaponId;
		broadcastInfo();
	}
	
	public void setRHandId(int newWeaponId)
	{
		_currentRHandId = newWeaponId;
		broadcastInfo();
	}
	
	public void setLRHandId(int newLWeaponId, int newRWeaponId)
	{
		_currentRHandId = newRWeaponId;
		_currentLHandId = newLWeaponId;
		broadcastInfo();
	}
	
	public void setEnchant(int newEnchantValue)
	{
		_currentEnchant = newEnchantValue;
		broadcastInfo();
	}
	
	public bool isShowName()
	{
		return getTemplate().isShowName();
	}
	
	public void setCollisionHeight(float height)
	{
		_currentCollisionHeight = height;
	}
	
	public void setCollisionRadius(float radius)
	{
		_currentCollisionRadius = radius;
	}
	
	public override float getCollisionHeight()
	{
		return _currentCollisionHeight;
	}
	
	public override float getCollisionRadius()
	{
		return _currentCollisionRadius;
	}
	
	public override void sendInfo(Player player)
	{
		if (isVisibleFor(player))
		{
			if (_isFakePlayer)
			{
				player.sendPacket(new FakePlayerInfo(this));
			}
			else if (getRunSpeed() == 0)
			{
				player.sendPacket(new ServerObjectInfo(this, player));
			}
			else
			{
				player.sendPacket(new NpcInfo(this));
			}
		}
	}
	
	public void scheduleDespawn(long delay)
	{
		ThreadPool.schedule(() =>
		{
			if (!_isDecayed)
			{
				deleteMe();
			}
		}, delay);
	}
	
	public override void notifyQuestEventSkillFinished(Skill skill, WorldObject target)
	{
		if ((target != null) && EventDispatcher.getInstance().hasListener(EventType.ON_NPC_SKILL_FINISHED, this))
		{
			EventDispatcher.getInstance().notifyEventAsync(new OnNpcSkillFinished(this, target.getActingPlayer(), skill), this);
		}
	}
	
	public override bool isMovementDisabled()
	{
		return base.isMovementDisabled() || !getTemplate().canMove() || (getAiType() == AIType.CORPSE);
	}
	
	public AIType getAiType()
	{
		return getTemplate().getAIType();
	}
	
	public void setDisplayEffect(int value)
	{
		if (value != _displayEffect)
		{
			_displayEffect = value;
			broadcastPacket(new ExChangeNpcState(getObjectId(), value));
		}
	}
	
	public bool hasDisplayEffect(int value)
	{
		return _displayEffect == value;
	}
	
	public int getDisplayEffect()
	{
		return _displayEffect;
	}
	
	public int getColorEffect()
	{
		return 0;
	}
	
	public override bool isNpc()
	{
		return true;
	}
	
	public void setTeam(Team team, bool broadcast)
	{
		base.setTeam(team);
		if (broadcast)
		{
			broadcastInfo();
		}
	}
	
	public override void setTeam(Team team)
	{
		base.setTeam(team);
		broadcastInfo();
	}
	
	public override bool isWalker()
	{
		return _isWalker;
	}
	
	public void setWalker()
	{
		_isWalker = true;
	}
	
	public override void rechargeShots(bool physical, bool magic, bool fish)
	{
		if (_isFakePlayer && Config.FAKE_PLAYER_USE_SHOTS)
		{
			if (physical)
			{
				Broadcast.toSelfAndKnownPlayersInRadius(this, new MagicSkillUse(this, this, 2154, 1, 0, 0), 600);
				chargeShot(ShotType.SOULSHOTS);
			}
			if (magic)
			{
				Broadcast.toSelfAndKnownPlayersInRadius(this, new MagicSkillUse(this, this, 2061, 1, 0, 0), 600);
				chargeShot(ShotType.SPIRITSHOTS);
			}
		}
		else
		{
			if (physical && (_soulshotamount > 0))
			{
				if (Rnd.get(100) > getTemplate().getSoulShotChance())
				{
					return;
				}
				_soulshotamount--;
				Broadcast.toSelfAndKnownPlayersInRadius(this, new MagicSkillUse(this, this, 2154, 1, 0, 0), 600);
				chargeShot(ShotType.SOULSHOTS);
			}
			if (magic && (_spiritshotamount > 0))
			{
				if (Rnd.get(100) > getTemplate().getSpiritShotChance())
				{
					return;
				}
				_spiritshotamount--;
				Broadcast.toSelfAndKnownPlayersInRadius(this, new MagicSkillUse(this, this, 2061, 1, 0, 0), 600);
				chargeShot(ShotType.SPIRITSHOTS);
			}
		}
	}
	
	/**
	 * Receive the stored int value for this {@link Npc} instance.
	 * @return stored script value
	 */
	public int getScriptValue()
	{
		return _scriptValue;
	}
	
	/**
	 * Sets the script value related with this {@link Npc} instance.
	 * @param value value to store
	 */
	public void setScriptValue(int value)
	{
		_scriptValue = value;
	}
	
	/**
	 * @param value value to store
	 * @return {@code true} if stored script value equals given value, {@code false} otherwise
	 */
	public bool isScriptValue(int value)
	{
		return _scriptValue == value;
	}
	
	/**
	 * @param npc NPC to check
	 * @return {@code true} if both given NPC and this NPC is in the same spawn group, {@code false} otherwise
	 */
	public bool isInMySpawnGroup(Npc npc)
	{
		return getSpawn().getNpcSpawnTemplate().getSpawnTemplate().getName().equals(npc.getSpawn().getNpcSpawnTemplate().getSpawnTemplate().getName());
	}
	
	/**
	 * @return {@code true} if NPC currently located in own spawn point, {@code false} otherwise
	 */
	public bool staysInSpawnLoc()
	{
		return ((_spawn != null) && (_spawn.getX() == getX()) && (_spawn.getY() == getY()));
	}
	
	/**
	 * @return {@code true} if {@link NpcVariables} instance is attached to current player's scripts, {@code false} otherwise.
	 */
	public bool hasVariables()
	{
		return getScript<NpcVariables>() != null;
	}
	
	/**
	 * @return {@link NpcVariables} instance containing parameters regarding NPC.
	 */
	public NpcVariables getVariables()
	{
		NpcVariables vars = getScript<NpcVariables>();
		return vars != null ? vars : addScript(new NpcVariables());
	}
	
	/**
	 * Send an "event" to all NPCs within given radius
	 * @param eventName - name of event
	 * @param radius - radius to send event
	 * @param reference - WorldObject to pass, if needed
	 */
	public void broadcastEvent(String eventName, int radius, WorldObject reference)
	{
		World.getInstance().forEachVisibleObjectInRange<Npc>(this, radius, obj =>
		{
			if (obj.hasListener(EventType.ON_NPC_EVENT_RECEIVED))
			{
				EventDispatcher.getInstance().notifyEventAsync(new OnNpcEventReceived(eventName, this, obj, reference), obj);
			}
		});
	}
	
	/**
	 * Sends an event to a given object.
	 * @param eventName the event name
	 * @param receiver the receiver
	 * @param reference the reference
	 */
	public void sendScriptEvent(String eventName, WorldObject receiver, WorldObject reference)
	{
		if (EventDispatcher.getInstance().hasListener(EventType.ON_NPC_EVENT_RECEIVED, receiver))
		{
			EventDispatcher.getInstance().notifyEventAsync(new OnNpcEventReceived(eventName, this, (Npc) receiver, reference), receiver);
		}
	}
	
	/**
	 * Gets point in range between radiusMin and radiusMax from this NPC
	 * @param radiusMin miminal range from NPC (not closer than)
	 * @param radiusMax maximal range from NPC (not further than)
	 * @return Location in given range from this NPC
	 */
	public Location getPointInRange(int radiusMin, int radiusMax)
	{
		if ((radiusMax == 0) || (radiusMax < radiusMin))
		{
			return new Location(getX(), getY(), getZ());
		}
		
		int radius = Rnd.get(radiusMin, radiusMax);
		double angle = Rnd.nextDouble() * 2 * Math.PI;
		return new Location((int) (getX() + (radius * Math.Cos(angle))), (int) (getY() + (radius * Math.Sin(angle))), getZ());
	}
	
	/**
	 * Drops an item.
	 * @param creature the last attacker or main damage dealer
	 * @param itemId the item ID
	 * @param itemCount the item count
	 * @return the dropped item
	 */
	public Item dropItem(Creature creature, int itemId, long itemCount)
	{
		Item item = null;
		for (int i = 0; i < itemCount; i++)
		{
			if (ItemData.getInstance().getTemplate(itemId) == null)
			{
				LOGGER.Error("Item doesn't exist so cannot be dropped. Item ID: " + itemId + " Quest: " + getName());
				return null;
			}
			
			item = ItemData.getInstance().createItem("Loot", itemId, itemCount, creature, this);
			if (item == null)
			{
				return null;
			}
			
			if (creature != null)
			{
				item.getDropProtection().protect(creature);
			}
			
			// Randomize drop position.
			int newX = (getX() + Rnd.get((RANDOM_ITEM_DROP_LIMIT * 2) + 1)) - RANDOM_ITEM_DROP_LIMIT;
			int newY = (getY() + Rnd.get((RANDOM_ITEM_DROP_LIMIT * 2) + 1)) - RANDOM_ITEM_DROP_LIMIT;
			int newZ = getZ() + 20;
			
			item.dropMe(this, newX, newY, newZ);
			
			// Add drop to auto destroy item task.
			if (!Config.LIST_PROTECTED_ITEMS.Contains(itemId) && (((Config.AUTODESTROY_ITEM_AFTER > 0) && !item.getTemplate().hasExImmediateEffect()) || ((Config.HERB_AUTO_DESTROY_TIME > 0) && item.getTemplate().hasExImmediateEffect())))
			{
				ItemsAutoDestroyTaskManager.getInstance().addItem(item);
			}
			item.setProtected(false);
			
			// If stackable, end loop as entire count is included in 1 instance of item.
			if (item.isStackable() || !Config.MULTIPLE_ITEM_DROP)
			{
				break;
			}
		}
		return item;
	}
	
	/**
	 * Method overload for {@link Attackable#dropItem(Creature, int, long)}
	 * @param creature the last attacker or main damage dealer
	 * @param item the item holder
	 * @return the dropped item
	 */
	public Item dropItem(Creature creature, ItemHolder item)
	{
		return dropItem(creature, item.getId(), item.getCount());
	}
	
	public override String getName()
	{
		return getTemplate().getName();
	}
	
	public override bool isVisibleFor(Player player)
	{
		if (hasListener(EventType.ON_NPC_CAN_BE_SEEN))
		{
			TerminateReturn term = EventDispatcher.getInstance().notifyEvent(new OnNpcCanBeSeen(this, player), this, TerminateReturn.class);
			if (term != null)
			{
				return term.terminate();
			}
		}
		return base.isVisibleFor(player);
	}
	
	/**
	 * Sets if the players can talk with this npc or not
	 * @param value {@code true} if the players can talk, {@code false} otherwise
	 */
	public void setTalkable(bool value)
	{
		_isTalkable = value;
	}
	
	/**
	 * Checks if the players can talk to this npc.
	 * @return {@code true} if the players can talk, {@code false} otherwise.
	 */
	public bool isTalkable()
	{
		return _isTalkable;
	}
	
	/**
	 * Checks if the NPC is a Quest Monster.
	 * @return {@code true} if the NPC is a Quest Monster, {@code false} otherwise.
	 */
	public bool isQuestMonster()
	{
		return _isQuestMonster;
	}
	
	/**
	 * Sets the weapon id with which this npc was killed.
	 * @param weaponId
	 */
	public void setKillingBlowWeapon(int weaponId)
	{
		_killingBlowWeaponId = weaponId;
	}
	
	/**
	 * @return the id of the weapon with which player killed this npc.
	 */
	public int getKillingBlowWeapon()
	{
		return _killingBlowWeaponId;
	}
	
	public override int getMinShopDistance()
	{
		return Config.SHOP_MIN_RANGE_FROM_NPC;
	}
	
	public override bool isFakePlayer()
	{
		return _isFakePlayer;
	}
	
	/**
	 * @return The player's object Id this NPC is cloning.
	 */
	public int getCloneObjId()
	{
		return _cloneObjId;
	}
	
	/**
	 * @param cloneObjId object id of player or 0 to disable it.
	 */
	public void setCloneObjId(int cloneObjId)
	{
		_cloneObjId = cloneObjId;
	}
	
	/**
	 * @return The clan's object Id this NPC is displaying.
	 */
	public override int getClanId()
	{
		return _clanId;
	}
	
	/**
	 * @param clanObjId object id of clan or 0 to disable it.
	 */
	public void setClanId(int clanObjId)
	{
		_clanId = clanObjId;
	}
	
	/**
	 * Broadcasts NpcSay packet to all known players.
	 * @param chatType the chat type
	 * @param text the text
	 */
	public void broadcastSay(ChatType chatType, String text)
	{
		Broadcast.toKnownPlayers(this, new NpcSay(this, chatType, text));
	}
	
	/**
	 * Broadcasts NpcSay packet to all known players with NPC string id.
	 * @param chatType the chat type
	 * @param npcStringId the NPC string id
	 * @param parameters the NPC string id parameters
	 */
	public void broadcastSay(ChatType chatType, NpcStringId npcStringId, params String[] parameters)
	{
		NpcSay npcSay = new NpcSay(this, chatType, npcStringId);
		if (parameters != null)
		{
			foreach (String parameter in parameters)
			{
				if (parameter != null)
				{
					npcSay.addStringParameter(parameter);
				}
			}
		}
		
		switch (chatType)
		{
			case ChatType.NPC_GENERAL:
			{
				Broadcast.toKnownPlayersInRadius(this, npcSay, 1250);
				break;
			}
			default:
			{
				Broadcast.toKnownPlayers(this, npcSay);
				break;
			}
		}
	}
	
	/**
	 * Broadcasts NpcSay packet to all known players with custom string in specific radius.
	 * @param chatType the chat type
	 * @param text the text
	 * @param radius the radius
	 */
	public void broadcastSay(ChatType chatType, String text, int radius)
	{
		Broadcast.toKnownPlayersInRadius(this, new NpcSay(this, chatType, text), radius);
	}
	
	/**
	 * Broadcasts NpcSay packet to all known players with NPC string id in specific radius.
	 * @param chatType the chat type
	 * @param npcStringId the NPC string id
	 * @param radius the radius
	 */
	public void broadcastSay(ChatType chatType, NpcStringId npcStringId, int radius)
	{
		Broadcast.toKnownPlayersInRadius(this, new NpcSay(this, chatType, npcStringId), radius);
	}
	
	/**
	 * @return the parameters of the npc merged with the spawn parameters (if there are any)
	 */
	public StatSet getParameters()
	{
		if (_params != null)
		{
			return _params;
		}
		
		if (_spawn != null) // Minions doesn't have Spawn object bound
		{
			NpcSpawnTemplate npcSpawnTemplate = _spawn.getNpcSpawnTemplate();
			if ((npcSpawnTemplate != null) && (npcSpawnTemplate.getParameters() != null) && !npcSpawnTemplate.getParameters().isEmpty())
			{
				StatSet @params = getTemplate().getParameters();
				if ((@params != null) && !@params.getSet().isEmpty())
				{
					StatSet set = new StatSet();
					set.merge(@params);
					set.merge(npcSpawnTemplate.getParameters());
					_params = set;
					return set;
				}
				_params = npcSpawnTemplate.getParameters();
				return _params;
			}
		}
		_params = getTemplate().getParameters();
		return _params;
	}
	
	public List<Skill> getLongRangeSkills()
	{
		return getTemplate().getAISkills(AISkillScope.LONG_RANGE);
	}
	
	public List<Skill> getShortRangeSkills()
	{
		return getTemplate().getAISkills(AISkillScope.SHORT_RANGE);
	}
	
	/**
	 * Verifies if the NPC can cast a skill given the minimum and maximum skill chances.
	 * @return {@code true} if the NPC has chances of casting a skill
	 */
	public bool hasSkillChance()
	{
		return Rnd.get(100) < Rnd.get(getTemplate().getMinSkillChance(), getTemplate().getMaxSkillChance());
	}
	
	/**
	 * @return the NpcStringId for name
	 */
	public NpcStringId getNameString()
	{
		return _nameString;
	}
	
	/**
	 * @return the NpcStringId for title
	 */
	public NpcStringId getTitleString()
	{
		return _titleString;
	}
	
	public void setNameString(NpcStringId nameString)
	{
		_nameString = nameString;
	}
	
	public void setTitleString(NpcStringId titleString)
	{
		_titleString = titleString;
	}
	
	public void sendChannelingEffect(Creature target, int state)
	{
		broadcastPacket(new ExShowChannelingEffect(this, target, state));
	}
	
	public void setDBStatus(RaidBossStatus status)
	{
		_raidStatus = status;
	}
	
	public RaidBossStatus getDBStatus()
	{
		return _raidStatus;
	}
	
	public void addQuestTimer(QuestTimer questTimer)
	{
		lock (_questTimers)
		{
			_questTimers.add(questTimer);
		}
	}
	
	public void removeQuestTimer(QuestTimer questTimer)
	{
		lock (_questTimers)
		{
			_questTimers.Remove(questTimer);
		}
	}
	
	public void stopQuestTimers()
	{
		lock (_questTimers)
		{
			foreach (QuestTimer timer in _questTimers)
			{
				timer.cancelTask();
			}
			_questTimers.Clear();
		}
	}
	
	public void addTimerHolder(TimerHolder<?> timer)
	{
		lock (_timerHolders)
		{
			_timerHolders.add(timer);
		}
	}
	
	public void removeTimerHolder(TimerHolder<?> timer)
	{
		lock (_timerHolders)
		{
			_timerHolders.remove(timer);
		}
	}
	
	public void stopTimerHolders()
	{
		lock (_timerHolders)
		{
			foreach (TimerHolder<?> timer in _timerHolders)
			{
				timer.cancelTask();
			}
			_timerHolders.clear();
		}
	}
	
	public override String ToString()
	{
		StringBuilder sb = new StringBuilder();
		sb.Append(GetType().Name);
		sb.Append(":");
		sb.Append(getName());
		sb.Append("(");
		sb.Append(getId());
		sb.Append(")[");
		sb.Append(getObjectId());
		sb.Append("]");
		return sb.ToString();
	}
}
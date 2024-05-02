using System.Globalization;
using L2Dn.GameServer.AI;
using L2Dn.GameServer.Data;
using L2Dn.GameServer.Data.Xml;
using L2Dn.GameServer.Db;
using L2Dn.GameServer.Enums;
using L2Dn.GameServer.Geo;
using L2Dn.GameServer.Handlers;
using L2Dn.GameServer.InstanceManagers;
using L2Dn.GameServer.Model;
using L2Dn.GameServer.Model.Actor;
using L2Dn.GameServer.Model.Actor.Instances;
using L2Dn.GameServer.Model.Actor.Templates;
using L2Dn.GameServer.Model.Html;
using L2Dn.GameServer.Network.Enums;
using L2Dn.GameServer.Network.OutgoingPackets;
using L2Dn.GameServer.Utilities;
using L2Dn.Geometry;
using L2Dn.Model.Enums;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace L2Dn.GameServer.Scripts.Handlers.AdminCommandHandlers;

/**
 * This class handles following admin commands: - show_moves - show_teleport - teleport_to_character - move_to - teleport_character
 * @version $Revision: 1.3.2.6.2.4 $ $Date: 2005/04/11 10:06:06 $ con.close() change and small typo fix by Zoey76 24/02/2011
 */
public class AdminTeleport: IAdminCommandHandler
{
	private static readonly Logger LOGGER = LogManager.GetLogger(nameof(AdminTeleport));
	
	private static readonly string[] ADMIN_COMMANDS =
	{
		"admin_show_moves",
		"admin_show_moves_other",
		"admin_show_teleport",
		"admin_teleport_to_character",
		"admin_teleportto",
		"admin_teleport",
		"admin_move_to",
		"admin_teleport_character",
		"admin_recall",
		"admin_walk",
		"teleportto",
		"recall",
		"admin_recall_npc",
		"admin_gonorth",
		"admin_gosouth",
		"admin_goeast",
		"admin_gowest",
		"admin_goup",
		"admin_godown",
		"admin_tele",
		"admin_teleto",
		"admin_instant_move",
		"admin_sendhome"
	};
	
	public bool useAdminCommand(String command, Player activeChar)
	{
		if (command.equals("admin_instant_move"))
		{
			BuilderUtil.sendSysMessage(activeChar, "Instant move ready. Click where you want to go.");
			activeChar.setTeleMode(AdminTeleportType.DEMONIC);
		}
		else if (command.equals("admin_teleto sayune"))
		{
			BuilderUtil.sendSysMessage(activeChar, "Sayune move ready. Click where you want to go.");
			activeChar.setTeleMode(AdminTeleportType.SAYUNE);
		}
		else if (command.equals("admin_teleto charge"))
		{
			BuilderUtil.sendSysMessage(activeChar, "Charge move ready. Click where you want to go.");
			activeChar.setTeleMode(AdminTeleportType.CHARGE);
		}
		else if (command.equals("admin_teleto end"))
		{
			activeChar.setTeleMode(AdminTeleportType.NORMAL);
		}
		else if (command.equals("admin_show_moves"))
		{
			AdminHtml.showAdminHtml(activeChar, "teleports.htm");
		}
		else if (command.equals("admin_show_moves_other"))
		{
			AdminHtml.showAdminHtml(activeChar, "tele/other.html");
		}
		else if (command.equals("admin_show_teleport"))
		{
			showTeleportCharWindow(activeChar);
		}
		else if (command.equals("admin_recall_npc"))
		{
			recallNPC(activeChar);
		}
		else if (command.equals("admin_teleport_to_character"))
		{
			teleportToCharacter(activeChar, activeChar.getTarget());
		}
		else if (command.startsWith("admin_walk"))
		{
			try
			{
				String val = command.Substring(11);
				StringTokenizer st = new StringTokenizer(val);
				int x = int.Parse(st.nextToken());
				int y = int.Parse(st.nextToken());
				int z = int.Parse(st.nextToken());
				activeChar.getAI().setIntention(CtrlIntention.AI_INTENTION_MOVE_TO, new Location3D(x, y, z));
			}
			catch (Exception e)
			{
			}
		}
		else if (command.startsWith("admin_move_to"))
		{
			try
			{
				String val = command.Substring(14);
				teleportTo(activeChar, val);
			}
			catch (IndexOutOfRangeException e)
			{
				// Case of empty or missing coordinates
				AdminHtml.showAdminHtml(activeChar, "teleports.htm");
			}
			catch (FormatException nfe)
			{
				BuilderUtil.sendSysMessage(activeChar, "Usage: //move_to <x> <y> <z>");
				AdminHtml.showAdminHtml(activeChar, "teleports.htm");
			}
		}
		else if (command.startsWith("admin_teleport_character"))
		{
			try
			{
				String val = command.Substring(25);
				teleportCharacter(activeChar, val);
			}
			catch (IndexOutOfRangeException e)
			{
				// Case of empty coordinates
				BuilderUtil.sendSysMessage(activeChar, "Wrong or no Coordinates given.");
				showTeleportCharWindow(activeChar); // back to character teleport
			}
		}
		else if (command.startsWith("admin_teleportto "))
		{
			try
			{
				String targetName = command.Substring(17);
				Player player = World.getInstance().getPlayer(targetName);
				teleportToCharacter(activeChar, player);
			}
			catch (IndexOutOfRangeException e)
			{
			}
		}
		else if (command.startsWith("admin_teleport"))
		{
			try
			{
				StringTokenizer st = new StringTokenizer(command, " ");
				st.nextToken();
				int x = (int) float.Parse(st.nextToken(), CultureInfo.InvariantCulture);
				int y = (int) float.Parse(st.nextToken(), CultureInfo.InvariantCulture);
				int z = st.hasMoreTokens() ? ((int) float.Parse(st.nextToken(), CultureInfo.InvariantCulture)) : GeoEngine.getInstance().getHeight(x, y, 10000);
				activeChar.teleToLocation(x, y, z);
			}
			catch (Exception e)
			{
				BuilderUtil.sendSysMessage(activeChar, "Wrong coordinates!");
			}
		}
		else if (command.startsWith("admin_recall "))
		{
			try
			{
				String[] param = command.Split(" ");
				if (param.Length != 2)
				{
					BuilderUtil.sendSysMessage(activeChar, "Usage: //recall <playername>");
					return false;
				}
				String targetName = param[1];
				Player player = World.getInstance().getPlayer(targetName);
				if (player != null)
				{
					teleportCharacter(player, activeChar.getLocation(), activeChar);
				}
				else
				{
					changeCharacterPosition(activeChar, targetName);
				}
			}
			catch (IndexOutOfRangeException e)
			{
			}
		}
		else if (command.equals("admin_tele"))
		{
			showTeleportWindow(activeChar);
		}
		else if (command.startsWith("admin_go"))
		{
			int intVal = 150;
			int x = activeChar.getX();
			int y = activeChar.getY();
			int z = activeChar.getZ();
			try
			{
				String val = command.Substring(8);
				StringTokenizer st = new StringTokenizer(val);
				String dir = st.nextToken();
				if (st.hasMoreTokens())
				{
					intVal = int.Parse(st.nextToken());
				}
				if (dir.equals("east"))
				{
					x += intVal;
				}
				else if (dir.equals("west"))
				{
					x -= intVal;
				}
				else if (dir.equals("north"))
				{
					y -= intVal;
				}
				else if (dir.equals("south"))
				{
					y += intVal;
				}
				else if (dir.equals("up"))
				{
					z += intVal;
				}
				else if (dir.equals("down"))
				{
					z -= intVal;
				}
				activeChar.teleToLocation(new Location(x, y, z));
				showTeleportWindow(activeChar);
			}
			catch (Exception e)
			{
				BuilderUtil.sendSysMessage(activeChar, "Usage: //go<north|south|east|west|up|down> [offset] (default 150)");
			}
		}
		else if (command.startsWith("admin_sendhome"))
		{
			StringTokenizer st = new StringTokenizer(command, " ");
			st.nextToken(); // Skip command.
			if (st.countTokens() > 1)
			{
				BuilderUtil.sendSysMessage(activeChar, "Usage: //sendhome <playername>");
			}
			else if (st.countTokens() == 1)
			{
				String name = st.nextToken();
				Player player = World.getInstance().getPlayer(name);
				if (player == null)
				{
					activeChar.sendPacket(SystemMessageId.THAT_PLAYER_IS_NOT_ONLINE);
					return false;
				}
				teleportHome(player);
			}
			else
			{
				WorldObject target = activeChar.getTarget();
				if ((target != null) && target.isPlayer())
				{
					teleportHome(target.getActingPlayer());
				}
				else
				{
					activeChar.sendPacket(SystemMessageId.INVALID_TARGET);
				}
			}
		}
		return true;
	}
	
	public String[] getAdminCommandList()
	{
		return ADMIN_COMMANDS;
	}
	
	/**
	 * This method sends a player to it's home town.
	 * @param player the player to teleport.
	 */
	private void teleportHome(Player player)
	{
		String regionName;
		switch (player.getRace())
		{
			case Race.ELF:
			{
				regionName = "elf_town";
				break;
			}
			case Race.DARK_ELF:
			{
				regionName = "darkelf_town";
				break;
			}
			case Race.ORC:
			{
				regionName = "orc_town";
				break;
			}
			case Race.DWARF:
			{
				regionName = "dwarf_town";
				break;
			}
			case Race.KAMAEL:
			{
				regionName = "kamael_town";
				break;
			}
			case Race.HUMAN:
			default:
			{
				regionName = "talking_island_town";
				break;
			}
		}
		
		player.teleToLocation(MapRegionManager.getInstance().getMapRegionByName(regionName).getSpawnLoc(), true, null);
	}
	
	private void teleportTo(Player activeChar, String coords)
	{
		try
		{
			StringTokenizer st = new StringTokenizer(coords);
			int x = int.Parse(st.nextToken());
			int y = int.Parse(st.nextToken());
			int z = int.Parse(st.nextToken());
			activeChar.getAI().setIntention(CtrlIntention.AI_INTENTION_IDLE);
			activeChar.teleToLocation(x, y, z, null);
			BuilderUtil.sendSysMessage(activeChar, "You have been teleported to " + coords);
		}
		catch (Exception nsee)
		{
			BuilderUtil.sendSysMessage(activeChar, "Wrong or no Coordinates given.");
		}
	}
	
	private void showTeleportWindow(Player activeChar)
	{
		AdminHtml.showAdminHtml(activeChar, "move.htm");
	}
	
	private void showTeleportCharWindow(Player activeChar)
	{
		WorldObject target = activeChar.getTarget();
		Player player = null;
		if ((target != null) && target.isPlayer())
		{
			player = (Player) target;
		}
		else
		{
			activeChar.sendPacket(SystemMessageId.INVALID_TARGET);
			return;
		}

		string replyMSG =
			"<html><title>Teleport Character</title><body>The character you will teleport is " + player.getName() +
			".<br>Co-ordinate x<edit var=\"char_cord_x\" width=110>Co-ordinate y<edit var=\"char_cord_y\" width=110>Co-ordinate z<edit var=\"char_cord_z\" width=110><button value=\"Teleport\" action=\"bypass -h admin_teleport_character $char_cord_x $char_cord_y $char_cord_z\" width=60 height=15 back=\"L2UI_ct1.button_df\" fore=\"L2UI_ct1.button_df\"><button value=\"Teleport near you\" action=\"bypass -h admin_teleport_character " +
			activeChar.getX() + " " + activeChar.getY() + " " + activeChar.getZ() +
			"\" width=115 height=15 back=\"L2UI_ct1.button_df\" fore=\"L2UI_ct1.button_df\"><center><button value=\"Back\" action=\"bypass -h admin_current_player\" width=40 height=15 back=\"L2UI_ct1.button_df\" fore=\"L2UI_ct1.button_df\"></center></body></html>";
		
		HtmlContent htmlContent = HtmlContent.LoadFromText(replyMSG, activeChar);
		NpcHtmlMessagePacket adminReply = new NpcHtmlMessagePacket(null, 1, htmlContent);
		activeChar.sendPacket(adminReply);
	}
	
	private void teleportCharacter(Player activeChar, String coords)
	{
		WorldObject target = activeChar.getTarget();
		Player player = null;
		if ((target != null) && target.isPlayer())
		{
			player = (Player) target;
		}
		else
		{
			activeChar.sendPacket(SystemMessageId.INVALID_TARGET);
			return;
		}
		
		if (player.getObjectId() == activeChar.getObjectId())
		{
			player.sendPacket(SystemMessageId.YOU_CANNOT_USE_THIS_ON_YOURSELF);
		}
		else
		{
			try
			{
				StringTokenizer st = new StringTokenizer(coords);
				String x1 = st.nextToken();
				int x = int.Parse(x1);
				String y1 = st.nextToken();
				int y = int.Parse(y1);
				String z1 = st.nextToken();
				int z = int.Parse(z1);
				teleportCharacter(player, new Location(x, y, z), null);
			}
			catch (Exception nsee)
			{
			}
		}
	}
	
	/**
	 * @param player
	 * @param loc
	 * @param activeChar
	 */
	private void teleportCharacter(Player player, Location loc, Player activeChar)
	{
		if (player != null)
		{
			// Check for jail
			if (player.isJailed())
			{
				BuilderUtil.sendSysMessage(activeChar, "Sorry, player " + player.getName() + " is in Jail.");
			}
			else
			{
				BuilderUtil.sendSysMessage(activeChar, "You have recalled " + player.getName());
				player.sendMessage("Admin is teleporting you.");
				player.getAI().setIntention(CtrlIntention.AI_INTENTION_IDLE);
				player.teleToLocation(loc, true, activeChar.getInstanceWorld());
			}
		}
	}
	
	private void teleportToCharacter(Player activeChar, WorldObject target)
	{
		if ((target == null) || !target.isPlayer())
		{
			activeChar.sendPacket(SystemMessageId.INVALID_TARGET);
			return;
		}
		
		Player player = target.getActingPlayer();
		if (player.getObjectId() == activeChar.getObjectId())
		{
			player.sendPacket(SystemMessageId.YOU_CANNOT_USE_THIS_ON_YOURSELF);
		}
		else
		{
			activeChar.getAI().setIntention(CtrlIntention.AI_INTENTION_IDLE);
			activeChar.teleToLocation(player, true, player.getInstanceWorld());
			BuilderUtil.sendSysMessage(activeChar, "You have teleported to character " + player.getName() + ".");
		}
	}
	
	private void changeCharacterPosition(Player activeChar, String name)
	{
		int x = activeChar.getX();
		int y = activeChar.getY();
		int z = activeChar.getZ();
		try
		{
			using GameServerDbContext ctx = DbFactory.Instance.CreateDbContext();
			int count = ctx.Characters.Where(c => c.Name == name).ExecuteUpdate(s =>
				s.SetProperty(r => r.X, x).SetProperty(r => r.Y, y).SetProperty(r => r.Z, z));

			if (count == 0)
			{
				BuilderUtil.sendSysMessage(activeChar, "Character not found or position unaltered.");
			}
			else
			{
				BuilderUtil.sendSysMessage(activeChar, "Player's [" + name + "] position is now set to (" + x + "," + y + "," + z + ").");
			}
		}
		catch (Exception se)
		{
			BuilderUtil.sendSysMessage(activeChar, "SQLException while changing offline character's position");
		}
	}
	
	private void recallNPC(Player activeChar)
	{
		WorldObject obj = activeChar.getTarget();
		if ((obj is Npc) && !((Npc) obj).isMinion() && !(obj is RaidBoss) && !(obj is GrandBoss))
		{
			Npc target = (Npc) obj;
			int monsterTemplate = target.getTemplate().getId();
			NpcTemplate template1 = NpcData.getInstance().getTemplate(monsterTemplate);
			if (template1 == null)
			{
				BuilderUtil.sendSysMessage(activeChar, "Incorrect monster template.");
				LOGGER.Warn("ERROR: NPC " + target.getObjectId() + " has a 'null' template.");
				return;
			}
			
			Spawn spawn = target.getSpawn();
			if (spawn == null)
			{
				BuilderUtil.sendSysMessage(activeChar, "Incorrect monster spawn.");
				LOGGER.Warn("ERROR: NPC " + target.getObjectId() + " has a 'null' spawn.");
				return;
			}
			
			TimeSpan respawnTime = spawn.getRespawnDelay();
			target.deleteMe();
			spawn.stopRespawn();
			SpawnTable.getInstance().deleteSpawn(spawn, true);
			
			try
			{
				spawn = new Spawn(template1);
				spawn.Location.setXYZ(activeChar.getLocation().ToLocation3D());
				spawn.setAmount(1);
				spawn.Location.setHeading(activeChar.getHeading());
				spawn.setRespawnDelay(respawnTime);
				if (activeChar.isInInstance())
				{
					spawn.setInstanceId(activeChar.getInstanceId());
				}
				SpawnTable.getInstance().addNewSpawn(spawn, true);
				spawn.init();
				if (respawnTime <= TimeSpan.Zero)
				{
					spawn.stopRespawn();
				}
				
				BuilderUtil.sendSysMessage(activeChar, "Created " + template1.getName() + " on " + target.getObjectId() + ".");
			}
			catch (Exception e)
			{
				BuilderUtil.sendSysMessage(activeChar, "Target is not in game.");
			}
		}
		else if (obj is RaidBoss)
		{
			RaidBoss target = (RaidBoss) obj;
			Spawn spawn = target.getSpawn();
			double curHP = target.getCurrentHp();
			double curMP = target.getCurrentMp();
			if (spawn == null)
			{
				BuilderUtil.sendSysMessage(activeChar, "Incorrect raid spawn.");
				LOGGER.Warn("ERROR: NPC Id" + target.getId() + " has a 'null' spawn.");
				return;
			}
			DBSpawnManager.getInstance().deleteSpawn(spawn, true);
			try
			{
				Spawn spawnDat = new Spawn(target.getId());
				spawnDat.Location.setXYZ(activeChar.getLocation().ToLocation3D());
				spawnDat.setAmount(1);
				spawnDat.Location.setHeading(activeChar.getHeading());
				spawnDat.setRespawnMinDelay(TimeSpan.FromSeconds(43200));
				spawnDat.setRespawnMaxDelay(TimeSpan.FromSeconds(129600));
				
				DBSpawnManager.getInstance().addNewSpawn(spawnDat, null, curHP, curMP, true);
			}
			catch (Exception e)
			{
				activeChar.sendPacket(SystemMessageId.YOUR_TARGET_CANNOT_BE_FOUND);
			}
		}
		else
		{
			activeChar.sendPacket(SystemMessageId.INVALID_TARGET);
		}
	}
}

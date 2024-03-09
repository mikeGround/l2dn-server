using System.Text;
using L2Dn.GameServer.Data;
using L2Dn.GameServer.Data.Xml;
using L2Dn.GameServer.Handlers;
using L2Dn.GameServer.InstanceManagers;
using L2Dn.GameServer.Model.Actor;
using L2Dn.GameServer.Model.Events;
using L2Dn.GameServer.Model.Events.Listeners;
using L2Dn.GameServer.Model.Quests;
using L2Dn.GameServer.Network.Enums;
using L2Dn.GameServer.Network.OutgoingPackets;
using L2Dn.GameServer.Utilities;

namespace L2Dn.GameServer.Scripts.Handlers.BypassHandlers;

public class QuestLink: IBypassHandler
{
	private static readonly string[] COMMANDS =
	{
		"Quest"
	};
	
	public bool useBypass(String command, Player player, Creature target)
	{
		String quest = "";
		try
		{
			quest = command.Substring(5).Trim();
		}
		catch (IndexOutOfRangeException ioobe)
		{
		}
		
		if (quest.isEmpty())
		{
			showQuestWindow(player, (Npc) target);
		}
		else
		{
			int questNameEnd = quest.IndexOf(' ');
			if (questNameEnd == -1)
			{
				showQuestWindow(player, (Npc) target, quest);
			}
			else
			{
				player.processQuestEvent(quest.Substring(0, questNameEnd), quest.Substring(questNameEnd).Trim());
			}
		}
		return true;
	}
	
	/**
	 * Open a choose quest window on client with all quests available of the Npc.<br>
	 * <br>
	 * <b><u>Actions</u>:</b><br>
	 * <li>Send a Server=>Client NpcHtmlMessage containing the text of the Npc to the Player</li><br>
	 * @param player The Player that talk with the Npc
	 * @param npc The table containing quests of the Npc
	 * @param quests
	 */
	private void showQuestChooseWindow(Player player, Npc npc, ICollection<Quest> quests)
	{
		StringBuilder sbStarted = new StringBuilder(128);
		StringBuilder sbCanStart = new StringBuilder(128);
		StringBuilder sbCantStart = new StringBuilder(128);
		StringBuilder sbCompleted = new StringBuilder(128);
		
		Set<Quest> startingQuests = new();
		foreach (AbstractEventListener listener in npc.getListeners(EventType.ON_NPC_QUEST_START))
		{
			Object owner = listener.getOwner();
			if ((owner is Quest) && (NewQuestData.getInstance().getQuestById(((Quest) owner).getId()) == null))
			{
				startingQuests.add((Quest) owner);
			}
		}
		
		List<Quest> questList = quests.ToList();
		if (Config.ORDER_QUEST_LIST_BY_QUESTID)
		{
			Map<int, Quest> orderedQuests = new(); // Use TreeMap to order quests
			foreach (Quest q in questList)
			{
				if (NewQuestData.getInstance().getQuestById(q.getId()) == null)
				{
					orderedQuests.put(q.getId(), q);
				}
			}
			
			questList = orderedQuests.values().ToList();
		}
		
		int startCount = 0;
		String startQuest = null;
		foreach (Quest quest in questList)
		{
			QuestState qs = player.getQuestState(quest.getScriptName());
			if ((qs == null) || qs.isCreated() || (qs.isCompleted() && qs.isNowAvailable()))
			{
				String startConditionHtml = quest.getStartConditionHtml(player, npc);
				if (((startConditionHtml != null) && startConditionHtml.isEmpty()) || !startingQuests.Contains(quest))
				{
					continue;
				}
				else if (startingQuests.Contains(quest) && quest.canStartQuest(player))
				{
					startCount++;
					startQuest = quest.getName();
					
					sbCanStart.Append("<font color=\"bbaa88\">");
					sbCanStart.Append("<button icon=\"quest\" align=\"left\" action=\"bypass -h npc_" + npc.getObjectId() + "_Quest " + quest.getName() + "\">");

					String localisation = quest.isCustomQuest() ? quest.getPath() : "<fstring>" + quest.getNpcStringId() + "01</fstring>";
					// if (Config.MULTILANG_ENABLE)
					// {
					// 	NpcStringId ns = (NpcStringId)(int.Parse(quest.getNpcStringId() + "01"));
					// 	if (ns != null)
					// 	{
					// 		NSLocalisation nsl = ns.getLocalisation(player.getLang());
					// 		if (nsl != null)
					// 		{
					// 			localisation = nsl.getLocalisation(Collections.emptyList());
					// 		}
					// 	}
					// }
					
					sbCanStart.Append(localisation);
					sbCanStart.Append("</button></font>");
				}
				else
				{
					sbCantStart.Append("<font color=\"a62f31\">");
					sbCantStart.Append("<button icon=\"quest\" align=\"left\" action=\"bypass -h npc_" + npc.getObjectId() + "_Quest " + quest.getName() + "\">");
					String localisation = quest.isCustomQuest() ? quest.getPath() : "<fstring>" + quest.getNpcStringId() + "01</fstring>";
					// if (Config.MULTILANG_ENABLE)
					// {
					// 	NpcStringId ns = NpcStringId.getNpcStringId(int.Parse(quest.getNpcStringId() + "01"));
					// 	if (ns != null)
					// 	{
					// 		NSLocalisation nsl = ns.getLocalisation(player.getLang());
					// 		if (nsl != null)
					// 		{
					// 			localisation = nsl.getLocalisation(Collections.emptyList());
					// 		}
					// 	}
					// }
					sbCantStart.Append(localisation);
					sbCantStart.Append("</button></font>");
				}
			}
			else if (Quest.getNoQuestMsg(player).equals(quest.onTalk(npc, player, true)))
			{
				continue;
			}
			else if (qs.isStarted())
			{
				startCount++;
				startQuest = quest.getName();
				
				sbStarted.Append("<font color=\"ffdd66\">");
				sbStarted.Append("<button icon=\"quest\" align=\"left\" action=\"bypass -h npc_" + npc.getObjectId() + "_Quest " + quest.getName() + "\">");
				String localisation = quest.isCustomQuest() ? quest.getPath() + " (In Progress)" : "<fstring>" + quest.getNpcStringId() + "02</fstring>";
				// if (Config.MULTILANG_ENABLE)
				// {
				// 	NpcStringId ns = NpcStringId.getNpcStringId(int.Parse(quest.getNpcStringId() + "02"));
				// 	if (ns != null)
				// 	{
				// 		NSLocalisation nsl = ns.getLocalisation(player.getLang());
				// 		if (nsl != null)
				// 		{
				// 			localisation = nsl.getLocalisation(Collections.emptyList());
				// 		}
				// 	}
				// }
				sbStarted.Append(localisation);
				sbStarted.Append("</button></font>");
			}
			else if (qs.isCompleted())
			{
				sbCompleted.Append("<font color=\"787878\">");
				sbCompleted.Append("<button icon=\"quest\" align=\"left\" action=\"bypass -h npc_" + npc.getObjectId() + "_Quest " + quest.getName() + "\">");
				String localisation = quest.isCustomQuest() ? quest.getPath() + " (Done) " : "<fstring>" + quest.getNpcStringId() + "03</fstring>";
				// if (Config.MULTILANG_ENABLE)
				// {
				// 	NpcStringId ns = NpcStringId.getNpcStringId(int.Parse(quest.getNpcStringId() + "03"));
				// 	if (ns != null)
				// 	{
				// 		NSLocalisation nsl = ns.getLocalisation(player.getLang());
				// 		if (nsl != null)
				// 		{
				// 			localisation = nsl.getLocalisation(Collections.emptyList());
				// 		}
				// 	}
				// }
				sbCompleted.Append(localisation);
				sbCompleted.Append("</button></font>");
			}
		}
		
		if (startCount == 1)
		{
			showQuestWindow(player, npc, startQuest);
			return;
		}
		
		String content;
		if ((sbStarted.Length > 0) || (sbCanStart.Length > 0) || (sbCantStart.Length > 0) || (sbCompleted.Length > 0))
		{
			StringBuilder sb = new StringBuilder(128);
			sb.Append("<html><body>");
			sb.Append(sbStarted.ToString());
			sb.Append(sbCanStart.ToString());
			sb.Append(sbCantStart.ToString());
			sb.Append(sbCompleted.ToString());
			sb.Append("</body></html>");
			content = sb.ToString();
		}
		else
		{
			content = Quest.getNoQuestMsg(player);
		}
		
		// Send a Server=>Client packet NpcHtmlMessage to the Player in order to display the message of the Npc
		content = content.Replace("%objectId%", npc.getObjectId().ToString());
		player.sendPacket(new NpcHtmlMessagePacket(npc.getObjectId(), content));
	}
	
	/**
	 * Open a quest window on client with the text of the Npc.<br>
	 * <br>
	 * <b><u>Actions</u>:</b><br>
	 * <ul>
	 * <li>Get the text of the quest state in the folder data/scripts/quests/questId/stateId.htm</li>
	 * <li>Send a Server=>Client NpcHtmlMessage containing the text of the Npc to the Player</li>
	 * <li>Send a Server=>Client ActionFailedPacket to the Player in order to avoid that the client wait another packet</li>
	 * </ul>
	 * @param player the Player that talk with the {@code npc}
	 * @param npc the Npc that chats with the {@code player}
	 * @param questId the Id of the quest to display the message
	 */
	private void showQuestWindow(Player player, Npc npc, String questId)
	{
		String content = null;
		
		Quest q = QuestManager.getInstance().getQuest(questId);
		
		// Get the state of the selected quest
		QuestState qs = player.getQuestState(questId);
		if (q != null)
		{
			if (((q.getId() >= 1) && (q.getId() < 20000)) && ((player.getWeightPenalty() >= 3) || !player.isInventoryUnder90(true)))
			{
				player.sendPacket(SystemMessageId.UNABLE_TO_PROCESS_THIS_REQUEST_UNTIL_YOUR_INVENTORY_S_WEIGHT_AND_SLOT_COUNT_ARE_LESS_THAN_80_PERCENT_OF_CAPACITY);
				return;
			}
			
			if ((qs == null) && (q.getId() >= 1) && (q.getId() < 20000) && (player.getAllActiveQuests().Count > 40))
			{
				HtmlPacketHelper helper = new HtmlPacketHelper(DataFileLocation.Data, "html/fullquest.html");
				NpcHtmlMessagePacket html = new NpcHtmlMessagePacket(npc.getObjectId(), helper);
				player.sendPacket(html);
				return;
			}
			
			q.notifyTalk(npc, player);
		}
		else
		{
			content = Quest.getNoQuestMsg(player); // no quests found
		}
		
		// Send a Server=>Client packet NpcHtmlMessage to the Player in order to display the message of the Npc
		if (content != null)
		{
			content = content.Replace("%objectId%", npc.getObjectId().ToString());
			player.sendPacket(new NpcHtmlMessagePacket(npc.getObjectId(), content));
		}
		
		// Send a Server=>Client ActionFailedPacket to the Player in order to avoid that the client wait another packet
		player.sendPacket(ActionFailedPacket.STATIC_PACKET);
	}
	
	/**
	 * Collect awaiting quests/start points and display a QuestChooseWindow (if several available) or QuestWindow.
	 * @param player the Player that talk with the {@code npc}.
	 * @param npc the Npc that chats with the {@code player}.
	 */
	private void showQuestWindow(Player player, Npc npc)
	{
		Set<Quest> quests = new();
		foreach (AbstractEventListener listener in npc.getListeners(EventType.ON_NPC_TALK))
		{
			Object owner = listener.getOwner();
			if (owner is Quest)
			{
				Quest quest = (Quest) owner;
				if ((quest.getId() > 0) && (quest.getId() < 20000) && (quest.getId() != 255) && !Quest.getNoQuestMsg(player).equals(quest.onTalk(npc, player, true)))
				{
					quests.add(quest);
				}
			}
		}
		
		if (quests.size() > 1)
		{
			showQuestChooseWindow(player, npc, quests);
		}
		else if (quests.size() == 1)
		{
			showQuestWindow(player, npc, quests.FirstOrDefault()?.getName() ?? string.Empty);
		}
		else
		{
			showQuestWindow(player, npc, "");
		}
	}
	
	public String[] getBypassList()
	{
		return COMMANDS;
	}
}
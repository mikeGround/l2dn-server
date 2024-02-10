using L2Dn.GameServer.Model.Actor;

namespace L2Dn.GameServer.Model.InstanceZones.Conditions;

/**
 * Instance quest condition
 * @author malyelfik
 */
public class ConditionQuest: Condition
{
	public ConditionQuest(InstanceTemplate template, StatSet parameters, bool onlyLeader, bool showMessageAndHtml):
		base(template, parameters, onlyLeader, showMessageAndHtml)
	{
		// Set message
		setSystemMessage(SystemMessageId.C1_DOES_NOT_MEET_QUEST_REQUIREMENTS_AND_CANNOT_ENTER,
			(message, player) => message.addString(player.getName()));
	}

	protected override bool test(Player player, Npc npc)
	{
		int id = getParameters().getInt("id");
		Quest q = QuestManager.getInstance().getQuest(id);
		if (q == null)
		{
			return false;
		}

		QuestState qs = player.getQuestState(q.getName());
		if (qs == null)
		{
			return false;
		}

		int cond = getParameters().getInt("cond", -1);
		return (cond == -1) || qs.isCond(cond);
	}
}
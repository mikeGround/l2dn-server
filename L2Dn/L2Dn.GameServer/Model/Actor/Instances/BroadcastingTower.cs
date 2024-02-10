﻿using L2Dn.GameServer.Enums;
using L2Dn.GameServer.Model.Actor.Templates;

namespace L2Dn.GameServer.Model.Actor.Instances;

public class BroadcastingTower: Npc
{
    public BroadcastingTower(NpcTemplate template): base(template)
    {
        setInstanceType(InstanceType.BroadcastingTower);
    }

    public override void showChatWindow(Player player, int value)
    {
        String filename = null;
        if (isInsideRadius2D(-79884, 86529, 0, 50) || isInsideRadius2D(-78858, 111358, 0, 50) ||
            isInsideRadius2D(-76973, 87136, 0, 50) || isInsideRadius2D(-75850, 111968, 0, 50))
        {
            if (value == 0)
            {
                filename = "data/html/observation/" + getId() + "-Oracle.htm";
            }
            else
            {
                filename = "data/html/observation/" + getId() + "-Oracle-" + value + ".htm";
            }
        }
        else if (value == 0)
        {
            filename = "data/html/observation/" + getId() + ".htm";
        }
        else
        {
            filename = "data/html/observation/" + getId() + "-" + value + ".htm";
        }

        NpcHtmlMessage html = new NpcHtmlMessage(getObjectId());
        html.setFile(player, filename);
        html.replace("%objectId%", String.valueOf(getObjectId()));
        player.sendPacket(html);
    }
}
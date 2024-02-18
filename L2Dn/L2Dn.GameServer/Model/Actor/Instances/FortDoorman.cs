﻿using L2Dn.GameServer.Data;
using L2Dn.GameServer.Enums;
using L2Dn.GameServer.Model.Actor.Templates;
using L2Dn.GameServer.Network.Enums;
using L2Dn.GameServer.Network.OutgoingPackets;
using L2Dn.GameServer.Utilities;

namespace L2Dn.GameServer.Model.Actor.Instances;

public class FortDoorman: Doorman
{
    public FortDoorman(NpcTemplate template): base(template)
    {
        setInstanceType(InstanceType.FortDoorman);
    }

    public override void showChatWindow(Player player)
    {
        player.sendPacket(ActionFailedPacket.STATIC_PACKET);

        HtmlPacketHelper helper;
        if (!isOwnerClan(player))
        {
            helper = new HtmlPacketHelper(DataFileLocation.Data, "html/doorman/" + getTemplate().getId() + "-no.htm");
        }
        else if (isUnderSiege())
        {
            helper = new HtmlPacketHelper(DataFileLocation.Data, "html/doorman/" + getTemplate().getId() + "-busy.htm");
        }
        else
        {
            helper = new HtmlPacketHelper(DataFileLocation.Data, "html/doorman/" + getTemplate().getId() + ".htm");
        }

        helper.Replace("%objectId%", getObjectId().ToString());

        NpcHtmlMessagePacket html = new NpcHtmlMessagePacket(getObjectId(), helper);
        player.sendPacket(html);
    }

    protected override void openDoors(Player player, String command)
    {
        StringTokenizer st = new StringTokenizer(command.Substring(10), ", ");
        st.nextToken();

        while (st.hasMoreTokens())
        {
            getFort().openDoor(player, int.Parse(st.nextToken()));
        }
    }

    protected override void closeDoors(Player player, String command)
    {
        StringTokenizer st = new StringTokenizer(command.Substring(11), ", ");
        st.nextToken();

        while (st.hasMoreTokens())
        {
            getFort().closeDoor(player, int.Parse(st.nextToken()));
        }
    }

    protected sealed override bool isOwnerClan(Player player)
    {
        return (player.getClan() != null) && (getFort() != null) && (getFort().getOwnerClan() != null) &&
               (player.getClanId() == getFort().getOwnerClan().getId());
    }

    protected sealed override bool isUnderSiege()
    {
        return getFort().getZone().isActive();
    }
}
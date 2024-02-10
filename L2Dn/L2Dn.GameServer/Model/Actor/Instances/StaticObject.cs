﻿using L2Dn.GameServer.AI;
using L2Dn.GameServer.Enums;
using L2Dn.GameServer.Model.Actor.Stats;
using L2Dn.GameServer.Model.Actor.Status;
using L2Dn.GameServer.Model.Actor.Templates;
using L2Dn.GameServer.Model.Items;
using L2Dn.GameServer.Model.Items.Instances;
using L2Dn.GameServer.Model.Skills;

namespace L2Dn.GameServer.Model.Actor.Instances;

/**
 * Static Object instance.
 * @author godson
 */
public class StaticObject: Creature
{
	/** The interaction distance of the StaticObject */
	public const int INTERACTION_DISTANCE = 150;
	
	private readonly int _staticObjectId;
	private int _meshIndex = 0; // 0 - static objects, alternate static objects
	private int _type = -1; // 0 - map signs, 1 - throne , 2 - arena signs
	private ShowTownMap _map;
	
	protected override CreatureAI initAI()
	{
		return null;
	}
	
	/**
	 * Gets the static object ID.
	 * @return the static object ID
	 */
	public override int getId()
	{
		return _staticObjectId;
	}
	
	/**
	 * @param template
	 * @param staticId
	 */
	public StaticObject(CreatureTemplate template, int staticId): base(template)
	{
		setInstanceType(InstanceType.StaticObject);
		_staticObjectId = staticId;
	}
	
	public override StaticObjectStat getStat()
	{
		return (StaticObjectStat)base.getStat();
	}
	
	public override void initCharStat()
	{
		setStat(new StaticObjectStat(this));
	}
	
	public override StaticObjectStatus getStatus()
	{
		return (StaticObjectStatus)base.getStatus();
	}
	
	public override void initCharStatus()
	{
		setStatus(new StaticObjectStatus(this));
	}
	
	public int getType()
	{
		return _type;
	}
	
	public void setType(int type)
	{
		_type = type;
	}
	
	public void setMap(String texture, int x, int y)
	{
		_map = new ShowTownMap("town_map." + texture, x, y);
	}
	
	public ShowTownMap getMap()
	{
		return _map;
	}
	
	public override int getLevel()
	{
		return 1;
	}
	
	public override Item getActiveWeaponInstance()
	{
		return null;
	}
	
	public override Weapon getActiveWeaponItem()
	{
		return null;
	}
	
	public override Item getSecondaryWeaponInstance()
	{
		return null;
	}
	
	public override Weapon getSecondaryWeaponItem()
	{
		return null;
	}
	
	public override bool isAutoAttackable(Creature attacker)
	{
		return false;
	}
	
	/**
	 * Set the meshIndex of the object.<br>
	 * <br>
	 * <b><u>Values</u>:</b>
	 * <ul>
	 * <li>default textures : 0</li>
	 * <li>alternate textures : 1</li>
	 * </ul>
	 * @param meshIndex
	 */
	public void setMeshIndex(int meshIndex)
	{
		_meshIndex = meshIndex;
		broadcastPacket(new StaticObjectInfo(this));
	}
	
	/**
	 * <b><u>Values</u>:</b>
	 * <ul>
	 * <li>default textures : 0</li>
	 * <li>alternate textures : 1</li>
	 * </ul>
	 * @return the meshIndex of the object
	 */
	public int getMeshIndex()
	{
		return _meshIndex;
	}
	
	public override void sendInfo(Player player)
	{
		player.sendPacket(new StaticObjectInfo(this));
	}
	
	public override void moveToLocation(int x, int y, int z, int offset)
	{
	}
	
	public override void stopMove(Location loc)
	{
	}
	
	public override void doAutoAttack(Creature target)
	{
	}
	
	public override void doCast(Skill skill)
	{
	}
}
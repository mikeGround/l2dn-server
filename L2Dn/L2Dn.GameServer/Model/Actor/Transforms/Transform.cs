﻿using L2Dn.GameServer.Data.Xml;
using L2Dn.GameServer.Enums;
using L2Dn.GameServer.Model.Events;
using L2Dn.GameServer.Model.Events.Impl.Creatures.Players;
using L2Dn.GameServer.Model.Holders;
using L2Dn.GameServer.Model.Interfaces;
using L2Dn.GameServer.Model.Items.Types;
using L2Dn.GameServer.Model.Skills;
using L2Dn.GameServer.Model.Stats;
using L2Dn.GameServer.Utilities;

namespace L2Dn.GameServer.Model.Actor.Transforms;

public class Transform: IIdentifiable
{
	private readonly int _id;
	private readonly int _displayId;
	private readonly TransformType _type;
	private readonly bool _canSwim;
	private readonly int _spawnHeight;
	private readonly bool _canAttack;
	private readonly String _name;
	private readonly String _title;
	
	private TransformTemplate _maleTemplate;
	private TransformTemplate _femaleTemplate;
	
	public Transform(StatSet set)
	{
		_id = set.getInt("id");
		_displayId = set.getInt("displayId", _id);
		_type = set.getEnum<TransformType>("type", TransformType.COMBAT);
		_canSwim = set.getInt("can_swim", 0) == 1;
		_canAttack = set.getInt("normal_attackable", 1) == 1;
		_spawnHeight = set.getInt("spawn_height", 0);
		_name = set.getString("setName", null);
		_title = set.getString("setTitle", null);
	}
	
	/**
	 * Gets the transformation ID.
	 * @return the transformation ID
	 */
	public int getId()
	{
		return _id;
	}
	
	public int getDisplayId()
	{
		return _displayId;
	}
	
	public TransformType getType()
	{
		return _type;
	}
	
	public bool canSwim()
	{
		return _canSwim;
	}
	
	public bool canAttack()
	{
		return _canAttack;
	}
	
	public int getSpawnHeight()
	{
		return _spawnHeight;
	}
	
	/**
	 * @return name that's going to be set to the player while is transformed with current transformation
	 */
	public String getName()
	{
		return _name;
	}
	
	/**
	 * @return title that's going to be set to the player while is transformed with current transformation
	 */
	public String getTitle()
	{
		return _title;
	}
	
	public TransformTemplate getTemplate(Creature creature)
	{
		if (creature.isPlayer())
		{
			return (creature.getActingPlayer().getAppearance().isFemale() ? _femaleTemplate : _maleTemplate);
		}
		else if (creature.isNpc())
		{
			return ((Npc) creature).getTemplate().getSex() == Sex.FEMALE ? _femaleTemplate : _maleTemplate;
		}
		return null;
	}
	
	public void setTemplate(bool male, TransformTemplate template)
	{
		if (male)
		{
			_maleTemplate = template;
		}
		else
		{
			_femaleTemplate = template;
		}
	}
	
	/**
	 * @return {@code true} if transform type is mode change, {@code false} otherwise
	 */
	public bool isStance()
	{
		return _type == TransformType.MODE_CHANGE;
	}
	
	/**
	 * @return {@code true} if transform type is combat, {@code false} otherwise
	 */
	public bool isCombat()
	{
		return _type == TransformType.COMBAT;
	}
	
	/**
	 * @return {@code true} if transform type is non combat, {@code false} otherwise
	 */
	public bool isNonCombat()
	{
		return _type == TransformType.NON_COMBAT;
	}
	
	/**
	 * @return {@code true} if transform type is flying, {@code false} otherwise
	 */
	public bool isFlying()
	{
		return _type == TransformType.FLYING;
	}
	
	/**
	 * @return {@code true} if transform type is cursed, {@code false} otherwise
	 */
	public bool isCursed()
	{
		return _type == TransformType.CURSED;
	}
	
	/**
	 * @return {@code true} if transform type is raiding, {@code false} otherwise
	 */
	public bool isRiding()
	{
		return _type == TransformType.RIDING_MODE;
	}
	
	/**
	 * @return {@code true} if transform type is pure stat, {@code false} otherwise
	 */
	public bool isPureStats()
	{
		return _type == TransformType.PURE_STAT;
	}
	
	public float getCollisionHeight(Creature creature, float defaultCollisionHeight)
	{
		TransformTemplate template = getTemplate(creature);
		if ((template != null) && (template.getCollisionHeight() != null))
		{
			return template.getCollisionHeight();
		}
		return defaultCollisionHeight;
	}
	
	public float getCollisionRadius(Creature creature, float defaultCollisionRadius)
	{
		TransformTemplate template = getTemplate(creature);
		if ((template != null) && (template.getCollisionRadius() != null))
		{
			return template.getCollisionRadius();
		}
		return defaultCollisionRadius;
	}
	
	public void onTransform(Creature creature, bool addSkills)
	{
		// Abort attacking and casting.
		creature.abortAttack();
		creature.abortCast();
		
		Player player = creature.getActingPlayer();
		
		// Get off the strider or something else if character is mounted
		if (creature.isPlayer() && player.isMounted())
		{
			player.dismount();
		}
		
		TransformTemplate template = getTemplate(creature);
		if (template != null)
		{
			// Start flying.
			if (isFlying())
			{
				creature.setFlying(true);
			}
			
			// Get player a bit higher so he doesn't drops underground after transformation happens
			creature.setXYZ(creature.getX(), creature.getY(), (int) (creature.getZ() + getCollisionHeight(creature, 0)));
			if (creature.isPlayer())
			{
				if (_name != null)
				{
					player.getAppearance().setVisibleName(_name);
				}
				if (_title != null)
				{
					player.getAppearance().setVisibleTitle(_title);
				}
				
				if (addSkills)
				{
					// Add common skills.
					foreach (SkillHolder h in template.getSkills())
					{
						player.addTransformSkill(h.getSkill());
					}
					
					// Add skills depending on level.
					foreach (AdditionalSkillHolder h in template.getAdditionalSkills())
					{
						if (player.getLevel() >= h.getMinLevel())
						{
							player.addTransformSkill(h.getSkill());
						}
					}
					
					// Add collection skills.
					foreach (SkillLearn s in SkillTreeData.getInstance().getCollectSkillTree().values())
					{
						Skill skill = player.getKnownSkill(s.getSkillId());
						if (skill != null)
						{
							player.addTransformSkill(skill);
						}
					}
				}
				
				// Set inventory blocks if needed.
				if (!template.getAdditionalItems().isEmpty())
				{
					 List<int> allowed = new();
					 List<int> notAllowed = new();
					foreach (AdditionalItemHolder holder in template.getAdditionalItems())
					{
						if (holder.isAllowedToUse())
						{
							allowed.Add(holder.getId());
						}
						else
						{
							notAllowed.Add(holder.getId());
						}
					}
					
					if (!allowed.isEmpty())
					{
						player.getInventory().setInventoryBlock(allowed, InventoryBlockType.WHITELIST);
					}
					
					if (!notAllowed.isEmpty())
					{
						player.getInventory().setInventoryBlock(notAllowed, InventoryBlockType.BLACKLIST);
					}
				}
				
				// Send basic action list.
				if (template.hasBasicActionList())
				{
					player.sendPacket(new ExBasicActionList(template.getBasicActionList()));
				}
				
				player.getEffectList().stopAllToggles();
				
				if (player.hasTransformSkills())
				{
					player.sendSkillList();
					player.sendPacket(new SkillCoolTime(player));
				}
				
				player.broadcastUserInfo();
				
				// Notify to scripts
				if (EventDispatcher.getInstance().hasListener(EventType.ON_PLAYER_TRANSFORM, player))
				{
					EventDispatcher.getInstance().notifyEventAsync(new OnPlayerTransform(player, getId()), player);
				}
			}
			else
			{
				creature.broadcastInfo();
			}
			
			// I don't know why, but you need to broadcast this to trigger the transformation client-side.
			// Usually should be sent naturally after applying effect, but sometimes is sent before that... i just dont know...
			creature.updateAbnormalVisualEffects();
		}
	}
	
	public void onUntransform(Creature creature)
	{
		// Abort attacking and casting.
		creature.abortAttack();
		creature.abortCast();
		
		TransformTemplate template = getTemplate(creature);
		if (template != null)
		{
			// Stop flying.
			if (isFlying())
			{
				creature.setFlying(false);
			}
			
			if (creature.isPlayer())
			{
				Player player = creature.getActingPlayer();
				bool hasTransformSkills = player.hasTransformSkills();
				if (_name != null)
				{
					player.getAppearance().setVisibleName(null);
				}
				if (_title != null)
				{
					player.getAppearance().setVisibleTitle(null);
				}
				
				// Remove transformation skills.
				player.removeAllTransformSkills();
				
				// Remove inventory blocks if needed.
				if (!template.getAdditionalItems().isEmpty())
				{
					player.getInventory().unblock();
				}
				
				player.sendPacket(ExBasicActionList.STATIC_PACKET);
				
				if (!player.getEffectList().stopEffects(AbnormalType.TRANSFORM))
				{
					player.getEffectList().stopEffects(AbnormalType.CHANGEBODY);
				}
				
				if (hasTransformSkills)
				{
					player.sendSkillList();
					player.sendPacket(new SkillCoolTime(player));
				}
				
				player.broadcastUserInfo();
				player.sendPacket(new ExUserInfoEquipSlot(player));
				
				// Notify to scripts
				if (EventDispatcher.getInstance().hasListener(EventType.ON_PLAYER_TRANSFORM, player))
				{
					EventDispatcher.getInstance().notifyEventAsync(new OnPlayerTransform(player, 0), player);
				}
			}
			else
			{
				creature.broadcastInfo();
			}
		}
	}
	
	public void onLevelUp(Player player)
	{
		// Add skills depending on level.
		TransformTemplate template = getTemplate(player);
		if ((template != null) && !template.getAdditionalSkills().isEmpty())
		{
			foreach (AdditionalSkillHolder holder in template.getAdditionalSkills())
			{
				if ((player.getLevel() >= holder.getMinLevel()) && (player.getSkillLevel(holder.getSkillId()) < holder.getSkillLevel()))
				{
					player.addTransformSkill(holder.getSkill());
				}
			}
		}
	}
	
	public WeaponType getBaseAttackType(Creature creature, WeaponType defaultAttackType)
	{
		TransformTemplate template = getTemplate(creature);
		if (template != null)
		{
			WeaponType weaponType = template.getBaseAttackType();
			if (weaponType != null)
			{
				return weaponType;
			}
		}
		return defaultAttackType;
	}
	
	public double getStats(Creature creature, Stat stat, double defaultValue)
	{
		double val = defaultValue;
		TransformTemplate template = getTemplate(creature);
		if (template != null)
		{
			val = template.getStats(stat, defaultValue);
			TransformLevelData data = template.getData(creature.getLevel());
			if (data != null)
			{
				val = data.getStats(stat, defaultValue);
			}
		}
		return val;
	}
	
	public int getBaseDefBySlot(Player player, int slot)
	{
		int defaultValue = player.getTemplate().getBaseDefBySlot(slot);
		TransformTemplate template = getTemplate(player);
		return template == null ? defaultValue : template.getDefense(slot, defaultValue);
	}
	
	/**
	 * @param creature
	 * @return {@code -1} if this transformation doesn't alter levelmod, otherwise a new levelmod will be returned.
	 */
	public double getLevelMod(Creature creature)
	{
		double val = 1;
		TransformTemplate template = getTemplate(creature);
		if (template != null)
		{
			TransformLevelData data = template.getData(creature.getLevel());
			if (data != null)
			{
				val = data.getLevelMod();
			}
		}
		return val;
	}
}

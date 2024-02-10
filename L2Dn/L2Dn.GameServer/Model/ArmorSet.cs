﻿using L2Dn.GameServer.Model.Actor;
using L2Dn.GameServer.Model.Holders;
using L2Dn.GameServer.Model.ItemContainers;
using L2Dn.GameServer.Model.Items.Instances;
using L2Dn.GameServer.Model.Stats;
using L2Dn.GameServer.Utilities;

namespace L2Dn.GameServer.Model;

public class ArmorSet
{
	private readonly int _id;
	private readonly int _minimumPieces;
	private readonly bool _isVisual;
	
	private readonly int[] _requiredItems;
	private readonly int[] _optionalItems;
	
	private readonly List<ArmorsetSkillHolder> _skills;
	private readonly Map<BaseStat, Double> _stats;
	
	private static readonly int[] ARMORSET_SLOTS = new int[]
	{
		Inventory.PAPERDOLL_CHEST,
		Inventory.PAPERDOLL_LEGS,
		Inventory.PAPERDOLL_HEAD,
		Inventory.PAPERDOLL_GLOVES,
		Inventory.PAPERDOLL_FEET
	};
	private static readonly  int[] ARTIFACT_1_SLOTS = new int[]
	{
		Inventory.PAPERDOLL_ARTIFACT1,
		Inventory.PAPERDOLL_ARTIFACT2,
		Inventory.PAPERDOLL_ARTIFACT3,
		Inventory.PAPERDOLL_ARTIFACT4,
		Inventory.PAPERDOLL_ARTIFACT13,
		Inventory.PAPERDOLL_ARTIFACT16,
		Inventory.PAPERDOLL_ARTIFACT19,
	
	};
	private static readonly  int[] ARTIFACT_2_SLOTS = new int[]
	{
		Inventory.PAPERDOLL_ARTIFACT5,
		Inventory.PAPERDOLL_ARTIFACT6,
		Inventory.PAPERDOLL_ARTIFACT7,
		Inventory.PAPERDOLL_ARTIFACT8,
		Inventory.PAPERDOLL_ARTIFACT14,
		Inventory.PAPERDOLL_ARTIFACT17,
		Inventory.PAPERDOLL_ARTIFACT20,
	
	};
	private static readonly  int[] ARTIFACT_3_SLOTS = new int[]
	{
		Inventory.PAPERDOLL_ARTIFACT9,
		Inventory.PAPERDOLL_ARTIFACT10,
		Inventory.PAPERDOLL_ARTIFACT11,
		Inventory.PAPERDOLL_ARTIFACT12,
		Inventory.PAPERDOLL_ARTIFACT15,
		Inventory.PAPERDOLL_ARTIFACT18,
		Inventory.PAPERDOLL_ARTIFACT21,
	
	};
	
	public ArmorSet(int id, int minimumPieces, bool isVisual, Set<int> requiredItems, Set<int> optionalItems, List<ArmorsetSkillHolder> skills, Map<BaseStat, Double> stats)
	{
		_id = id;
		_minimumPieces = minimumPieces;
		_isVisual = isVisual;
		_requiredItems = requiredItems.stream().mapToInt(x => x).toArray();
		_optionalItems = optionalItems.stream().mapToInt(x => x).toArray();
		_skills = skills;
		_stats = stats;
	}
	
	public int getId()
	{
		return _id;
	}
	
	/**
	 * @return the minimum amount of pieces equipped to form a set
	 */
	public int getMinimumPieces()
	{
		return _minimumPieces;
	}
	
	/**
	 * @return {@code true} if the set is visual only, {@code} otherwise
	 */
	public bool isVisual()
	{
		return _isVisual;
	}
	
	/**
	 * @return the set of items that can form a set
	 */
	public int[] getRequiredItems()
	{
		return _requiredItems;
	}
	
	/**
	 * @return the set of shields
	 */
	public int[] getOptionalItems()
	{
		return _optionalItems;
	}
	
	/**
	 * The list of skills that are activated when set reaches it's minimum equipped items condition
	 * @return
	 */
	public List<ArmorsetSkillHolder> getSkills()
	{
		return _skills;
	}
	
	/**
	 * @param stat
	 * @return the stats bonus value or 0 if doesn't exists
	 */
	public double getStatsBonus(BaseStat stat)
	{
		return _stats.getOrDefault(stat, 0d);
	}
	
	/**
	 * @param shieldId
	 * @return {@code true} if player has the shield of this set equipped, {@code false} in case set doesn't have a shield or player doesn't
	 */
	public bool containOptionalItem(int shieldId)
	{
		return CommonUtil.contains(_optionalItems, shieldId);
	}
	
	/**
	 * @param playable
	 * @return true if all parts of set are enchanted to +6 or more
	 */
	public int getLowestSetEnchant(Playable playable)
	{
		// Playable don't have full set
		if (getPiecesCountById(playable) < _minimumPieces)
		{
			return 0;
		}
		
		Inventory inv = playable.getInventory();
		int enchantLevel = sbyte.MaxValue;
		foreach (int armorSlot in ARMORSET_SLOTS)
		{
			Item itemPart = inv.getPaperdollItem(armorSlot);
			if ((itemPart != null) && CommonUtil.contains(_requiredItems, itemPart.getId()) && (enchantLevel > itemPart.getEnchantLevel()))
			{
				enchantLevel = itemPart.getEnchantLevel();
			}
		}
		if (enchantLevel == sbyte.MaxValue)
		{
			enchantLevel = 0;
		}
		return enchantLevel;
	}
	
	/**
	 * Condition for 3 Lv. Set Effect Applied Skill
	 * @param playable
	 * @param bookSlot
	 * @return total paperdoll(busy) count for 1 of 3 artifact book slots
	 */
	public int getArtifactSlotMask(Playable playable, int bookSlot)
	{
		Inventory inv = playable.getInventory();
		int slotMask = 0;
		switch (bookSlot)
		{
			case 1:
			{
				foreach (int artifactSlot in ARTIFACT_1_SLOTS)
				{
					Item itemPart = inv.getPaperdollItem(artifactSlot);
					if ((itemPart != null) && CommonUtil.contains(_requiredItems, itemPart.getId()))
					{
						slotMask += artifactSlot;
					}
				}
				break;
			}
			case 2:
			{
				foreach (int artifactSlot in ARTIFACT_2_SLOTS)
				{
					Item itemPart = inv.getPaperdollItem(artifactSlot);
					if ((itemPart != null) && CommonUtil.contains(_requiredItems, itemPart.getId()))
					{
						slotMask += artifactSlot;
					}
				}
				break;
			}
			case 3:
			{
				foreach (int artifactSlot in ARTIFACT_3_SLOTS)
				{
					Item itemPart = inv.getPaperdollItem(artifactSlot);
					if ((itemPart != null) && CommonUtil.contains(_requiredItems, itemPart.getId()))
					{
						slotMask += artifactSlot;
					}
				}
				break;
			}
		}
		return slotMask;
	}
	
	public bool hasOptionalEquipped(Playable playable, Func<Item, int> idProvider)
	{
		foreach (Item item in playable.getInventory().getPaperdollItems())
		{
			if (CommonUtil.contains(_optionalItems, idProvider(item)))
			{
				return true;
			}
		}
		return false;
	}
	
	/**
	 * @param playable
	 * @param idProvider
	 * @return the amount of set visual items that playable has equipped
	 */
	public long getPiecesCount(Playable playable, Func<Item, int> idProvider)
	{
		return playable.getInventory().getPaperdollItemCount(item => CommonUtil.contains(_requiredItems, idProvider(item)));
	}
	
	public long getPiecesCountById(Playable playable)
	{
		return playable.getInventory().getPaperdollItemCount(item => CommonUtil.contains(_requiredItems, item.getId()));
	}
}

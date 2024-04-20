using L2Dn.GameServer.Data.Xml;
using L2Dn.GameServer.Model;
using L2Dn.GameServer.Model.Actor;
using L2Dn.GameServer.Model.Items.Instances;
using L2Dn.GameServer.Model.Stats;
using L2Dn.GameServer.Utilities;
using L2Dn.Model.Enums;

namespace L2Dn.GameServer.Cache;

/**
 * @author Sahar
 */
public class PaperdollCache
{
	private readonly Set<Item> _paperdollItems = new();
	
	private readonly Map<BaseStat, Double> _baseStatValues = new();
	private readonly Map<Stat, Double> _statValues = new();
	private int _maxSetEnchant = -1;
	
	public Set<Item> getPaperdollItems()
	{
		return _paperdollItems;
	}
	
	public void clearCachedStats()
	{
		_baseStatValues.clear();
		_statValues.clear();
		
		clearMaxSetEnchant();
	}
	
	public void clearMaxSetEnchant()
	{
		_maxSetEnchant = -1;
	}
	
	public double getBaseStatValue(Player player, BaseStat stat)
	{
		Double baseStatValue = _baseStatValues.get(stat);
		if (baseStatValue != null)
		{
			return baseStatValue;
		}
		
		Set<ArmorSet> appliedSets = new();
		double value = 0;
		foreach (Item item in _paperdollItems)
		{
			foreach (ArmorSet set in ArmorSetData.getInstance().getSets(item.getId()))
			{
				if ((set.getPiecesCountById(player) >= set.getMinimumPieces()) && appliedSets.add(set))
				{
					value += set.getStatsBonus(stat);
				}
			}
		}
		
		_baseStatValues.put(stat, value);
		return value;
	}
	
	public int getMaxSetEnchant(Playable playable)
	{
		if (_maxSetEnchant >= 0)
		{
			return _maxSetEnchant;
		}
		
		int maxSetEnchant = 0;
		foreach (Item item in _paperdollItems)
		{
			foreach (ArmorSet set in ArmorSetData.getInstance().getSets(item.getId()))
			{
				int enchantEffect = set.getLowestSetEnchant(playable);
				if (enchantEffect > maxSetEnchant)
				{
					maxSetEnchant = enchantEffect;
				}
			}
		}
		
		_maxSetEnchant = maxSetEnchant;
		return maxSetEnchant;
	}
	
	public double getStats(Stat stat)
	{
		Double statValue = _statValues.get(stat);
		if (statValue != null)
		{
			return statValue;
		}
		
		double value = 0;
		foreach (Item item in _paperdollItems)
		{
			value += item.getTemplate().getStats(stat, 0);
		}
		
		_statValues.put(stat, value);
		return value;
	}
}
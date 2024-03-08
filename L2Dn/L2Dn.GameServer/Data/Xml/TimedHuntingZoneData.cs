using System.Xml.Linq;
using L2Dn.Extensions;
using L2Dn.GameServer.Model;
using L2Dn.GameServer.Model.Holders;
using L2Dn.GameServer.Utilities;
using L2Dn.Utilities;
using NLog;

namespace L2Dn.GameServer.Data.Xml;

/**
 * @author Mobius
 */
public class TimedHuntingZoneData: DataReaderBase
{
	private static readonly Logger LOGGER = LogManager.GetLogger(nameof(TimedHuntingZoneData));
	
	private readonly Map<int, TimedHuntingZoneHolder> _timedHuntingZoneData = new();
	
	protected TimedHuntingZoneData()
	{
		load();
	}
	
	public void load()
	{
		_timedHuntingZoneData.clear();
        
		XDocument document = LoadXmlDocument(DataFileLocation.Data, "TimedHuntingZoneData.xml");
		document.Elements("list").Where(e => e.Attribute("enabled").GetBoolean(false)).Elements("zone")
			.ForEach(parseElement);
		
		if (!_timedHuntingZoneData.isEmpty())
		{
			LOGGER.Info(GetType().Name + ": Loaded " + _timedHuntingZoneData.size() + " timed hunting zones.");
		}
		else
		{
			LOGGER.Info(GetType().Name + ": System is disabled.");
		}
	}

	private void parseElement(XElement element)
	{
		int id = element.GetAttributeValueAsInt32("id");
		String name = element.Attribute("name").GetString("");
		int initialTime = 0;
		int maxAddedTime = 0;
		int resetDelay = 0;
		int entryItemId = 57;
		int entryFee = 10000;
		int minLevel = 1;
		int maxLevel = 999;
		int remainRefillTime = 3600;
		int refillTimeMax = 3600;
		bool pvpZone = false;
		bool noPvpZone = false;
		int instanceId = 0;
		bool soloInstance = true;
		bool weekly = false;
		bool useWorldPrefix = false;
		bool zonePremiumUserOnly = false;
		Location enterLocation = null;
		Location subEnterLocation1 = null;
		Location subEnterLocation2 = null;
		Location subEnterLocation3 = null;
		Location exitLocation = null;

		element.Elements("enterLocation").ForEach(el =>
		{
			String[] coordinates = ((string)el).Split(",");
			enterLocation = new Location(int.Parse(coordinates[0]), int.Parse(coordinates[1]),
				int.Parse(coordinates[2]));
		});

		element.Elements("subEnterLocation1").ForEach(el =>
		{
			String[] coordinates = ((string)el).Split(",");
			subEnterLocation1 = new Location(int.Parse(coordinates[0]), int.Parse(coordinates[1]),
				int.Parse(coordinates[2]));
		});

		element.Elements("subEnterLocation2").ForEach(el =>
		{
			String[] coordinates = ((string)el).Split(",");
			subEnterLocation2 = new Location(int.Parse(coordinates[0]), int.Parse(coordinates[1]),
				int.Parse(coordinates[2]));
		});

		element.Elements("subEnterLocation3").ForEach(el =>
		{
			String[] coordinates = ((string)el).Split(",");
			subEnterLocation3 = new Location(int.Parse(coordinates[0]), int.Parse(coordinates[1]),
				int.Parse(coordinates[2]));
		});

		element.Elements("exitLocation").ForEach(el =>
		{
			String[] coordinates = ((string)el).Split(",");
			exitLocation = new Location(int.Parse(coordinates[0]), int.Parse(coordinates[1]),
				int.Parse(coordinates[2]));
		});

		element.Elements("initialTime").ForEach(el => initialTime = (int)el * 1000);
		element.Elements("maxAddedTime").ForEach(el => maxAddedTime = (int)el * 1000);
		element.Elements("resetDelay").ForEach(el => resetDelay = (int)el * 1000);
		element.Elements("entryItemId").ForEach(el => entryItemId = (int)el);
		element.Elements("entryFee").ForEach(el => entryFee = (int)el);
		element.Elements("minLevel").ForEach(el => minLevel = (int)el);
		element.Elements("maxLevel").ForEach(el => maxLevel = (int)el);
		element.Elements("remainRefillTime").ForEach(el => remainRefillTime = (int)el);
		element.Elements("refillTimeMax").ForEach(el => refillTimeMax = (int)el);
		element.Elements("pvpZone").ForEach(el => pvpZone = (bool)el);
		element.Elements("noPvpZone").ForEach(el => noPvpZone = (bool)el);
		element.Elements("instanceId").ForEach(el => instanceId = (int)el);
		element.Elements("soloInstance").ForEach(el => soloInstance = (bool)el);
		element.Elements("weekly").ForEach(el => weekly = (bool)el);
		element.Elements("useWorldPrefix").ForEach(el => useWorldPrefix = (bool)el);
		element.Elements("zonePremiumUserOnly").ForEach(el => zonePremiumUserOnly = (bool)el);

		_timedHuntingZoneData.put(id,
			new TimedHuntingZoneHolder(id, name, initialTime, maxAddedTime, TimeSpan.FromMilliseconds(resetDelay), entryItemId, entryFee, minLevel,
				maxLevel, remainRefillTime, refillTimeMax, pvpZone, noPvpZone, instanceId, soloInstance, weekly,
				useWorldPrefix, zonePremiumUserOnly, enterLocation, subEnterLocation1, subEnterLocation2,
				subEnterLocation3, exitLocation));
	}

	public TimedHuntingZoneHolder getHuntingZone(int zoneId)
	{
		return _timedHuntingZoneData.get(zoneId);
	}
	
	public ICollection<TimedHuntingZoneHolder> getAllHuntingZones()
	{
		return _timedHuntingZoneData.values();
	}
	
	public int getSize()
	{
		return _timedHuntingZoneData.size();
	}
	
	public static TimedHuntingZoneData getInstance()
	{
		return SingletonHolder.INSTANCE;
	}
	
	private static class SingletonHolder
	{
		public static readonly TimedHuntingZoneData INSTANCE = new();
	}
}
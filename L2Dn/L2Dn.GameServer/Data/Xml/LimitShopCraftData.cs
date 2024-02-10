using L2Dn.GameServer.Model.Holders;
using L2Dn.GameServer.Model.Items;
using L2Dn.GameServer.Utilities;
using NLog;

namespace L2Dn.GameServer.Data.Xml;

/**
 * @author Mobius, GustavoFonseca
 */
public class LimitShopCraftData
{
	private static readonly Logger LOGGER = LogManager.GetLogger(nameof(LimitShopData));
	
	private readonly List<LimitShopProductHolder> _products = new();
	
	protected LimitShopCraftData()
	{
		load();
	}
	
	public void load()
	{
		_products.Clear();
		parseDatapackFile("data/LimitShopCraft.xml");
		
		if (!_products.isEmpty())
		{
			LOGGER.Info(GetType().Name + ": Loaded " + _products.size() + " items.");
		}
		else
		{
			LOGGER.Info(GetType().Name + ": System is disabled.");
		}
	}
	
	public void parseDocument(Document doc, File f)
	{
		for (Node n = doc.getFirstChild(); n != null; n = n.getNextSibling())
		{
			if ("list".equalsIgnoreCase(n.getNodeName()))
			{
				NamedNodeMap at = n.getAttributes();
				Node attribute = at.getNamedItem("enabled");
				if ((attribute != null) && Boolean.parseBoolean(attribute.getNodeValue()))
				{
					for (Node d = n.getFirstChild(); d != null; d = d.getNextSibling())
					{
						if ("product".equalsIgnoreCase(d.getNodeName()))
						{
							NamedNodeMap attrs = d.getAttributes();
							Node att;
							StatSet set = new StatSet();
							for (int i = 0; i < attrs.getLength(); i++)
							{
								att = attrs.item(i);
								set.set(att.getNodeName(), att.getNodeValue());
							}
							
							int id = parseInteger(attrs, "id");
							int category = parseInteger(attrs, "category");
							int minLevel = parseInteger(attrs, "minLevel", 1);
							int maxLevel = parseInteger(attrs, "maxLevel", 999);
							int[] ingredientIds = new int[5];
							ingredientIds[0] = 0;
							ingredientIds[1] = 0;
							ingredientIds[2] = 0;
							ingredientIds[3] = 0;
							ingredientIds[4] = 0;
							long[] ingredientQuantities = new long[5];
							ingredientQuantities[0] = 0;
							ingredientQuantities[1] = 0;
							ingredientQuantities[2] = 0;
							ingredientQuantities[3] = 0;
							ingredientQuantities[4] = 0;
							int[] ingredientEnchants = new int[5];
							ingredientEnchants[0] = 0;
							ingredientEnchants[1] = 0;
							ingredientEnchants[2] = 0;
							ingredientEnchants[3] = 0;
							ingredientEnchants[4] = 0;
							int productionId = 0;
							int productionId2 = 0;
							int productionId3 = 0;
							int productionId4 = 0;
							int productionId5 = 0;
							long count = 1L;
							long count2 = 1L;
							long count3 = 1L;
							long count4 = 1L;
							long count5 = 1L;
							float chance = 100f;
							float chance2 = 100f;
							float chance3 = 100f;
							float chance4 = 100f;
							bool announce = false;
							bool announce2 = false;
							bool announce3 = false;
							bool announce4 = false;
							bool announce5 = false;
							int enchant = 0;
							int accountDailyLimit = 0;
							int accountMontlyLimit = 0;
							int accountBuyLimit = 0;
							for (Node b = d.getFirstChild(); b != null; b = b.getNextSibling())
							{
								attrs = b.getAttributes();
								
								if ("ingredient".equalsIgnoreCase(b.getNodeName()))
								{
									int ingredientId = parseInteger(attrs, "id");
									long ingredientQuantity = Parse(attrs, "count", 1L);
									int ingredientEnchant = parseInteger(attrs, "enchant", 0);
									
									if (ingredientId > 0)
									{
										ItemTemplate item = ItemData.getInstance().getTemplate(ingredientId);
										if (item == null)
										{
											LOGGER.Error(GetType().Name + ": Item template null for itemId: " + productionId + " productId: " + id);
											continue;
										}
									}
									
									if (ingredientIds[0] == 0)
									{
										ingredientIds[0] = ingredientId;
									}
									else if (ingredientIds[1] == 0)
									{
										ingredientIds[1] = ingredientId;
									}
									else if (ingredientIds[2] == 0)
									{
										ingredientIds[2] = ingredientId;
									}
									else if (ingredientIds[3] == 0)
									{
										ingredientIds[3] = ingredientId;
									}
									else
									{
										ingredientIds[4] = ingredientId;
									}
									
									if (ingredientQuantities[0] == 0)
									{
										ingredientQuantities[0] = ingredientQuantity;
									}
									else if (ingredientQuantities[1] == 0)
									{
										ingredientQuantities[1] = ingredientQuantity;
									}
									else if (ingredientQuantities[2] == 0)
									{
										ingredientQuantities[2] = ingredientQuantity;
									}
									else if (ingredientQuantities[3] == 0)
									{
										ingredientQuantities[3] = ingredientQuantity;
									}
									else
									{
										ingredientQuantities[4] = ingredientQuantity;
									}
									
									if (ingredientEnchants[0] == 0)
									{
										ingredientEnchants[0] = ingredientEnchant;
									}
									else if (ingredientEnchants[1] == 0)
									{
										ingredientEnchants[1] = ingredientEnchant;
									}
									else if (ingredientEnchants[2] == 0)
									{
										ingredientEnchants[2] = ingredientEnchant;
									}
									else if (ingredientEnchants[3] == 0)
									{
										ingredientEnchants[3] = ingredientEnchant;
									}
									else
									{
										ingredientEnchants[4] = ingredientEnchant;
									}
								}
								else if ("production".equalsIgnoreCase(b.getNodeName()))
								{
									productionId = parseInteger(attrs, "id");
									count = Parse(attrs, "count", 1L);
									chance = parseFloat(attrs, "chance", 100f);
									announce = parseBoolean(attrs, "announce", false);
									enchant = parseInteger(attrs, "enchant", 0);
									productionId2 = parseInteger(attrs, "id2", 0);
									count2 = Parse(attrs, "count2", 1L);
									chance2 = parseFloat(attrs, "chance2", 100f);
									announce2 = parseBoolean(attrs, "announce2", false);
									productionId3 = parseInteger(attrs, "id3", 0);
									count3 = Parse(attrs, "count3", 1L);
									chance3 = parseFloat(attrs, "chance3", 100f);
									announce3 = parseBoolean(attrs, "announce3", false);
									productionId4 = parseInteger(attrs, "id4", 0);
									count4 = Parse(attrs, "count4", 1L);
									chance4 = parseFloat(attrs, "chance4", 100f);
									announce4 = parseBoolean(attrs, "announce4", false);
									productionId5 = parseInteger(attrs, "id5", 0);
									count5 = Parse(attrs, "count5", 1L);
									announce5 = parseBoolean(attrs, "announce5", false);
									accountDailyLimit = parseInteger(attrs, "accountDailyLimit", 0);
									accountMontlyLimit = parseInteger(attrs, "accountMontlyLimit", 0);
									accountBuyLimit = parseInteger(attrs, "accountBuyLimit", 0);
									
									ItemTemplate item = ItemData.getInstance().getTemplate(productionId);
									if (item == null)
									{
										LOGGER.Error(GetType().Name + ": Item template null for itemId: " + productionId + " productId: " + id);
										continue;
									}
								}
							}
							
							_products.add(new LimitShopProductHolder(id, category, minLevel, maxLevel, ingredientIds, ingredientQuantities, ingredientEnchants, productionId, count, chance, announce, enchant, productionId2, count2, chance2, announce2, productionId3, count3, chance3, announce3, productionId4, count4, chance4, announce4, productionId5, count5, announce5, accountDailyLimit, accountMontlyLimit, accountBuyLimit));
						}
					}
				}
			}
		}
	}
	
	public LimitShopProductHolder getProduct(int id)
	{
		foreach (LimitShopProductHolder product in _products)
		{
			if (product.getId() == id)
			{
				return product;
			}
		}
		return null;
	}
	
	public List<LimitShopProductHolder> getProducts()
	{
		return _products;
	}
	
	public static LimitShopCraftData getInstance()
	{
		return SingletonHolder.INSTANCE;
	}
	
	private static class SingletonHolder
	{
		public static readonly LimitShopCraftData INSTANCE = new();
	}
}
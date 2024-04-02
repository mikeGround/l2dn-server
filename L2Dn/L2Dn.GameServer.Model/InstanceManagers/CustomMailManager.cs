using L2Dn.GameServer.Db;
using L2Dn.GameServer.Enums;
using L2Dn.GameServer.Model;
using L2Dn.GameServer.Model.Actor;
using L2Dn.GameServer.Model.Holders;
using L2Dn.GameServer.Model.ItemContainers;
using L2Dn.GameServer.Utilities;
using Microsoft.EntityFrameworkCore;
using NLog;
using ThreadPool = L2Dn.GameServer.Utilities.ThreadPool;

namespace L2Dn.GameServer.InstanceManagers;

/**
 * @author Mobius
 */
public class CustomMailManager
{
	private static readonly Logger LOGGER = LogManager.GetLogger(nameof(CustomMailManager));
	
	protected CustomMailManager()
	{
		ThreadPool.scheduleAtFixedRate(() =>
		{
			try 
			{
				using GameServerDbContext ctx = DbFactory.Instance.CreateDbContext();
				foreach (DbCustomMail record in ctx.CustomMails)
				{
					int playerId = record.ReceiverId;
					Player player = World.getInstance().getPlayer(playerId);
					if ((player != null) && player.isOnline())
					{
						// Create message.
						String items = record.Items;
						Message msg = new Message(playerId, record.Subject, record.Message, items.Length > 0 ? MailType.PRIME_SHOP_GIFT : MailType.REGULAR);
						List<ItemHolder> itemHolders = new();
						foreach (String str in items.Split(";"))
						{
							if (str.Contains(" "))
							{
								String itemId = str.Split(" ")[0];
								String itemCount = str.Split(" ")[1];
								if (Util.isDigit(itemId) && Util.isDigit(itemCount))
								{
									itemHolders.add(new ItemHolder(int.Parse(itemId), long.Parse(itemCount)));
								}
							}
							else if (Util.isDigit(str))
							{
								itemHolders.add(new ItemHolder(int.Parse(str), 1));
							}
						}
						if (!itemHolders.isEmpty())
						{
							Mail attachments = msg.createAttachments();
							foreach (ItemHolder itemHolder in itemHolders)
							{
								attachments.addItem("Custom-Mail", itemHolder.getId(), itemHolder.getCount(), null, null);
							}
						}
						
						// Delete entry from database.
						try
						{
							DateTime time = record.Time;

							// TODO: deletion during querying, refactor this
							ctx.CustomMails.Where(m => m.Time == time && m.ReceiverId == playerId).ExecuteDelete();
							
						}
						catch (Exception e)
						{
							LOGGER.Warn(GetType().Name + ": Error deleting entry from database: " + e);
						}
						
						// Send message.
						MailManager.getInstance().sendMessage(msg);
						LOGGER.Info(GetType().Name +": Message sent to " + player.getName() + ".");
					}
				}
			}
			catch (Exception e)
			{
				LOGGER.Warn(GetType().Name + ": Error reading from database: " + e);
			}
		}, Config.CUSTOM_MAIL_MANAGER_DELAY, Config.CUSTOM_MAIL_MANAGER_DELAY);
		
		LOGGER.Info(GetType().Name +": Enabled.");
	}
	
	public static CustomMailManager getInstance()
	{
		return SingletonHolder.INSTANCE;
	}
	
	private static class SingletonHolder
	{
		public static readonly CustomMailManager INSTANCE = new CustomMailManager();
	}
}
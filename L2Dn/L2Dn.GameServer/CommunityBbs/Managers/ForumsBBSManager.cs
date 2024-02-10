using L2Dn.GameServer.CommunityBbs.BB;
using L2Dn.GameServer.Model.Actor;
using L2Dn.GameServer.Utilities;
using NLog;

namespace L2Dn.GameServer.CommunityBbs.Managers;

public class ForumsBBSManager: BaseBBSManager
{
	private static readonly Logger LOGGER = LogManager.GetLogger(nameof(ForumsBBSManager));
	private readonly Set<Forum> _table;
	private int _lastid = 1;
	
	/**
	 * Instantiates a new forums bbs manager.
	 */
	protected ForumsBBSManager()
	{
		_table = new();
		try 
		{
			Connection con = DatabaseFactory.getConnection();
			Statement s = con.createStatement();
			ResultSet rs = s.executeQuery("SELECT forum_id FROM forums WHERE forum_type = 0");
			while (rs.next())
			{
				addForum(new Forum(rs.getInt("forum_id"), null));
			}
		}
		catch (Exception e)
		{
			LOGGER.Warn(GetType().Name + ": Data error on Forum (root): " + e);
		}
	}
	
	/**
	 * Inits the root.
	 */
	public void initRoot()
	{
		_table.forEach(x => x.vload());
		LOGGER.Info(GetType().Name + ": Loaded " + _table.Count + " forums. Last forum id used: " + _lastid);
	}
	
	/**
	 * Adds the forum.
	 * @param ff the forum
	 */
	public void addForum(Forum ff)
	{
		if (ff == null)
		{
			return;
		}
		
		_table.add(ff);
		
		if (ff.getID() > _lastid)
		{
			_lastid = ff.getID();
		}
	}
	
	public override void parsecmd(String command, Player player)
	{
	}
	
	/**
	 * Gets the forum by name.
	 * @param name the forum name
	 * @return the forum by name
	 */
	public Forum getForumByName(String name)
	{
		foreach (Forum f in _table)
		{
			if (f.getName().equals(name))
			{
				return f;
			}
		}
		return null;
	}
	
	/**
	 * Creates the new forum.
	 * @param name the forum name
	 * @param parent the parent forum
	 * @param type the forum type
	 * @param perm the perm
	 * @param oid the oid
	 * @return the new forum
	 */
	public Forum createNewForum(String name, Forum parent, int type, int perm, int oid)
	{
		Forum forum = new Forum(name, parent, type, perm, oid);
		forum.insertIntoDb();
		return forum;
	}
	
	/**
	 * Gets the a new Id.
	 * @return the a new Id
	 */
	public int getANewID()
	{
		return ++_lastid;
	}
	
	/**
	 * Gets the forum by Id.
	 * @param idf the the forum Id
	 * @return the forum by Id
	 */
	public Forum getForumByID(int idf)
	{
		foreach (Forum f in _table)
		{
			if (f.getID() == idf)
			{
				return f;
			}
		}
		return null;
	}
	
	public override void parsewrite(String ar1, String ar2, String ar3, String ar4, String ar5, Player player)
	{
	}
	
	/**
	 * Gets the single instance of ForumsBBSManager.
	 * @return single instance of ForumsBBSManager
	 */
	public static ForumsBBSManager getInstance()
	{
		return SingletonHolder.INSTANCE;
	}
	
	private static class SingletonHolder
	{
		public static readonly ForumsBBSManager INSTANCE = new ForumsBBSManager();
	}
}
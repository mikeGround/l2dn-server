﻿using L2Dn.GameServer.Enums;
using L2Dn.GameServer.Model.Actor;
using L2Dn.GameServer.Model.Variables;
using L2Dn.GameServer.Utilities;
using Org.BouncyCastle.Utilities;

namespace L2Dn.GameServer.Model;

public class ClientSettings
{
	private readonly Player _player;
	private bool _announceEnabled;
	private bool _partyRequestRestrictedFromOthers;
	private bool _partyRequestRestrictedFromClan;
	private bool _partyRequestRestrictedFromFriends;
	private bool _friendRequestRestrictedFromOthers;
	private bool _friendRequestRestrictedFromClan;
	private PartyDistributionType _partyContributionType;
	
	public ClientSettings(Player player)
	{
		_player = player;
		
		string variable = _player.getVariables().getString(PlayerVariables.CLIENT_SETTINGS, "");

		StatSet settings = new StatSet();
		if (!string.IsNullOrEmpty(variable))
		{
			var values = variable.Split(",").Select(entry => entry.Split("=")).Select(entry =>
				(entry[0].Replace("{", "").Replace(" ", ""), entry[1].Replace("}", "").Replace(" ", "")));

			foreach (var val in values)
			{
				settings.set(val.Item1, val.Item2);
			}
		}
		
		_announceEnabled = settings.getBoolean("ANNOUNCE_ENABLED", true);
		_partyRequestRestrictedFromOthers = settings.getBoolean("PARTY_REQUEST_RESTRICTED_FROM_OTHERS", false);
		_partyRequestRestrictedFromClan = settings.getBoolean("PARTY_REQUEST_RESTRICTED_FROM_CLAN", false);
		_partyRequestRestrictedFromFriends = settings.getBoolean("PARTY_REQUEST_RESTRICTED_FROM_FRIENDS", false);
		_friendRequestRestrictedFromOthers = settings.getBoolean("FRIENDS_REQUEST_RESTRICTED_FROM_OTHERS", false);
		_friendRequestRestrictedFromClan = settings.getBoolean("FRIENDS_REQUEST_RESTRICTED_FROM_CLAN", false);
		_partyContributionType = (PartyDistributionType)settings.getInt("PARTY_CONTRIBUTION_TYPE", 0);
	}
	
	public void storeSettings()
	{
		StatSet settings = new StatSet();
		settings.set("ANNOUNCE_ENABLED", _announceEnabled);
		settings.set("PARTY_REQUEST_RESTRICTED_FROM_OTHERS", _partyRequestRestrictedFromOthers);
		settings.set("PARTY_REQUEST_RESTRICTED_FROM_CLAN", _partyRequestRestrictedFromClan);
		settings.set("PARTY_REQUEST_RESTRICTED_FROM_FRIENDS", _partyRequestRestrictedFromFriends);
		settings.set("FRIENDS_REQUEST_RESTRICTED_FROM_OTHERS", _friendRequestRestrictedFromOthers);
		settings.set("FRIENDS_REQUEST_RESTRICTED_FROM_CLAN", _friendRequestRestrictedFromClan);
		settings.set("PARTY_CONTRIBUTION_TYPE", _partyContributionType);
		_player.getVariables().set(PlayerVariables.CLIENT_SETTINGS, settings.getSet());
	}
	
	public bool isAnnounceEnabled()
	{
		return _announceEnabled;
	}
	
	public void setAnnounceEnabled(bool enabled)
	{
		_announceEnabled = enabled;
		storeSettings();
	}
	
	public bool isPartyRequestRestrictedFromOthers()
	{
		return _partyRequestRestrictedFromOthers;
	}
	
	public void setPartyRequestRestrictedFromOthers(bool partyRequestRestrictedFromOthers)
	{
		_partyRequestRestrictedFromOthers = partyRequestRestrictedFromOthers;
	}
	
	public bool isPartyRequestRestrictedFromClan()
	{
		return _partyRequestRestrictedFromClan;
	}
	
	public void setPartyRequestRestrictedFromClan(bool partyRequestRestrictedFromClan)
	{
		_partyRequestRestrictedFromClan = partyRequestRestrictedFromClan;
	}
	
	public bool isPartyRequestRestrictedFromFriends()
	{
		return _partyRequestRestrictedFromFriends;
	}
	
	public void setPartyRequestRestrictedFromFriends(bool partyRequestRestrictedFromFriends)
	{
		_partyRequestRestrictedFromFriends = partyRequestRestrictedFromFriends;
	}
	
	public bool isFriendRequestRestrictedFromOthers()
	{
		return _friendRequestRestrictedFromOthers;
	}
	
	public void setFriendRequestRestrictedFromOthers(bool friendRequestRestrictedFromOthers)
	{
		_friendRequestRestrictedFromOthers = friendRequestRestrictedFromOthers;
	}
	
	public bool isFriendRequestRestrictedFromClan()
	{
		return _friendRequestRestrictedFromClan;
	}
	
	public void setFriendRequestRestrictionFromClan(bool friendRequestRestrictedFromClan)
	{
		_friendRequestRestrictedFromClan = friendRequestRestrictedFromClan;
	}
	
	public PartyDistributionType getPartyContributionType()
	{
		return _partyContributionType;
	}
	
	public void setPartyContributionType(PartyDistributionType partyContributionType)
	{
		_partyContributionType = partyContributionType;
		storeSettings();
	}
}

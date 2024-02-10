﻿namespace L2Dn.GameServer.Model.Fishings;

public class FishingRod
{
    private readonly int _itemId;
    private readonly int _reduceFishingTime;
    private readonly float _xpMultiplier;
    private readonly float _spMultiplier;
	
    public FishingRod(int itemId, int reduceFishingTime, float xpMultiplier, float spMultiplier)
    {
        _itemId = itemId;
        _reduceFishingTime = reduceFishingTime;
        _xpMultiplier = xpMultiplier;
        _spMultiplier = spMultiplier;
    }
	
    public int getItemId()
    {
        return _itemId;
    }
	
    public int getReduceFishingTime()
    {
        return _reduceFishingTime;
    }
	
    public float getXpMultiplier()
    {
        return _xpMultiplier;
    }
	
    public float getSpMultiplier()
    {
        return _spMultiplier;
    }
}
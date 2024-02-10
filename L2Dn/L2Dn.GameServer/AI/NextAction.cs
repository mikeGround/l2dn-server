﻿using L2Dn.GameServer.Utilities;

namespace L2Dn.GameServer.AI;

/**
 * Class for AI action after some event.<br>
 * Has 2 array list for "work" and "break".
 * @author Yaroslav
 */
public class NextAction
{
	public interface NextActionCallback
	{
		void doWork();
	}
	
	private List<CtrlEvent> _events;
	private List<CtrlIntention> _intentions;
	private NextActionCallback _callback;
	
	/**
	 * Main constructor.
	 * @param events
	 * @param intentions
	 * @param callback
	 */
	public NextAction(List<CtrlEvent> events, List<CtrlIntention> intentions, NextActionCallback callback)
	{
		_events = events;
		_intentions = intentions;
		setCallback(callback);
	}
	
	/**
	 * Single constructor.
	 * @param event
	 * @param intention
	 * @param callback
	 */
	public NextAction(CtrlEvent @event, CtrlIntention intention, NextActionCallback callback)
	{
		if (_events == null)
		{
			_events = new();
		}
		
		if (_intentions == null)
		{
			_intentions = new();
		}
		
		if (@event != null)
		{
			_events.add(@event);
		}
		
		if (intention != null)
		{
			_intentions.add(intention);
		}
		setCallback(callback);
	}
	
	/**
	 * Do action.
	 */
	public void doAction()
	{
		if (_callback != null)
		{
			_callback.doWork();
		}
	}
	
	/**
	 * @return the _event
	 */
	public List<CtrlEvent> getEvents()
	{
		// If null return empty list.
		if (_events == null)
		{
			_events = new();
		}
		return _events;
	}
	
	/**
	 * @param event the event to set.
	 */
	public void setEvents(List<CtrlEvent> @event)
	{
		_events = @event;
	}
	
	/**
	 * @param event
	 */
	public void addEvent(CtrlEvent @event)
	{
		if (_events == null)
		{
			_events = new();
		}
		
		if (@event != null)
		{
			_events.add(@event);
		}
	}
	
	/**
	 * @param event
	 */
	public void removeEvent(CtrlEvent @event)
	{
		if (_events == null)
		{
			return;
		}
		_events.Remove(@event);
	}
	
	/**
	 * @return the _callback
	 */
	public NextActionCallback getCallback()
	{
		return _callback;
	}
	
	/**
	 * @param callback the callback to set.
	 */
	public void setCallback(NextActionCallback callback)
	{
		_callback = callback;
	}
	
	/**
	 * @return the _intentions
	 */
	public List<CtrlIntention> getIntentions()
	{
		// If null return empty list.
		if (_intentions == null)
		{
			_intentions = new();
		}
		return _intentions;
	}
	
	/**
	 * @param intentions the intention to set.
	 */
	public void setIntentions(List<CtrlIntention> intentions)
	{
		_intentions = intentions;
	}
	
	/**
	 * @param intention
	 */
	public void addIntention(CtrlIntention intention)
	{
		if (_intentions == null)
		{
			_intentions = new();
		}
		
		if (intention != null)
		{
			_intentions.add(intention);
		}
	}
	
	/**
	 * @param intention
	 */
	public void removeIntention(CtrlIntention intention)
	{
		if (_intentions == null)
		{
			return;
		}
		_intentions.Remove(intention);
	}
}
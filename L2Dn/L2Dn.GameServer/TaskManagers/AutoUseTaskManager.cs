using System.Runtime.CompilerServices;
using L2Dn.GameServer.Data.Xml;
using L2Dn.GameServer.Handlers;
using L2Dn.GameServer.Model;
using L2Dn.GameServer.Model.Actor;
using L2Dn.GameServer.Model.Actor.Instances;
using L2Dn.GameServer.Model.Actor.Transforms;
using L2Dn.GameServer.Model.Effects;
using L2Dn.GameServer.Model.Events;
using L2Dn.GameServer.Model.Events.Impl.Items;
using L2Dn.GameServer.Model.Holders;
using L2Dn.GameServer.Model.Items;
using L2Dn.GameServer.Model.Items.Instances;
using L2Dn.GameServer.Model.Skills;
using L2Dn.GameServer.Model.Skills.Targets;
using L2Dn.GameServer.Model.Zones;
using L2Dn.GameServer.Network.Enums;
using L2Dn.GameServer.Utilities;
using ThreadPool = System.Threading.ThreadPool;

namespace L2Dn.GameServer.TaskManagers;

/**
 * @author Mobius
 */
public class AutoUseTaskManager
{
	private static readonly Set<Set<Player>> POOLS = new();
	private const int POOL_SIZE = 300;
	private const int TASK_DELAY = 300;
	private const int REUSE_MARGIN_TIME = 3;
	
	protected AutoUseTaskManager()
	{
	}
	
	private class AutoUse: Runnable
	{
		private readonly Set<Player> _players;
		
		public AutoUse(Set<Player> players)
		{
			_players = players;
		}
		
		public void run()
		{
			if (_players.isEmpty())
			{
				return;
			}
			
			foreach (Player player in _players)
			{
				if (player.getAutoUseSettings().isEmpty() || !player.isOnline() || (player.isInOfflineMode() && !player.isOfflinePlay()))
				{
					stopAutoUseTask(player);
					continue;
				}
				
				if (player.isSitting() || player.hasBlockActions() || player.isControlBlocked() || player.isAlikeDead() || player.isMounted() || (player.isTransformed() && player.getTransformation().get().isRiding()))
				{
					continue;
				}
				
				bool isInPeaceZone = player.isInsideZone(ZoneId.PEACE) || player.isInsideZone(ZoneId.SAYUNE);
				
				if (Config.ENABLE_AUTO_ITEM && !isInPeaceZone)
				{
					Pet pet = player.getPet();
					foreach (int itemId in player.getAutoUseSettings().getAutoSupplyItems())
					{
						if (player.isTeleporting())
						{
							break;
						}
						
						Item item = player.getInventory().getItemByItemId(itemId);
						if (item == null)
						{
							player.getAutoUseSettings().getAutoSupplyItems().remove(itemId);
							continue;
						}
						
						// Pet food is managed by Pet FeedTask.
						if ((pet != null) && pet.getPetData().getFood().Contains(itemId))
						{
							continue;
						}
						
						ItemTemplate it = item.getTemplate();
						if (it != null)
						{
							if (!it.checkCondition(player, player, false))
							{
								continue;
							}
							
							List<ItemSkillHolder> skills = it.getAllSkills();
							if (skills != null)
							{
								bool continueItems = false;
								foreach (ItemSkillHolder itemSkillHolder in skills)
								{
									Skill skill = itemSkillHolder.getSkill();
									if (player.isAffectedBySkill(skill.getId()) || player.hasSkillReuse(skill.getReuseHashCode()) || !skill.checkCondition(player, player, false))
									{
										continueItems = true;
										break;
									}
									
									// Check item skills that affect pets.
									if ((pet != null) && !pet.isDead() && (pet.isAffectedBySkill(skill.getId()) || pet.hasSkillReuse(skill.getReuseHashCode()) || !skill.checkCondition(pet, pet, false)))
									{
										continueItems = true;
										break;
									}
								}

								if (continueItems)
									continue;
							}
						}
						
						int reuseDelay = item.getReuseDelay();
						if ((reuseDelay <= 0) || (player.getItemRemainingReuseTime(item.getObjectId()) <= 0))
						{
							EtcItem etcItem = item.getEtcItem();
							IItemHandler handler = ItemHandler.getInstance().getHandler(etcItem);
							if ((handler != null) && handler.useItem(player, item, false))
							{
								if (reuseDelay > 0)
								{
									player.addTimeStampItem(item, reuseDelay);
								}
								
								// Notify events.
								if (EventDispatcher.getInstance().hasListener(EventType.ON_ITEM_USE, item.getTemplate()))
								{
									EventDispatcher.getInstance().notifyEventAsync(new OnItemUse(player, item), item.getTemplate());
								}
							}
						}
					}
				}
				
				if (Config.ENABLE_AUTO_POTION && !isInPeaceZone && (player.getCurrentHpPercent() < player.getAutoPlaySettings().getAutoPotionPercent()))
				{
					int itemId = player.getAutoUseSettings().getAutoPotionItem();
					if (itemId > 0)
					{
						Item item = player.getInventory().getItemByItemId(itemId);
						if (item == null)
						{
							player.getAutoUseSettings().setAutoPotionItem(0);
						}
						else
						{
							int reuseDelay = item.getReuseDelay();
							if ((reuseDelay <= 0) || (player.getItemRemainingReuseTime(item.getObjectId()) <= 0))
							{
								EtcItem etcItem = item.getEtcItem();
								IItemHandler handler = ItemHandler.getInstance().getHandler(etcItem);
								if ((handler != null) && handler.useItem(player, item, false))
								{
									if (reuseDelay > 0)
									{
										player.addTimeStampItem(item, reuseDelay);
									}
									
									// Notify events.
									if (EventDispatcher.getInstance().hasListener(EventType.ON_ITEM_USE, item.getTemplate()))
									{
										EventDispatcher.getInstance().notifyEventAsync(new OnItemUse(player, item), item.getTemplate());
									}
								}
							}
						}
					}
				}
				
				if (Config.ENABLE_AUTO_PET_POTION && !isInPeaceZone)
				{
					Pet pet = player.getPet();
					if ((pet != null) && !pet.isDead())
					{
						int percent = pet.getCurrentHpPercent();
						if ((percent < 100) && (percent <= player.getAutoPlaySettings().getAutoPetPotionPercent()))
						{
							int itemId = player.getAutoUseSettings().getAutoPetPotionItem();
							if (itemId > 0)
							{
								Item item = player.getInventory().getItemByItemId(itemId);
								if (item == null)
								{
									player.getAutoUseSettings().setAutoPetPotionItem(0);
								}
								else
								{
									int reuseDelay = item.getReuseDelay();
									if ((reuseDelay <= 0) || (player.getItemRemainingReuseTime(item.getObjectId()) <= 0))
									{
										EtcItem etcItem = item.getEtcItem();
										IItemHandler handler = ItemHandler.getInstance().getHandler(etcItem);
										if ((handler != null) && handler.useItem(player, item, false) && (reuseDelay > 0))
										{
											player.addTimeStampItem(item, reuseDelay);
										}
									}
								}
							}
						}
					}
				}
				
				if (Config.ENABLE_AUTO_SKILL)
				{
					foreach (int skillId in player.getAutoUseSettings().getAutoBuffs())
					{
						// Fixes start area issue.
						if (isInPeaceZone)
						{
							break;
						}
						
						// Already casting.
						if (player.isCastingNow())
						{
							break;
						}
						
						// Player is teleporting.
						if (player.isTeleporting())
						{
							break;
						}
						
						Playable pet = null;
						Skill skill = player.getKnownSkill(skillId);
						if (skill == null)
						{
							if (player.hasServitors())
							{
								foreach (Summon summon in player.getServitors().values())
								{
									skill = summon.getKnownSkill(skillId);
									if (skill != null)
									{
										pet = summon;
										break;
									}
								}
							}
							if ((skill == null) && player.hasPet())
							{
								pet = player.getPet();
								skill = pet.getKnownSkill(skillId);
							}
							if (skill == null)
							{
								player.getAutoUseSettings().getAutoBuffs().remove(skillId);
								continue;
							}
						}
						
						WorldObject target = player.getTarget();
						if (canCastBuff(player, target, skill))
						{
							foreach (AttachSkillHolder holder in skill.getAttachSkills())
							{
								if (player.isAffectedBySkill(holder.getRequiredSkillId()))
								{
									skill = holder.getSkill();
									break;
								}
							}
							
							// Playable target cast.
							Playable caster = pet != null ? pet : player;
							if ((target != null) && target.isPlayable() && (target.getActingPlayer().getPvpFlag() == 0) && (target.getActingPlayer().getReputation() >= 0))
							{
								caster.doCast(skill);
							}
							else // Target self, cast and re-target.
							{
								WorldObject savedTarget = target;
								caster.setTarget(caster);
								caster.doCast(skill);
								caster.setTarget(savedTarget);
							}
						}
					}
					
					// Continue when auto play is not enabled.
					if (!player.isAutoPlaying())
					{
						continue;
					}
					
					{
						// Already casting.
						if (player.isCastingNow())
						{
							break;
						}
						
						// Player is teleporting.
						if (player.isTeleporting())
						{
							break;
						}
						
						// Acquire next skill.
						Playable pet = null;
						int skillId = player.getAutoUseSettings().getNextSkillId();
						Skill skill = player.getKnownSkill(skillId);
						if (skill == null)
						{
							if (player.hasServitors())
							{
								foreach (Summon summon in player.getServitors().values())
								{
									skill = summon.getKnownSkill(skillId);
									if (skill != null)
									{
										pet = summon;
										break;
									}
								}
							}
							if ((skill == null) && player.hasPet())
							{
								pet = player.getPet();
								skill = pet.getKnownSkill(skillId);
								if (pet.isSkillDisabled(skill))
								{
									player.getAutoUseSettings().incrementSkillOrder();
									break;
								}
							}
							if (skill == null)
							{
								player.getAutoUseSettings().getAutoSkills().remove(skillId);
								player.getAutoUseSettings().resetSkillOrder();
								break;
							}
						}
						
						// Casting on self stops movement.
						WorldObject target = player.getTarget();
						if (target == player)
						{
							break;
						}
						
						// Check bad skill target.
						if ((target == null) || ((Creature) target).isDead())
						{
							break;
						}
						
						// Peace zone and auto attackable checks.
						if (target.isInsideZone(ZoneId.PEACE) || !target.isAutoAttackable(player))
						{
							break;
						}
						
						// Do not attack guards.
						if (target is Guard)
						{
							int targetMode = player.getAutoPlaySettings().getNextTargetMode();
							if ((targetMode != 3 /* NPC */) && (targetMode != 0 /* Any Target */))
							{
								break;
							}
						}
						
						if (!canUseMagic(player, target, skill) || (pet != null ? pet : player).useMagic(skill, null, true, false))
						{
							player.getAutoUseSettings().incrementSkillOrder();
						}
					}
					
					foreach (int actionId in player.getAutoUseSettings().getAutoActions())
					{
						BuffInfo info = player.getEffectList().getFirstBuffInfoByAbnormalType(AbnormalType.BOT_PENALTY);
						if (info != null)
						{
							foreach (AbstractEffect effect in info.getEffects())
							{
								if (!effect.checkCondition(actionId))
								{
									player.sendPacket(SystemMessageId.YOU_HAVE_BEEN_REPORTED_AS_AN_ILLEGAL_PROGRAM_USER_SO_YOUR_ACTIONS_HAVE_BEEN_RESTRICTED);
									break;
								}
							}
						}
						
						// Do not allow to do some action if player is transformed.
						if (player.isTransformed())
						{
							TransformTemplate transformTemplate = player.getTransformation().get().getTemplate(player);
							int[] allowedActions = transformTemplate.getBasicActionList();
							if ((allowedActions == null) || (Array.BinarySearch(allowedActions, actionId) < 0))
							{
								continue;
							}
						}
						
						ActionDataHolder actionHolder = ActionData.getInstance().getActionData(actionId);
						if (actionHolder != null)
						{
							IPlayerActionHandler actionHandler = PlayerActionHandler.getInstance().getHandler(actionHolder.getHandler());
							if (actionHandler != null)
							{
								if (!actionHandler.isPetAction())
								{
									actionHandler.useAction(player, actionHolder, false, false);
								}
								else
								{
									Summon summon = player.getAnyServitor();
									if ((summon != null) && !summon.isAlikeDead())
									{
										Skill skill = summon.getKnownSkill(actionHolder.getOptionId());
										if ((skill != null) && !canSummonCastSkill(player, summon, skill))
										{
											continue;
										}
										
										actionHandler.useAction(player, actionHolder, false, false);
									}
								}
							}
						}
					}
				}
			}
		}
		
		private bool canCastBuff(Player player, WorldObject target, Skill skill)
		{
			// Summon check.
			if ((skill.getAffectScope() == AffectScope.SUMMON_EXCEPT_MASTER) || (skill.getTargetType() == TargetType.SUMMON))
			{
				if (!player.hasServitors())
				{
					return false;
				}
				int occurrences = 0;
				foreach (Summon servitor in player.getServitors().values())
				{
					if (servitor.isAffectedBySkill(skill.getId()))
					{
						occurrences++;
					}
				}
				if (occurrences == player.getServitors().size())
				{
					return false;
				}
			}
			
			if ((target != null) && target.isCreature() && ((Creature) target).isAlikeDead() && (skill.getTargetType() != TargetType.SELF) && (skill.getTargetType() != TargetType.NPC_BODY) && (skill.getTargetType() != TargetType.PC_BODY))
			{
				return false;
			}
			
			Playable playableTarget = (target == null) || !target.isPlayable() || (skill.getTargetType() == TargetType.SELF) ? player : (Playable) target;
			if ((player != playableTarget) && (player.calculateDistance3D(playableTarget) > skill.getCastRange()))
			{
				return false;
			}
			
			if (!canUseMagic(player, playableTarget, skill))
			{
				return false;
			}
			
			BuffInfo buffInfo = playableTarget.getEffectList().getBuffInfoBySkillId(skill.getId());
			BuffInfo abnormalBuffInfo = playableTarget.getEffectList().getFirstBuffInfoByAbnormalType(skill.getAbnormalType());
			if (abnormalBuffInfo != null)
			{
				if (buffInfo != null)
				{
					return (abnormalBuffInfo.getSkill().getId() == buffInfo.getSkill().getId()) && ((buffInfo.getTime() <= REUSE_MARGIN_TIME) || (buffInfo.getSkill().getLevel() < skill.getLevel()));
				}
				return (abnormalBuffInfo.getSkill().getAbnormalLevel() < skill.getAbnormalLevel()) || abnormalBuffInfo.isAbnormalType(AbnormalType.NONE);
			}
			return buffInfo == null;
		}
		
		private bool canUseMagic(Player player, WorldObject target, Skill skill)
		{
			if ((skill.getItemConsumeCount() > 0) && (player.getInventory().getInventoryItemCount(skill.getItemConsumeId(), -1) < skill.getItemConsumeCount()))
			{
				return false;
			}
			
			if ((skill.getMpConsume() > 0) && (player.getCurrentMp() < skill.getMpConsume()))
			{
				return false;
			}
			
			foreach (AttachSkillHolder holder in skill.getAttachSkills())
			{
				if (player.isAffectedBySkill(holder.getRequiredSkillId()) //
					&& (player.hasSkillReuse(holder.getSkill().getReuseHashCode()) || player.isAffectedBySkill(holder)))
				{
					return false;
				}
			}
			
			return !player.isSkillDisabled(skill) && skill.checkCondition(player, target, false);
		}
		
		private bool canSummonCastSkill(Player player, Summon summon, Skill skill)
		{
			if (skill.isBad() && (player.getTarget() == null))
			{
				return false;
			}
			
			int mpConsume = skill.getMpConsume() + skill.getMpInitialConsume();
			if ((((mpConsume != 0) && (mpConsume > (int) Math.Floor(summon.getCurrentMp()))) || ((skill.getHpConsume() != 0) && (skill.getHpConsume() > (int) Math.floor(summon.getCurrentHp())))))
			{
				return false;
			}
			
			if (summon.isSkillDisabled(skill))
			{
				return false;
			}
			
			if (((player.getTarget() != null) && !skill.checkCondition(summon, player.getTarget(), false)) || ((player.getTarget() == null) && !skill.checkCondition(summon, player, false)))
			{
				return false;
			}
			
			if ((skill.getItemConsumeCount() > 0) && (summon.getInventory().getInventoryItemCount(skill.getItemConsumeId(), -1) < skill.getItemConsumeCount()))
			{
				return false;
			}
			
			if (skill.getTargetType()==TargetType.SELF || skill.getTargetType()==TargetType.SUMMON)
			{
				BuffInfo summonInfo = summon.getEffectList().getBuffInfoBySkillId(skill.getId());
				return (summonInfo != null) && (summonInfo.getTime() >= REUSE_MARGIN_TIME);
			}
			
			if ((skill.getEffects(EffectScope.GENERAL) != null) && skill.getEffects(EffectScope.GENERAL).stream().anyMatch(a -> a.getEffectType().equals(EffectType.MANAHEAL_BY_LEVEL)) && (player.getCurrentMpPercent() > 80))
			{
				return false;
			}
			
			BuffInfo buffInfo = player.getEffectList().getBuffInfoBySkillId(skill.getId());
			BuffInfo abnormalBuffInfo = player.getEffectList().getFirstBuffInfoByAbnormalType(skill.getAbnormalType());
			if (abnormalBuffInfo != null)
			{
				if (buffInfo != null)
				{
					return (abnormalBuffInfo.getSkill().getId() == buffInfo.getSkill().getId()) && ((buffInfo.getTime() <= REUSE_MARGIN_TIME) || (buffInfo.getSkill().getLevel() < skill.getLevel()));
				}
				return (abnormalBuffInfo.getSkill().getAbnormalLevel() < skill.getAbnormalLevel()) || abnormalBuffInfo.isAbnormalType(AbnormalType.NONE);
			}
			
			return true;
		}
	}
	
	[MethodImpl(MethodImplOptions.Synchronized)]
	public void startAutoUseTask(Player player)
	{
		foreach (Set<Player> pool in POOLS)
		{
			if (pool.Contains(player))
			{
				return;
			}
		}
		
		foreach (Set<Player> pool in POOLS)
		{
			if (pool.Count < POOL_SIZE)
			{
				pool.add(player);
				return;
			}
		}
		
		Set<Player> pool1 = new();
		pool1.add(player);
		ThreadPool.scheduleAtFixedRate(new AutoUse(pool1), TASK_DELAY, TASK_DELAY);
		POOLS.add(pool1);
	}
	
	public void stopAutoUseTask(Player player)
	{
		player.getAutoUseSettings().resetSkillOrder();
		if (player.getAutoUseSettings().isEmpty() || !player.isOnline() || (player.isInOfflineMode() && !player.isOfflinePlay()))
		{
			foreach (Set<Player> pool in POOLS)
			{
				if (pool.remove(player))
				{
					return;
				}
			}
		}
	}
	
	public void addAutoSupplyItem(Player player, int itemId)
	{
		player.getAutoUseSettings().getAutoSupplyItems().add(itemId);
		startAutoUseTask(player);
	}
	
	public void removeAutoSupplyItem(Player player, int itemId)
	{
		player.getAutoUseSettings().getAutoSupplyItems().remove(itemId);
		stopAutoUseTask(player);
	}
	
	public void setAutoPotionItem(Player player, int itemId)
	{
		player.getAutoUseSettings().setAutoPotionItem(itemId);
		startAutoUseTask(player);
	}
	
	public void removeAutoPotionItem(Player player)
	{
		player.getAutoUseSettings().setAutoPotionItem(0);
		stopAutoUseTask(player);
	}
	
	public void setAutoPetPotionItem(Player player, int itemId)
	{
		player.getAutoUseSettings().setAutoPetPotionItem(itemId);
		startAutoUseTask(player);
	}
	
	public void removeAutoPetPotionItem(Player player)
	{
		player.getAutoUseSettings().setAutoPetPotionItem(0);
		stopAutoUseTask(player);
	}
	
	public void addAutoBuff(Player player, int skillId)
	{
		player.getAutoUseSettings().getAutoBuffs().add(skillId);
		startAutoUseTask(player);
	}
	
	public void removeAutoBuff(Player player, int skillId)
	{
		player.getAutoUseSettings().getAutoBuffs().remove(skillId);
		stopAutoUseTask(player);
	}
	
	public void addAutoSkill(Player player, int skillId)
	{
		player.getAutoUseSettings().getAutoSkills().add(skillId);
		startAutoUseTask(player);
	}
	
	public void removeAutoSkill(Player player, int skillId)
	{
		player.getAutoUseSettings().getAutoSkills().remove(skillId);
		stopAutoUseTask(player);
	}
	
	public void addAutoAction(Player player, int actionId)
	{
		player.getAutoUseSettings().getAutoActions().add(actionId);
		startAutoUseTask(player);
	}
	
	public void removeAutoAction(Player player, int actionId)
	{
		player.getAutoUseSettings().getAutoActions().remove(actionId);
		stopAutoUseTask(player);
	}
	
	public static AutoUseTaskManager getInstance()
	{
		return SingletonHolder.INSTANCE;
	}
	
	private static class SingletonHolder
	{
		public static AutoUseTaskManager INSTANCE = new AutoUseTaskManager();
	}
}
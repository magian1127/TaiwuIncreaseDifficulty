using System;
using System.Collections.Generic;
using System.Diagnostics;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.Item.Display;
using GameData.Domains.TaiwuEvent;
using GameData.Domains.TaiwuEvent.DisplayEvent;
using GameData.Domains.TaiwuEvent.EventHelper;
using GameData.Utilities;
using HarmonyLib;

namespace IncreaseDifficultyBackend
{
    [HarmonyPatch]
    public class EventHelperPatch
    {
        /// <summary>
        /// 更改亲密度,送礼减少人物级别*600的亲密度
        /// </summary>
        /// <param name="characterA"></param>
        /// <param name="characterB"></param>
        /// <param name="changeValue"></param>
        /// <param name="personalityType"></param>
        /// <param name="requiredPersonality"></param>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(EventHelper), nameof(EventHelper.ChangeFavorability))]
        public static void ChangeFavorabilityPrefix(Character characterA, Character characterB, ref short changeValue, sbyte personalityType, sbyte requiredPersonality)
        {
            bool isTaiwu = characterB.GetId() == DomainManager.Taiwu.GetTaiwuCharId();
            if (!isTaiwu || changeValue <= 600)
            {
                return;
            }

            TaiwuEvent taiwuEvent = Traverse.Create(DomainManager.TaiwuEvent).Field("_showingEvent").GetValue<TaiwuEvent>();
            if (taiwuEvent.EventGuid == IncreaseDifficulty.EventGuid.Gift)
            {
                changeValue -= (short)(characterA.GetInteractionGrade() * 600);
                if (changeValue < 600)
                {
                    changeValue = 600;
                }
                //AdaptableLog.Info($"降低太吾送礼{taiwuEvent.EventGuid} - {changeValue} - {characterA.GetInteractionGrade()}");
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EventHelper), nameof(EventHelper.SelectCharacterItemRequest))]
        public static void SelectCharacterItemRequestPrefix(int charId, EventArgBox argBox, SelectItemFilter filter, Action onSelectFinish, ref bool includeEquipment, bool skipAddItem)
        {
            if (DomainManager.TaiwuEvent.IsTriggeredEvent(IncreaseDifficulty.EventGuid.Steal))
            {
                //AdaptableLog.Info($"触发了偷窃");
                includeEquipment = false;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EventHelper), nameof(EventHelper.SelectCharacterItemRequest))]
        public static void SelectCharacterItemRequestPostfix(int charId, ref EventArgBox argBox, SelectItemFilter filter, Action onSelectFinish, bool includeEquipment, bool skipAddItem)
        {
            if (DomainManager.TaiwuEvent.IsTriggeredEvent(IncreaseDifficulty.EventGuid.Steal) || DomainManager.TaiwuEvent.IsTriggeredEvent(IncreaseDifficulty.EventGuid.Cheat) || DomainManager.TaiwuEvent.IsTriggeredEvent(IncreaseDifficulty.EventGuid.Rob))
            {
                EventSelectItemData data;
                if (argBox.Get("SelectItemInfo", out data))
                {
                    Character taiwu = DomainManager.Character.GetElement_Objects(DomainManager.Taiwu.GetTaiwuCharId());
                    Character character = DomainManager.Character.GetElement_Objects(charId);
                    var orgTemplateId = character.GetOrganizationInfo().OrgTemplateId;

                    data.CanSelectItemList.RemoveAll(delegate (ItemDisplayData item)
                    {//删除所有不传之秘
                        if (item.Key.ItemType == 10)
                        {
                            var skillBook = Config.SkillBook.Instance[item.Key.TemplateId];
                            if (skillBook.CombatSkillTemplateId >= 0)
                            {
                                var combatSkill = Config.CombatSkill.Instance[skillBook.CombatSkillTemplateId];
                                if (combatSkill.SectId == orgTemplateId && combatSkill.IsNonPublic)
                                {
                                    return true;
                                }
                            }
                        }
                        return false;
                    });

                    var clever = EventHelper.GetRolePersonality(taiwu, PersonalityType.Clever);
                    //太吾每5聪颖,可以多看到一个物品
                    int seeItem = IncreaseDifficulty.CheatStealRobNum + clever / 5;
                    if (data.CanSelectItemList.Count <= seeItem)
                    {
                        return;
                    }


                    int seed = DomainManager.World.GetCurrDate();
                    string date = seed.ToString("0000");
                    date = date.Substring(date.Length - 4);
                    date = charId + date;
                    int.TryParse(date, out seed);
                    Random random = new Random(seed);
                    List<ItemDisplayData> newSelectItemList = new List<ItemDisplayData>();

                    for (int i = 0; i < seeItem; i++)
                    {
                        var item = data.CanSelectItemList[data.CanSelectItemList.Count == 1 ? 0 : random.Next(0, data.CanSelectItemList.Count - 1)];
                        newSelectItemList.Add(item);
                        data.CanSelectItemList.Remove(item);
                    }
                    data.CanSelectItemList = newSelectItemList;
                }
            }
        }
    }
}

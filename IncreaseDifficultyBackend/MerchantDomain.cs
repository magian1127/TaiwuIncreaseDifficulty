using GameData.Common;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.Item;
using GameData.Domains.Item.Display;
using GameData.Domains.Merchant;
using GameData.Utilities;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncreaseDifficultyBackend
{
    [HarmonyPatch]
    public class MerchantDomainPatch
    {
        /// <summary>
        /// 显示交换的书籍,修改为不能换的书不显示
        /// </summary>
        /// <param name="context"></param>
        /// <param name="npcId"></param>
        /// <param name="isFavor"></param>
        /// <returns></returns>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MerchantDomain), nameof(MerchantDomain.GetTradeBookDisplayData))]
        public static bool GetTradeBookDisplayDataPrefix(MerchantDomain __instance, ref List<ItemDisplayData> __result, DataContext context, int npcId, bool isFavor)
        {
            MerchantData merchantData = __instance.GetTradeBookMerchantData(context, npcId);
            Character character = DomainManager.Character.GetElement_Objects(npcId);
            Inventory inventory = character.GetInventory();
            List<ItemKey> itemKeys = new List<ItemKey>();

            var orgTemplateId = character.GetOrganizationInfo().OrgTemplateId;
            //AdaptableLog.Info($"门派ID {orgTemplateId}");
            sbyte visibleLevel = 10;
            if (0 < orgTemplateId && orgTemplateId < 16)
            {
                var orgCombatSkillsDisplayData = DomainManager.Organization.GetOrganizationCombatSkillsDisplayData(orgTemplateId);
                visibleLevel = (sbyte)Math.Max(orgCombatSkillsDisplayData.ApprovingRate / 100 - 2, 0);
            }
            var taiwu = DomainManager.Taiwu.GetTaiwu();
            var consummateLevel = taiwu.GetConsummateLevel();
            var learnedCombatSkills = DomainManager.Taiwu.GetTaiwu().GetLearnedCombatSkills();

            if (isFavor)
            {
                itemKeys.AddRange(merchantData.GoodsList0.Items.Keys.ToList().FindAll(IsNonPublicBook(orgTemplateId, isFavor, consummateLevel, visibleLevel, learnedCombatSkills)));
                itemKeys.AddRange(merchantData.GoodsList1.Items.Keys.ToList().FindAll(IsNonPublicBook(orgTemplateId, isFavor, consummateLevel, visibleLevel, learnedCombatSkills)));
            }
            else
            {
                itemKeys.AddRange(merchantData.GoodsList2.Items.Keys.ToList().FindAll(IsNonPublicBook(orgTemplateId, isFavor, consummateLevel, visibleLevel, learnedCombatSkills)));
            }

            itemKeys.AddRange(inventory.Items.Keys.ToList().FindAll(IsNonPublicBook(orgTemplateId, isFavor, consummateLevel, visibleLevel, learnedCombatSkills)));

            __result = DomainManager.Item.GetItemDisplayDataList(itemKeys, -1);
            return false;
        }

        private static Predicate<ItemKey> IsNonPublicBook(sbyte orgTemplateId, bool isFavor, sbyte consummateLevel, sbyte visibleLevel, List<short> learnedCombatSkills)
        {
            return delegate (ItemKey item)
            {
                if (isFavor)
                {
                    if (item.ItemType == 10)
                    {//是书
                        if (orgTemplateId == 16)
                        {//是太吾村
                            return true;
                        }
                        var skillBook = Config.SkillBook.Instance[item.TemplateId];
                        //AdaptableLog.Info($"{skillBook.Name} {consummateLevel} - {(skillBook.Grade - 1) * 2}");
                        if (consummateLevel < (skillBook.Grade - 1) * 2)
                        {//精纯不够
                            return false;
                        }

                        if (skillBook.CombatSkillTemplateId >= 0)
                        {//是技能书
                            var combatSkill = Config.CombatSkill.Instance[skillBook.CombatSkillTemplateId];
                            if (combatSkill.SectId == orgTemplateId && !combatSkill.IsNonPublic)
                            {//技能门派等于当前人物门派,并且技能不是不传之秘
                                return true;
                            }
                        }
                        else
                        {//是生活书
                            return true;
                        }
                    }
                }
                else
                {
                    if (ItemTemplateHelper.GetItemSubType(item.ItemType, item.TemplateId) == 1001)
                    {//1001 可能是门派发的功法书
                        var skillBook = Config.SkillBook.Instance[item.TemplateId];
                        var combatSkill = Config.CombatSkill.Instance[skillBook.CombatSkillTemplateId];

                        if (combatSkill.SectId == orgTemplateId)
                        {//技能门派等于当前人物门派
                            if (learnedCombatSkills.Contains(combatSkill.TemplateId))
                            {//太吾已经学会的
                                return true;
                            }
                            if (combatSkill.SectId == orgTemplateId && !combatSkill.IsNonPublic && visibleLevel >= combatSkill.Grade && consummateLevel >= (skillBook.Grade - 1) * 2)
                            {//技能不是不传之秘,支持度足够,精纯足够
                             //AdaptableLog.Info($"{skillBook.Name} {visibleLevel} == {combatSkill.Grade}");
                                return true;
                            }
                        }
                    }
                }

                return false;
            };
        }

    }
}

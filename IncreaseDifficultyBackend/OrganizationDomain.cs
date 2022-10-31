using GameData.Common;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.Item;
using GameData.Domains.Organization;
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
    public class OrganizationDomainPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(OrganizationDomain), nameof(OrganizationDomain.LeaveOrganization))]
        public static void LeaveOrganizationPrefix(DataContext context, ref Character character, bool charIsDead)
        {
            OrganizationInfo orgInfo = character.GetOrganizationInfo();
            if (orgInfo.SettlementId < 0)
            {
                return;
            }

            List<ItemKey> removeBooks = new List<ItemKey>();

            foreach (var item in character.GetInventory().Items)
            {
                if (item.Key.ItemType == 10)
                {
                    var skillBook = Config.SkillBook.Instance[item.Key.TemplateId];
                    if (skillBook.CombatSkillTemplateId >= 0)
                    {
                        var combatSkill = Config.CombatSkill.Instance[skillBook.CombatSkillTemplateId];
                        if (combatSkill.SectId == orgInfo.OrgTemplateId && combatSkill.IsNonPublic)
                        {
                            removeBooks.Add(item.Key);
                        }
                    }
                }
            }

            if (removeBooks.Count > 0)
            {//离开门派时,没收所有保密书籍
                AdaptableLog.Info($"被没收了{removeBooks.Count}本书籍");
                character.RemoveInventoryItemList(context, removeBooks, true, false);
            }
        }
    }
}

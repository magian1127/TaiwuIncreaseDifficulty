using CharacterDataMonitor;
using GameData.Domains.CombatSkill;
using GameData.Utilities;
using HarmonyLib;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace IncreaseDifficultyFrontend
{
    //[HarmonyPatch]
    public class UI_CharacterMenuCombatSkillPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_CharacterMenuCombatSkill), "UpdateDetailCombatSkillList")]
        public static bool UpdateDetailCombatSkillListPrefix(UI_CharacterMenuCombatSkill __instance)
        {
            int taiwuId = Traverse.Create(__instance).Field("_taiwuCharId").GetValue<int>();
            int characterId = __instance.CharacterMenu.CurCharacterId;
            bool isTaiwu = characterId == taiwuId;
            if (isTaiwu)
            {
                return true;
            }
            var allCombatSkillList = Traverse.Create(__instance).Field("_allCombatSkillList").GetValue<List<CombatSkillDisplayData>>();
            var detailSkillScroll = Traverse.Create(__instance).Field("_detailSkillScroll").GetValue<CombatSkillScrollView>();

            detailSkillScroll.SetCombatSkillList(allCombatSkillList, false, true, "[MOD吾觉太易]标记用数据55667", OnRenderSectSkill);
            var detailRefers = Traverse.Create(__instance).Field("_detailRefers").GetValue<Refers>();
            detailRefers.CGet<GameObject>("NoContent").SetActive(detailSkillScroll.SortAndFilter.OutputSkillList.Count == 0);

            return false;
        }

        public static void OnRenderSectSkill(CombatSkillDisplayData skillData, CombatSkillView skillView)
        {
            var orgTemplateId = SingletonObject.getInstance<CharacterMonitorModel>().GetMonitorItem<BasicInfoMonitor>(skillData.CharId, 0, false).NameRelatedData.OrgTemplateId;
            var CombatSkillItem = Config.CombatSkill.Instance[skillData.TemplateId];
            var sectId = CombatSkillItem.SectId;

            if (orgTemplateId == sectId && CombatSkillItem.IsNonPublic)
            {
                skillView.CGet<TextMeshProUGUI>("Name").text = "未知功法";
                MouseTipDisplayer mouseTips = skillView.GetComponent<MouseTipDisplayer>();
                mouseTips.enabled = false;
            }
        }
    }
}

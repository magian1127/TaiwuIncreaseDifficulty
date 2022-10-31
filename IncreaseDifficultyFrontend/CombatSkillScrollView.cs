using CharacterDataMonitor;
using GameData.Domains.CombatSkill;
using GameData.Utilities;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;

namespace IncreaseDifficultyFrontend
{
    [HarmonyPatch]
    public class CombatSkillScrollViewPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CombatSkillScrollView), "SetCombatSkillList")]
        public static void SetCombatSkillListPrefix(CombatSkillScrollView __instance, List<CombatSkillDisplayData> skillList, bool reset, bool interactable, string listTag, ref Action<CombatSkillDisplayData, CombatSkillView> onRenderSkill, bool addEmptyItem)
        {
            if (skillList == null || skillList.Count < 1)
            {
                return;
            }
            int taiwuId = SingletonObject.getInstance<BasicGameData>().TaiwuCharId;
            int characterId = skillList[0].CharId;
            if (characterId == taiwuId)
            {
                return;
            }
            onRenderSkill = OnRenderSectSkill;
            Traverse.Create(__instance).Field("_onRenderSkill").SetValue(onRenderSkill);
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

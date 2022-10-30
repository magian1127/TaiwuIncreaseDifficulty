using CharacterDataMonitor;
using GameData.Domains.CombatSkill;
using GameData.Utilities;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncreaseDifficultyFrontend
{
    [HarmonyPatch]
    public class CombatSkillScrollViewPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CombatSkillScrollView), "SetCombatSkillList")]
        public static void SetCombatSkillListPrefix(CombatSkillScrollView __instance, List<CombatSkillDisplayData> skillList, bool reset, bool interactable, ref string listTag, Action<CombatSkillDisplayData, CombatSkillView> onRenderSkill, bool addEmptyItem)
        {

            if (listTag == "[MOD吾觉太易]标记用数据55667")
            {
                Traverse.Create(__instance).Field("_onRenderSkill").SetValue(onRenderSkill);
                listTag = null;
            }
        }

    }
}

using GameData.Domains.CombatSkill;
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
    public class CombatSkillPatch
    {
        /// <summary>
        /// 获取提气消耗,轻功的提气都修改成50
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CombatSkill), nameof(CombatSkill.GetCostBreathPercent))]
        public static void GetCostBreathPercentPrefix(CombatSkill __instance)
        {
            if (!IncreaseDifficulty.DisableMobility)
            {
                return;
            }
            
            Config.CombatSkillItem configData = Config.CombatSkill.Instance[__instance.GetId().SkillTemplateId];
            if (configData.EquipType == 2)
            {
                //AdaptableLog.Info($"设置 {configData.Name}");
                Traverse.Create(__instance).Field("_costBreathPercent").SetValue((sbyte)50);
            }
        }
    }
}

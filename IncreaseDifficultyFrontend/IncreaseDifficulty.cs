using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TaiwuModdingLib.Core.Plugin;

namespace IncreaseDifficultyFrontend
{
    [PluginConfig("吾觉太易-前端", "Magian", "0.1.0")]
    public class IncreaseDifficulty : TaiwuRemakeHarmonyPlugin
    {
        public override void Initialize()
        {
            base.Initialize();
            //MethodInfo methodInfo = AccessTools.FirstMethod(typeof(UI_CharacterMenuCombatSkill), (MethodInfo it) => it.Name.Contains("UpdateDetailCombatSkillList"));
            //MethodInfo method = typeof(UI_CharacterMenuCombatSkillPatch).GetMethod("UpdateDetailCombatSkillListPrefix");
            //base.HarmonyInstance.Patch(methodInfo, null, new HarmonyMethod(method), null, null, null);
        }
    }
}

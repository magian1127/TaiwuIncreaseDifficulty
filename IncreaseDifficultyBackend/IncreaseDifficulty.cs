using System;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.Combat;
using GameData.Domains.Item;
using GameData.Domains.Taiwu;
using GameData.Utilities;
using HarmonyLib;
using TaiwuModdingLib;
using TaiwuModdingLib.Core.Plugin;

namespace IncreaseDifficultyBackend
{
    [PluginConfig("吾觉太易", "Magian", "v0.0.5")]
    public class IncreaseDifficulty : TaiwuRemakeHarmonyPlugin
    {
        /// <summary>
        /// 降低历练的倍数
        /// </summary>
        public static int ExpDivisor { get; private set; }

        /// <summary>
        /// 降低亲密度的倍数
        /// </summary>
        public static int FavorabilityDivisor { get; private set; }

        /// <summary>
        /// 更换武器
        /// </summary>
        public static bool ChangeWeapony { get; private set; }

        /// <summary>
        /// 一起放护体
        /// </summary>
        public static bool TogetherDefendSkill { get; private set; }
        

        public override void OnModSettingUpdate()
        {
            int val = 10;

            DomainManager.Mod.GetSetting(base.ModIdStr, "ExpDivisor", ref val);
            ExpDivisor = Math.Clamp(val, 2, 10);
            
            DomainManager.Mod.GetSetting(base.ModIdStr, "FavorabilityDivisor", ref val);
            FavorabilityDivisor = Math.Clamp(val, 2, 10);


            bool bval=true;

            DomainManager.Mod.GetSetting(base.ModIdStr, "ChangeWeapony", ref bval);
            ChangeWeapony = bval;

            DomainManager.Mod.GetSetting(base.ModIdStr, "TogetherDefendSkill", ref bval);
            TogetherDefendSkill = bval;
        }
    }
}

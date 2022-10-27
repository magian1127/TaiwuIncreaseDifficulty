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
    [PluginConfig("吾觉太易", "Magian", "v0.0.3")]
    public class IncreaseDifficulty : TaiwuRemakeHarmonyPlugin
    {
        /// <summary>
        /// 降低亲密度的倍数
        /// </summary>
        public static int favorabilityDivisor = 10;

        /// <summary>
        /// 降低历练的倍数
        /// </summary>
        public static int expDivisor = 10;

    }
}

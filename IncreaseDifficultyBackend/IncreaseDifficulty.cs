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
    [PluginConfig("吾觉太易-后端", "Magian", "v0.0.9")]
    public class IncreaseDifficulty : TaiwuRemakeHarmonyPlugin
    {
        public const string Version = "";

        /// <summary>
        /// 降低历练的倍数
        /// </summary>
        public static int ExpDivisor { get; private set; } = 10;

        /// <summary>
        /// 骗偷抢显示数
        /// </summary>
        public static int CheatStealRobNum { get; private set; } = 3;

        /// <summary>
        /// 更换武器
        /// </summary>
        public static bool ChangeWeapony { get; private set; } = true;

        /// <summary>
        /// 一起放护体
        /// </summary>
        public static bool TogetherDefendSkill { get; private set; } = true;

        /// <summary>
        /// 移动修练提醒
        /// </summary>
        public static bool MoveNotification { get; private set; }


        public override void OnModSettingUpdate()
        {
            int valI = ExpDivisor;
            DomainManager.Mod.GetSetting(base.ModIdStr, "ExpDivisor", ref valI);
            ExpDivisor = Math.Clamp(valI, 2, 10);

            valI = CheatStealRobNum;
            DomainManager.Mod.GetSetting(base.ModIdStr, "CheatStealRobNum", ref valI);
            CheatStealRobNum = Math.Clamp(valI, 3, 10);

            bool valB = ChangeWeapony;
            DomainManager.Mod.GetSetting(base.ModIdStr, "ChangeWeapony", ref valB);
            ChangeWeapony = valB;

            valB = TogetherDefendSkill;
            DomainManager.Mod.GetSetting(base.ModIdStr, "TogetherDefendSkill", ref valB);
            TogetherDefendSkill = valB;

            valB = MoveNotification;
            DomainManager.Mod.GetSetting(base.ModIdStr, "MoveNotification", ref valB);
            MoveNotification = valB;
        }

        public static class EventGuid
        {
            /// <summary>
            /// 偷窃
            /// </summary>
            public const string Steal = "1fdd9d65-a207-4e4a-9f1c-99512cf96fd9";

            /// <summary>
            /// 哄骗
            /// </summary>
            public const string Cheat = "586e9c28-7d1a-4945-b3c5-0394bdd7665c";

            /// <summary>
            /// 抢夺
            /// </summary>
            public const string Rob = "f370a0e3-3ebc-4e52-93bd-9fd75a1d3b78";

            /// <summary>
            /// 送礼
            /// </summary>
            public const string Gift = "5699d2a7-30c6-456e-9fe2-695b674e9e46";
        }
    }
}

using System;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.Combat;
using GameData.Domains.Item;
using GameData.Domains.Mod;
using GameData.Domains.Taiwu;
using GameData.Utilities;
using HarmonyLib;
using TaiwuModdingLib;
using TaiwuModdingLib.Core.Plugin;

namespace IncreaseDifficultyBackend
{
    [PluginConfig("IncreaseDifficultyBackend", "Magian", "0.2.0")]
    public class IncreaseDifficulty : TaiwuRemakePlugin
    {
        public const string Version = "0.2.0";

        /// <summary>
        /// 降低历练的倍数
        /// </summary>
        public static int ExpDivisor { get; private set; } = 10;

        /// <summary>
        /// 哄骗/偷窃/抢夺时，物品选择列表默认可见的物品数量基础值。
        /// 最终可见数量 = BaseVisibleCount + 太吾聪颖 / ClevernessPerExtra。
        /// </summary>
        public const int BaseVisibleCount = 0;

        /// <summary>每多少点聪颖可多看到 1 个物品。</summary>
        public const int ClevernessPerExtra = 10;

        /// <summary>
        /// 更换武器
        /// </summary>
        public static bool ChangeWeapony { get; private set; } = true;

        /// <summary>
        /// 一起放护体
        /// </summary>
        public static bool TogetherDefendSkill { get; private set; } = true;

        /// <summary>
        /// 禁用脚力
        /// </summary>
        public static bool DisableMobility { get; private set; } = false;

        /// <summary>
        /// 移动修练提醒
        /// </summary>
        public static bool MoveNotification { get; private set; } = false;

        /// <summary>日志标签</summary>
        public const string LogTag = "IncreaseDifficulty";

        /// <summary>Harmony 实例，用于选择性挂载 patch（不挂的就保留代码但不生效）</summary>
        private Harmony? _harmony;

        public override void Initialize()
        {
            AdaptableLog.Info($"[{LogTag}] ★后端 Initialize 开始执行★ ModIdStr={ModIdStr}");
            _harmony = new Harmony(ModIdStr);

            // ★ 显式挂载要启用的 patch 类。
            //   其余功能（战斗调整/送礼/读书/移动/突破）的 patch 类保留代码但不挂载 = 禁用。
            //   需要时把对应类加进来即可。
            //
            // 「更保密的不传之秘」相关：
            //   - OrganizationDomainPatch：离开门派没收保密书
            //   - MerchantDomainPatch：交换书籍时把保密功法书从候选移除
            //   - TaiwuDomainPatch：与 NPC 交换物品时过滤保密功法书（GetExchangeDisplayData）
            //   - EventHelperPatch：哄骗/偷窃/抢夺等敌对交互过滤保密功法书 + 按聪颖限制可见物品
            // 「运功按门派限制装备功法」相关：
            //   - CombatSkillDomainPatch：过滤运功界面候选功法列表（GetEquipCombatSkillDisplayData）
            //   - CharacterDomainPatch：拦截装备动作，防自动运功绕过
            try
            {
                _harmony.PatchAll(typeof(OrganizationDomainPatch));
                _harmony.PatchAll(typeof(MerchantDomainPatch));
                _harmony.PatchAll(typeof(TaiwuDomainPatch));
                _harmony.PatchAll(typeof(EventHelperPatch));
                _harmony.PatchAll(typeof(CombatSkillDomainPatch));
                _harmony.PatchAll(typeof(CharacterDomainPatch));
                AdaptableLog.Info($"[{LogTag}] 后端 patch 已挂载");
            }
            catch (Exception ex)
            {
                AdaptableLog.Info($"[{LogTag}] 后端 PatchAll 异常: {ex.Message}");
            }

            try
            {
                RefreshSettings();
            }
            catch (Exception ex)
            {
                AdaptableLog.Info($"[{LogTag}] 后端 RefreshSettings 异常: {ex.Message}");
            }

            // 注册供前端调用的 Mod 方法
            try
            {
                DomainManager.Mod.AddModMethod(
                    ModIdStr,
                    "GetTaiwuConsummateLevel",
                    new Func<DataContext, SerializableModData, SerializableModData>(HandleGetTaiwuConsummateLevel)
                );
                AdaptableLog.Info($"[{LogTag}] 已注册 Mod 方法 GetTaiwuConsummateLevel");
            }
            catch (Exception ex)
            {
                AdaptableLog.Info($"[{LogTag}] 注册 Mod 方法异常: {ex.Message}");
            }

            AdaptableLog.Info($"[{LogTag}] 后端 Initialize 完成");
        }

        /// <summary>
        /// 处理「获取太吾精纯值」的 Mod 方法调用。
        /// 返回：consummate(int，0~9)、maxSects(int，1+精纯，允许的门派功法种类上限)。
        /// </summary>
        private static SerializableModData HandleGetTaiwuConsummateLevel(DataContext context, SerializableModData param)
        {
            var result = new SerializableModData();
            try
            {
                int taiwuId = DomainManager.Taiwu.GetTaiwuCharId();
                var taiwu = DomainManager.Character.GetElement_Objects(taiwuId);
                int consummate = taiwu != null ? taiwu.GetConsummateLevel() : 0;
                result.Set("consummate", consummate);
                result.Set("maxSects", 1 + consummate);
            }
            catch (Exception ex)
            {
                AdaptableLog.Info($"[{LogTag}] HandleGetTaiwuConsummateLevel 异常: {ex.Message}");
                result.Set("consummate", 0);
                result.Set("maxSects", 1);
            }
            return result;
        }

        public override void OnModSettingUpdate()
        {
            RefreshSettings();
        }

        /// <summary>调试模式开关（唯一保留的设置项，控制日志输出）</summary>
        public static bool DebugMode { get; private set; } = false;

        /// <summary>从游戏设置读取调试模式开关。其余功能全部强制开启，不读设置。</summary>
        private void RefreshSettings()
        {
            bool valB = DebugMode;
            DomainManager.Mod.GetSetting(ModIdStr, "DebugMode", ref valB);
            DebugMode = valB;
        }

        public override void Dispose()
        {
            _harmony?.UnpatchSelf();
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

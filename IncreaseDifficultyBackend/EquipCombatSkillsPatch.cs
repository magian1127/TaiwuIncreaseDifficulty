using System;
using System.Reflection;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.Character.Ai;
using GameData.Domains.Character.ParallelModifications;
using GameData.Utilities;
using HarmonyLib;

namespace IncreaseDifficultyBackend
{
    /// <summary>
    /// 自动运功门派限制 —— Patch A：内层 EquipCombatSkills(4参数)，记录「本次是否太吾」标记。
    ///
    /// <see cref="SelectCombatSkillsPatch"/>（Patch B）需要知道当前自动运功是否针对太吾才能
    /// 决定是否过滤可用池。但 SelectCombatSkills 的 context 是 ref struct（Harmony 无法注入），
    /// 故由本 patch（能注入 Character 参数）把判断结果写入 <see cref="EquippingPatchState.IsTaiwuEquip"/>。
    ///
    /// 【为什么用 TargetMethod】内层方法含 sbyte* 指针参数，与 3参数 public 重载同名，attribute
    ///   无法表达指针类型，按 docs/05-harmony-pitfalls.md 坑一方式 B 手动解析。
    /// </summary>
    [HarmonyPatch]
    public class EquipCombatSkillsPatch
    {
        static MethodBase? TargetMethod()
        {
            Type pointerSbyte = typeof(sbyte).MakePointerType();
            return AccessTools.Method(typeof(Equipping), "EquipCombatSkills",
                new[]
                {
                    typeof(Character),       // character
                    pointerSbyte,            // skillSlotTotalCounts
                    typeof(short),           // combatConfigTemplateId
                    typeof(SelectEquipmentsModification)  // mod
                });
        }

        [HarmonyPrefix]
        public static void Prefix(Character character)
        {
            EquippingPatchState.IsTaiwuEquip = character != null
                && character.GetId() == DomainManager.Taiwu.GetTaiwuCharId();
            if (EquippingPatchState.IsTaiwuEquip)
                AdaptableLog.Info($"[IncreaseDifficulty] 自动运功 PatchA：检测到太吾，标记已设置");
        }

        [HarmonyPostfix]
        public static void Postfix() => EquippingPatchState.IsTaiwuEquip = false;
    }

    /// <summary>两个 patch 共享的状态与反射缓存。</summary>
    internal static class EquippingPatchState
    {
        /// <summary>「本次自动运功是否针对太吾」标记。Patch A 设置，Patch B 读取。
        /// 太吾自动运功是玩家点击的同步调用，无并发风险。</summary>
        internal static bool IsTaiwuEquip = false;

        /// <summary>反射缓存：_availableCombatSkills（private，List&lt;CombatSkill&gt;[]，5个池）。</summary>
        internal static readonly FieldInfo? FiAvailableCombatSkills =
            AccessTools.Field(typeof(Equipping), "_availableCombatSkills");
    }
}

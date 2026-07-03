using System;
using System.Collections.Generic;
using System.Linq;
using GameData.Domains;
using GameData.Domains.Character.Ai;
using GameData.Domains.CombatSkill;
using GameData.Utilities;
using HarmonyLib;

namespace IncreaseDifficultyBackend
{
    /// <summary>
    /// 自动运功门派限制 —— Patch B：在 SelectCombatSkills「选池」前过滤可用功法池。
    ///
    /// 【时机】内层 EquipCombatSkills(4参数) 先填 <c>_availableCombatSkills</c>（5个池），再调用 5 次
    ///   SelectCombatSkills 从对应池挑功法装满格子。本 patch 的 Prefix 在 SelectCombatSkills 读池之前
    ///   跑——只过滤数组、不改动原方法逻辑，原方法从合规池正常挑选 → 格子装满 + 门派不超额。
    ///
    /// 【只过滤一次】SelectCombatSkills 被调 5 次（equipType=0..4）。只在 equipType==0（第一次，
    ///   5 个池已全部填好）时过滤全部池；后续 4 次调用看到的是已过滤的共享池子，直接放行。
    ///
    /// 【门派数量规则】N = 1 + 太吾精纯。统计全部门派功法数量，保留最多的 N 类
    ///   （无门派 SectId&lt;=0 不计入限额），其余门派的功法从池中 RemoveAll 移除。
    ///   例：会 A=10、B=3、C=7、D=5 四类门派功法，N=3，保留 A、C、D。
    ///
    /// 【只过滤太吾】非太吾时 <see cref="EquippingPatchState.IsTaiwuEquip"/> 为 false 直接放行，
    ///   NPC/队友自动运功完全不受影响。
    /// </summary>
    [HarmonyPatch(typeof(Equipping), "SelectCombatSkills")]
    public class SelectCombatSkillsPatch
    {
        /// <param name="__instance">Equipping 实例，反射读 _availableCombatSkills。</param>
        /// <param name="equipType">装备类型（0内功 1身法 2-4其他）。只在 0 时过滤一次。</param>
        [HarmonyPrefix]
        public static void Prefix(Equipping __instance, sbyte equipType)
        {
            if (!EquippingPatchState.IsTaiwuEquip || equipType != 0) return;
            if (EquippingPatchState.FiAvailableCombatSkills == null)
            {
                AdaptableLog.Info("[IncreaseDifficulty] 自动运功选池：反射缓存缺失，跳过");
                return;
            }

            try
            {
                var pools = (List<CombatSkill>[]?)EquippingPatchState.FiAvailableCombatSkills.GetValue(__instance);
                if (pools == null) return;

                // 统计 5 个池里各门派功法的数量
                var sectCounts = new Dictionary<sbyte, int>();
                foreach (var pool in pools)
                {
                    if (pool == null) continue;
                    foreach (var skill in pool)
                    {
                        short tmplId = skill.GetId().SkillTemplateId;
                        sbyte sectId = CombatSkillDomainPatch.GetSectId(tmplId);
                        if (sectId <= 0) continue;
                        sectCounts.TryGetValue(sectId, out int c);
                        sectCounts[sectId] = c + 1;
                    }
                }

                int maxSects = 1 + DomainManager.Taiwu.GetTaiwu().GetConsummateLevel();

                // 门派数未超限额，无需过滤
                if (sectCounts.Count <= maxSects)
                {
                    AdaptableLog.Info($"[IncreaseDifficulty] 自动运功选池：门派 {sectCounts.Count} <= 上限 {maxSects}，不限制");
                    return;
                }

                // 保留数量最多的 N 类，其余门派的功法全部移除
                var removeSects = new HashSet<sbyte>(
                    sectCounts
                        .OrderByDescending(kv => kv.Value)
                        .Skip(maxSects)
                        .Select(kv => kv.Key));

                int totalRemoved = 0;
                foreach (var pool in pools)
                {
                    if (pool == null) continue;
                    totalRemoved += pool.RemoveAll(skill =>
                    {
                        short tmplId = skill.GetId().SkillTemplateId;
                        sbyte sectId = CombatSkillDomainPatch.GetSectId(tmplId);
                        return removeSects.Contains(sectId);
                    });
                }

                var kept = sectCounts.OrderByDescending(kv => kv.Value).Take(maxSects).Select(kv => kv.Key.ToString());
                AdaptableLog.Info($"[IncreaseDifficulty] 自动运功选池：门派 {sectCounts.Count} > 上限 {maxSects}，" +
                    $"保留 [{string.Join(",", kept)}]，从可用池移除 {totalRemoved} 个功法（门派 [{string.Join(",", removeSects)}]）");
            }
            catch (Exception ex)
            {
                AdaptableLog.Info($"[IncreaseDifficulty] 自动运功选池过滤异常: {ex.Message}");
            }
        }
    }
}

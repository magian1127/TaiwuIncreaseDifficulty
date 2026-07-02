using System;
using System.Collections.Generic;
using System.Linq;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.CombatSkill;
using GameData.Domains.Item;
using GameData.Utilities;
using HarmonyLib;

namespace IncreaseDifficultyBackend
{
    /// <summary>
    /// 运功界面按门派限制装备功法 —— 候选列表过滤。
    ///
    /// 【需求】太吾受相枢影响，运功界面装备功法时，门派功法的种类数受精纯限制：
    ///   允许门派数 = 1 + 太吾精纯值。无门派功法(SectId&lt;=0)不受限制。
    ///   已装备的门派功法定义「允许门派集合」；未达上限时可选新门派，达上限后只能选已选门派的。
    ///
    /// 【方案】patch CombatSkillDomain.GetEquipCombatSkillDisplayData 的 Postfix。
    ///   这是运功界面取候选功法列表的后端入口（前端 ViewCharacterMenuEquipCombatSkill 通过
    ///   CombatSkillDomainMethod.Call.GetEquipCombatSkillDisplayData 请求）。
    ///   返回的 EquipCombatSkillDisplayData.CombatSkillDisplayDatas 是候选列表，
    ///   CurrentEquipPlan 是已装备的功法。在 Postfix 里按规则过滤候选列表。
    ///
    /// 【只过滤太吾】NPC 运功不受此限制（charId == 太吾才执行）。
    /// 【不动已装备的】已装备功法即使不符合当前规则也保留在 CurrentEquipPlan，避免已装备的凭空消失。
    /// </summary>
    [HarmonyPatch]
    public class CombatSkillDomainPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CombatSkillDomain), nameof(CombatSkillDomain.GetEquipCombatSkillDisplayData))]
        public static void GetEquipCombatSkillDisplayDataPostfix(ref EquipCombatSkillDisplayData __result, DataContext context, int charId)
        {
            try
            {
                if (__result?.CombatSkillDisplayDatas == null || __result.CombatSkillDisplayDatas.Count == 0) return;

                // 只限制太吾
                int taiwuId = DomainManager.Taiwu.GetTaiwuCharId();
                if (charId != taiwuId) return;

                // 收集已装备功法的 TemplateId（5 个分类）
                var equipped = CollectEquippedTemplateIds(__result.CurrentEquipPlan);
                // 算「已装备的门派集合」和「允许的最大门派数」
                var allowedSects = new HashSet<sbyte>();
                foreach (short tmplId in equipped)
                {
                    sbyte sectId = GetSectId(tmplId);
                    if (sectId > 0) allowedSects.Add(sectId);
                }

                int taiwuConsummate = DomainManager.Character.GetElement_Objects(taiwuId).GetConsummateLevel();
                int maxSects = 1 + taiwuConsummate;

                // 未达上限：不限制（玩家可选新门派）
                if (allowedSects.Count < maxSects)
                {
                    AdaptableLog.Info($"[IncreaseDifficulty] 运功过滤：已装备门派 {allowedSects.Count} < 上限 {maxSects}（精纯 {taiwuConsummate}），不限制");
                    return;
                }

                // 已达上限：只保留 allowedSects 里的门派功法 + 无门派功法
                int removed = __result.CombatSkillDisplayDatas.RemoveAll(item =>
                {
                    sbyte sectId = GetSectId(item.TemplateId);
                    return sectId > 0 && !allowedSects.Contains(sectId);
                });

                if (removed > 0)
                    AdaptableLog.Info($"[IncreaseDifficulty] 运功过滤：已装备门派 {allowedSects.Count} >= 上限 {maxSects}，移除 {removed} 个其他门派功法（精纯 {taiwuConsummate}）");
            }
            catch (Exception ex)
            {
                AdaptableLog.Info($"[IncreaseDifficulty] GetEquipCombatSkillDisplayData postfix 异常: {ex.Message}");
            }
        }

        #region 公共规则（供 CharacterDomainPatch 共用）

        /// <summary>收集已装备功法的所有 TemplateId（5 个分类合并）。</summary>
        internal static List<short> CollectEquippedTemplateIds(GameData.Domains.Character.CombatSkillEquipment plan)
        {
            var result = new List<short>();
            if (plan == null) return result;
            // CombatSkillEquipment 有 5 个 ArraySegmentList<short>：Neigong/Attack/Agility/Defense/Assistance
            AddRange(result, plan.Neigong);
            AddRange(result, plan.Attack);
            AddRange(result, plan.Agility);
            AddRange(result, plan.Defense);
            AddRange(result, plan.Assistance);
            return result;
        }

        private static void AddRange(List<short> dst, GameData.Utilities.ArraySegmentList<short> src)
        {
            // ArraySegmentList<short> 是值类型，不会为 null。直接遍历，空列表自然不添加。
            foreach (var v in src) dst.Add(v);
        }

        /// <summary>取功法门派 ID（SectId，sbyte）。&lt;=0 表示无门派。</summary>
        internal static sbyte GetSectId(short templateId)
        {
            var skill = Config.CombatSkill.Instance[templateId];
            return skill?.SectId ?? 0;
        }

        /// <summary>
        /// 判断太吾是否可以装备指定 templateId 的功法（按门派限制规则）。
        /// 供 CharacterDomainPatch.AddEquippedCombatSkill 拦截用。
        /// </summary>
        internal static bool CanTaiwuEquip(short templateId)
        {
            sbyte sectId = GetSectId(templateId);
            if (sectId <= 0) return true;  // 无门派功法，永远可装

            int taiwuId = DomainManager.Taiwu.GetTaiwuCharId();
            var character = DomainManager.Character.GetElement_Objects(taiwuId);
            if (character == null) return true;  // 取不到太吾，不拦截

            var equipped = CollectEquippedTemplateIds(character.GetCombatSkillEquipment());
            var allowedSects = new HashSet<sbyte>();
            foreach (short tmplId in equipped)
            {
                sbyte s = GetSectId(tmplId);
                if (s > 0) allowedSects.Add(s);
            }

            // 已包含此门派 → 可装
            if (allowedSects.Contains(sectId)) return true;

            int maxSects = 1 + character.GetConsummateLevel();
            // 未达上限 → 可装（会引入新门派）
            return allowedSects.Count < maxSects;
        }

        #endregion
    }
}

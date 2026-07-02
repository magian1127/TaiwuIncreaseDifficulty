using System;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Utilities;
using HarmonyLib;

namespace IncreaseDifficultyBackend
{
    /// <summary>
    /// 运功界面按门派限制装备功法 —— 装备动作拦截。
    ///
    /// 【为什么需要拦截】运功界面的候选列表已在 CombatSkillDomainPatch 过滤，但「自动运功」
    ///   （AutoEquipCombatSkills）会绕过候选列表直接装备。这里在装备动作入口做安全网拦截，
    ///   确保无论如何装备都不会超出允许的门派种类数。
    ///
    /// 【规则】同 CombatSkillDomainPatch：允许门派数 = 1 + 太吾精纯；
    ///   无门派功法(SectId&lt;=0)不受限；已装备门派未达上限时可引入新门派，达上限后只能装已有门派的。
    ///
    /// 【只拦截太吾】charId == 太吾才执行。NPC 装备不受限。
    /// </summary>
    [HarmonyPatch]
    public class CharacterDomainPatch
    {
        /* 旧代码,暂时屏蔽
        /// <summary>
        /// 更改太吾获取亲密度
        /// </summary>
        /// <param name="character">目标角色</param>
        /// <param name="relatedChar">太吾</param>
        /// <param name="baseDelta">亲密度</param>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharacterDomain), "CalcFavorabilityDelta")]
        public static void CalcFavorabilityDeltaPrefix(Character character, Character relatedChar, ref int baseDelta)
        {
            bool isTaiwu = relatedChar.GetId() == DomainManager.Taiwu.GetTaiwuCharId();
            if (isTaiwu && baseDelta > 0)
            {
                baseDelta /= IncreaseDifficulty.FavorabilityDivisor;
            }
        }
        */

        /// <summary>
        /// 手动装备单个功法 —— 拦截超出门派额度的装备。
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharacterDomain), nameof(CharacterDomain.AddEquippedCombatSkill))]
        public static bool AddEquippedCombatSkillPrefix(int charId, short skillTemplateId)
        {
            return CheckCanEquip(charId, skillTemplateId, nameof(CharacterDomain.AddEquippedCombatSkill));
        }

        /// <summary>
        /// 自动运功 —— 若当前已达门派上限，直接跳过（避免自动装备引入新门派功法）。
        ///   AutoEquipCombatSkills 内部会按 AI 配置挑功法装，无法逐个拦截，故达上限时整体跳过。
        ///   未达上限时放行（让它自由装，装完仍在上限内）。
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharacterDomain), nameof(CharacterDomain.AutoEquipCombatSkills))]
        public static bool AutoEquipCombatSkillsPrefix(DataContext context, int charId, short combatConfigsTemplateId)
        {
            int taiwuId = DomainManager.Taiwu.GetTaiwuCharId();
            if (charId != taiwuId) return true;  // 非太吾，放行

            // 取已装备门派集合与上限，判断是否还有余量
            var character = DomainManager.Character.GetElement_Objects(taiwuId);
            if (character == null) return true;

            var equipped = CombatSkillDomainPatch.CollectEquippedTemplateIds(character.GetCombatSkillEquipment());
            var allowedSects = new System.Collections.Generic.HashSet<sbyte>();
            foreach (short tmplId in equipped)
            {
                sbyte s = CombatSkillDomainPatch.GetSectId(tmplId);
                if (s > 0) allowedSects.Add(s);
            }
            int maxSects = 1 + character.GetConsummateLevel();

            if (allowedSects.Count >= maxSects)
            {
                AdaptableLog.Info($"[IncreaseDifficulty] 自动运功拦截：已装备门派 {allowedSects.Count} >= 上限 {maxSects}，跳过自动运功（避免引入新门派）");
                return false;  // 达上限，跳过自动运功
            }
            return true;  // 未达上限，放行
        }

        /// <summary>通用装备检查：太吾则按门派规则判断，允许返回 true，拦截返回 false。</summary>
        private static bool CheckCanEquip(int charId, short skillTemplateId, string methodName)
        {
            int taiwuId = DomainManager.Taiwu.GetTaiwuCharId();
            if (charId != taiwuId) return true;  // 非太吾，放行

            bool can = CombatSkillDomainPatch.CanTaiwuEquip(skillTemplateId);
            if (!can)
            {
                sbyte sectId = CombatSkillDomainPatch.GetSectId(skillTemplateId);
                AdaptableLog.Info($"[IncreaseDifficulty] {methodName} 拦截装备功法 tmpl={skillTemplateId} sect={sectId}（超出太吾门派额度）");
            }
            return can;
        }
    }
}

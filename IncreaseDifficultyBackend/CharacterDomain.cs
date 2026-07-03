using System;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Utilities;
using HarmonyLib;

namespace IncreaseDifficultyBackend
{
    /// <summary>
    /// 运功界面按门派限制装备功法 —— 手动单装拦截。
    ///
    /// 【职责】只拦截 <see cref="CharacterDomain.AddEquippedCombatSkill"/>（玩家手动装备单个功法），
    ///   按 CombatSkillDomainPatch 的门派规则判断是否允许装备。
    ///
    /// 【自动运功】不在此处理。「自动运功」AI 会一次性装多个门派功法，无法逐个拦截，
    ///   改由 <see cref="EquippingPatch"/> 在 Equipping.EquipCombatSkills 跑完后按数量规则
    ///   移除超出门派额度的功法。
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
        /// 自动运功的门派限制由 <see cref="EquippingPatch"/> 处理（在 Equipping.EquipCombatSkills
        /// 跑完后按数量规则移除超出门派额度的功法），这里不再拦截 AutoEquipCombatSkills。
        /// </summary>

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

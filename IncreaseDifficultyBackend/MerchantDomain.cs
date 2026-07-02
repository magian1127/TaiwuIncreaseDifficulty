using System;
using System.Collections.Generic;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.Item;
using GameData.Domains.Item.Display;
using GameData.Domains.Merchant;
using GameData.Utilities;
using HarmonyLib;

namespace IncreaseDifficultyBackend
{
    /// <summary>
    /// 「更保密的不传之秘」交换书籍过滤 —— 与 NPC 交换物品时，把该 NPC 门派的保密功法书
    /// 从交换候选列表移除，让玩家无法换到这些书。
    ///
    /// 【方案】patch MerchantDomain.GetTradeBookDisplayData 的 Postfix。
    ///   这是「获取可交换书籍显示数据」的入口（和 NPC 交换书籍经过这里）。
    ///   在它返回 List&lt;ItemDisplayData&gt; 后，遍历移除保密功法书。
    ///
    /// 【★ 与旧代码的区别】旧 MerchantDomainPatch 用 Prefix 自己重写整个方法，依赖
    ///   GetTradeBookMerchantData（已失效）。新方案改用 Postfix，让原版正常生成候选列表，
    ///   我们只在结果上过滤，更稳健、不依赖内部 API。
    ///
    /// 【保密判断】书的 CombatSkill.SectId == 持有 NPC 的门派 && IsNonPublic。
    ///   门派 ID 从 npcId 查 Character.GetOrganizationInfo()（后端可直接 DomainManager 查）。
    /// </summary>
    [HarmonyPatch]
    public class MerchantDomainPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MerchantDomain), nameof(MerchantDomain.GetTradeBookDisplayData))]
        public static void GetTradeBookDisplayDataPostfix(ref List<ItemDisplayData> __result, DataContext context, int npcId, bool isFavor)
        {
            try
            {
                if (__result == null || __result.Count == 0) return;

                // 查持有者的门派 ID
                Character character = DomainManager.Character.GetElement_Objects(npcId);
                if (character == null) return;
                sbyte orgTemplateId = character.GetOrganizationInfo().OrgTemplateId;
                if (orgTemplateId <= 0) return;

                // 移除该门派的保密功法书
                int removed = __result.RemoveAll(item => IsNonPublicBookOfOrg(item.Key, orgTemplateId));
                if (removed > 0)
                    AdaptableLog.Info($"[IncreaseDifficulty] 交换书籍移除 {removed} 本保密功法书 (NPC={npcId} org={orgTemplateId} isFavor={isFavor})");
            }
            catch (Exception ex)
            {
                AdaptableLog.Info($"[IncreaseDifficulty] GetTradeBookDisplayData postfix 异常: {ex.Message}");
            }
        }

        /// <summary>判断一本书是否是指定门派的保密功法书。</summary>
        private static bool IsNonPublicBookOfOrg(ItemKey key, sbyte orgTemplateId)
        {
            if (key.ItemType != 10) return false;  // 只看书籍

            var skillBook = Config.SkillBook.Instance[key.TemplateId];
            if (skillBook == null || skillBook.CombatSkillTemplateId < 0) return false;  // 非功法书

            var combatSkill = Config.CombatSkill.Instance[skillBook.CombatSkillTemplateId];
            if (combatSkill == null) return false;

            return combatSkill.SectId == orgTemplateId && combatSkill.IsNonPublic;
        }
    }
}

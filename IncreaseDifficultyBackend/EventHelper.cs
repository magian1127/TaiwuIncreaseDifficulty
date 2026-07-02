using System;
using System.Collections.Generic;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.Item.Display;
using GameData.Domains.TaiwuEvent;
using GameData.Domains.TaiwuEvent.DisplayEvent;
using GameData.Domains.TaiwuEvent.EventHelper;
using GameData.Utilities;
using HarmonyLib;

namespace IncreaseDifficultyBackend
{
    /// <summary>
    /// 「更保密的不传之秘」+「骗偷抢按聪颖限制可见数量」—— 两个功能合并 patch 同一个方法：
    /// EventHelper.SelectCharacterItemRequest（事件中选择 NPC 物品的统一入口）。
    ///
    /// 【功能A · 保密功法书移除】所有场景下，把持有 NPC 门派的保密功法书从候选移除。
    ///   保密判断：书的 CombatSkill.SectId == 持有 NPC 的门派 && IsNonPublic。
    ///   门派 ID 从 charId 查 Character 的 OrganizationInfo。
    ///
    /// 【功能B · 聪颖限制可见数量】仅当当前事件是 哄骗(Cheat)/偷窃(Steal)/抢夺(Rob) 时生效。
    ///   可见数量 = BaseVisibleCount + 太吾聪颖 / ClevernessPerExtra（默认 0 + 聪颖/10）。
    ///   超出的物品从候选列表随机移除（完全隐藏，看不到=选不到）。
    ///
    ///   【只限物品，不动资源】金钱/材料/威望等「资源」(IsResource=true)、信息、人质等一律保留，
    ///     只有真正的「物品」(ItemDisplayData 且非资源) 参与数量限制。
    ///
    ///   【随机种子】用「当前日期 + charId」做种子：同一天对同一 NPC 反复打开列表看到的物品一致，
    ///     推进一天/换 NPC 才变化，避免每次重开列表都变来变去的糟糕体验。
    ///
    ///   【抽取算法】洗牌抽样（不放回）：从物品子集随机抽出 visibleCount 个保留，其余物品从原列表移除。
    ///     修正旧实现（commit 0cafbc6）里 random.Next(0, Count-1-i) 的索引越界 bug。
    /// </summary>
    [HarmonyPatch]
    public class EventHelperPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EventHelper), nameof(EventHelper.SelectCharacterItemRequest))]
        public static void SelectCharacterItemRequestPostfix(int charId, EventArgBox argBox)
        {
            try
            {
                // 取出选择数据（候选物品列表）
                if (!argBox.Get("SelectItemInfo", out EventSelectItemData data) || data == null) return;
                var list = data.CanSelectItemList;
                if (list == null || list.Count == 0) return;

                // 查持有者，拿门派 ID（功能A 用）
                Character character = DomainManager.Character.GetElement_Objects(charId);
                sbyte orgTemplateId = character?.GetOrganizationInfo().OrgTemplateId ?? 0;

                // —— 功能A：移除该门派的保密功法书 ——
                if (orgTemplateId > 0)
                {
                    int removed = list.RemoveAll(item => IsNonPublicBookOfOrg(item, orgTemplateId));
                    if (removed > 0)
                        AdaptableLog.Info($"[IncreaseDifficulty] 交换候选移除 {removed} 本保密功法书 (NPC={charId} org={orgTemplateId})");
                }

                // —— 功能B：哄骗/偷窃/抢夺 按聪颖限制可见数量 ——
                bool isHostile = DomainManager.TaiwuEvent.IsTriggeredEvent(IncreaseDifficulty.EventGuid.Cheat)
                                 || DomainManager.TaiwuEvent.IsTriggeredEvent(IncreaseDifficulty.EventGuid.Steal)
                                 || DomainManager.TaiwuEvent.IsTriggeredEvent(IncreaseDifficulty.EventGuid.Rob);
                if (!isHostile) return;

                // 【只限制「物品」数量】资源（金钱/材料/威望等）、信息、人质等一律保留，不参与裁剪。
                //   物品判定：item 是 ItemDisplayData 且不是资源（IsResource）。资源虽然也用 ItemDisplayData
                //   表示，但 IsResource=true，应跳过。
                var itemsOnly = list.FindAll(item => item is ItemDisplayData && !item.IsResource);
                if (itemsOnly.Count == 0) return;

                // 读太吾聪颖
                Character taiwu = DomainManager.Character.GetElement_Objects(DomainManager.Taiwu.GetTaiwuCharId());
                sbyte clever = (taiwu != null)
                    ? EventHelper.GetRolePersonality(taiwu, PersonalityType.Clever)
                    : (sbyte)0;

                int visibleCount = IncreaseDifficulty.BaseVisibleCount + clever / IncreaseDifficulty.ClevernessPerExtra;
                if (itemsOnly.Count <= visibleCount)
                {
                    AdaptableLog.Info($"[IncreaseDifficulty] 骗偷抢可见数量：物品 {itemsOnly.Count} <= {visibleCount}（聪颖 {clever}），无需裁剪");
                    return;
                }

                // 随机种子：当前日期 + charId（同天同NPC稳定）
                int seed = DomainManager.World.GetCurrDate() * 100000 + charId;
                var random = new Random(seed);

                // 洗牌抽样：从「物品」集合随机抽出 visibleCount 个保留，其余物品从原列表移除。
                //   资源/信息等非物品项不在 pool 里，不会被删。
                var pool = new List<ITradeableContent>(itemsOnly);
                int take = visibleCount;
                if (take > pool.Count) take = pool.Count;
                var keepSet = new HashSet<ITradeableContent>();
                for (int i = 0; i < take; i++)
                {
                    int idx = random.Next(0, pool.Count);
                    keepSet.Add(pool[idx]);
                    pool.RemoveAt(idx);  // 不放回，避免重复抽中
                }

                // 从原列表移除「未被保留的物品」。资源/信息等保留不动。
                int removedItems = list.RemoveAll(item => item is ItemDisplayData && !item.IsResource && !keepSet.Contains(item));

                AdaptableLog.Info($"[IncreaseDifficulty] 骗偷抢可见数量：聪颖 {clever} → 可见物品 {visibleCount}，移除物品 {removedItems}（资源/信息保留）(NPC={charId} 日期={DomainManager.World.GetCurrDate()})");
            }
            catch (Exception ex)
            {
                AdaptableLog.Info($"[IncreaseDifficulty] SelectCharacterItemRequest postfix 异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 判断一个候选物品是否是指定门派的保密功法书。
        /// </summary>
        private static bool IsNonPublicBookOfOrg(ITradeableContent item, sbyte orgTemplateId)
        {
            if (item.Key.ItemType != 10) return false;  // 只看书籍

            var skillBook = Config.SkillBook.Instance[item.Key.TemplateId];
            if (skillBook == null || skillBook.CombatSkillTemplateId < 0) return false;  // 非功法书

            var combatSkill = Config.CombatSkill.Instance[skillBook.CombatSkillTemplateId];
            if (combatSkill == null) return false;

            return combatSkill.SectId == orgTemplateId && combatSkill.IsNonPublic;
        }
    }
}

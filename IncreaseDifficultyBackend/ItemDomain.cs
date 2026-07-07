using System;
using System.Collections.Generic;
using System.Linq;
using GameData.Common;
using GameData.Domains.Character;
using GameData.Domains.Item;
using GameData.Utilities;
using HarmonyLib;

namespace IncreaseDifficultyBackend
{
    /// <summary>
    /// 促织决斗 —— 后端合并/过滤奖励列表。
    ///
    /// <see cref="CalcEnemyWagersPostfix"/>：过滤不传之秘。
    ///   影响奖励列表 + 额外奖励候选池，不传之秘（保密功法书）不应在任何地方出现。
    ///
    /// <see cref="SelectCricketWagersPostfix"/>：合并 Type==1 物品奖励。
    ///   多个物品类奖励合并为 1 个随机项（玩家不可知具体物品），
    ///   仅影响押注界面的 BetRewards 列表，不影响额外奖励的随机选取。
    ///
    /// 【为什么不在前端做合并？】
    ///   前端修改 <c>BetRewards</c> 列表后，后端结算时 <c>SetCricketBettingResult</c>
    ///   用自己的副本按索引查奖励，前后索引错位导致玩家拿到错误的奖励。
    ///   改后端则数据源头就改好，前后端索引天然一致。
    /// </summary>
    [HarmonyPatch]
    public class ItemDomainPatch
    {
        /// <summary>
        /// CalcEnemyWagers Postfix：过滤不传之秘（保密功法书）。
        /// 候选池来自敌人背包物品，对方门派的保密功法书不应可被赢取。
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemDomain), "CalcEnemyWagers")]
        public static void CalcEnemyWagersPostfix(
            Character character, ref IEnumerable<Wager> __result)
        {
            // __result 为 null 或空直接放行
            if (__result == null) return;

            // 敌方门派 ID（<=0 无门派，无不传之秘）
            sbyte orgId = character.GetOrganizationInfo().OrgTemplateId;
            if (orgId <= 0) return;

            // 包一层过滤：移除该门派的不传之秘（物品类 Wager 才可能是功法书）
            var original = __result;
            __result = original.Where(w => !(w.Type == 1 && ModUtils.IsNonPublicBookOfOrg(w.ItemKey, orgId)));
        }

        /// <summary>
        /// SelectCricketWagers Postfix：合并 Type==1 物品 reward 为 1 个随机项。
        /// 收集所有 Wager.Type==1 的条目，随机保留 1 个，其余从列表删除。
        /// 不修改列表则前后端索引始终一致，选中/结算正确。
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemDomain), "SelectCricketWagers")]
        public static void SelectCricketWagersPostfix(
            DataContext context, ref List<CricketWagerData> __result)
        {
            if (__result == null || __result.Count < 2) return;

            // 收集所有 Type==1 的索引
            var itemIndices = new List<int>();
            for (int i = 0; i < __result.Count; i++)
            {
                if (__result[i]?.Wager.Type == 1)
                    itemIndices.Add(i);
            }

            if (itemIndices.Count < 2) return;

            // 随机选 1 个保留，其余删除（倒序删除避免索引错位）
            int keepIndex = itemIndices[context.Random.Next(itemIndices.Count)];
            for (int k = itemIndices.Count - 1; k >= 0; k--)
            {
                if (itemIndices[k] != keepIndex)
                    __result.RemoveAt(itemIndices[k]);
            }

            AdaptableLog.Info($"[{IncreaseDifficulty.LogTag}] 后端促织：合并 {itemIndices.Count} 个物品 reward 为 1 个");
        }
    }
}

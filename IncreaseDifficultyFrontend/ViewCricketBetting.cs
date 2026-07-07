using System.Collections.Generic;
using Game.Views.Cricket;
using GameData.Domains.Item;
using GameData.Utilities;
using HarmonyLib;

namespace IncreaseDifficultyFrontend
{
    /// <summary>
    /// 促织押注界面 —— Patch A：遮蔽 Type==1（物品类）reward 的显示。
    ///
    /// RefreshRewardList 的 Prefix：记录 BetRewards 中所有 Type==1 的索引到
    /// <see cref="CricketItemMaskShared.HiddenItemIndices"/>，不修改列表本身。
    ///
    /// 【为什么不改列表？】
    ///   后端 <c>TaiwuEventDomain.SetCricketBettingResult</c> 结算奖励时使用自己维护的
    ///   <c>BetRewards</c> 副本，前端修改列表会导致索引错位：
    ///   玩家在修改后的列表选了索引 N，后端用 N 查它未改的列表，拿到错误的奖励。
    ///
    /// 【遮蔽逻辑】
    ///   所有 Type==1 索引加入 HiddenItemIndices → <see cref="CricketBettingRewardItemViewPatch"/>
    ///   将其显示改为"物品"（隐藏名称/图标/品级），玩家无法区分具体物品。
    ///   列表不动则前后端索引始终一致，选中/结算正确。
    /// </summary>
    [HarmonyPatch]
    public class ViewCricketBettingPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ViewCricketBetting), "RefreshRewardList")]
        internal static void RefreshRewardListPrefix(ViewCricketBetting __instance)
        {
            CricketItemMaskShared.HiddenItemIndices.Clear();

            if (CricketItemMaskShared.FiBettingData == null) return;
            var bettingData = CricketItemMaskShared.FiBettingData.GetValue(__instance);
            if (bettingData == null) return;

            // 反射拿 BetRewards（List<CricketWagerData>）
            var rewardsField = AccessTools.Field(bettingData.GetType(), "BetRewards");
            if (rewardsField == null) return;
            var rewards = rewardsField.GetValue(bettingData) as IList<CricketWagerData>;
            if (rewards == null || rewards.Count == 0) return;

            // 收集所有 Type==1（物品类）的索引，加入遮蔽集合
            // 不修改列表本身，确保前后端索引始终同步
            for (int i = 0; i < rewards.Count; i++)
            {
                if (rewards[i]?.Wager.Type == 1)
                {
                    CricketItemMaskShared.HiddenItemIndices.Add(i);
                }
            }

            if (CricketItemMaskShared.HiddenItemIndices.Count > 0)
            {
                AdaptableLog.Info($"[{IncreaseDifficulty.LogTag}] 促织押注：遮蔽 {CricketItemMaskShared.HiddenItemIndices.Count} 个物品 reward（索引：{string.Join(",", CricketItemMaskShared.HiddenItemIndices)}）");
            }
        }
    }
}

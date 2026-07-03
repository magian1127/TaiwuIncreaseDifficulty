using System.Collections.Generic;
using Game.Views.Cricket;
using GameData.Domains.Item;
using GameData.Utilities;
using HarmonyLib;

namespace IncreaseDifficultyFrontend
{
    /// <summary>
    /// 促织押注界面 —— Patch A：数据层合并 Type==1 reward。
    ///
    /// RefreshRewardList 的 Prefix：进入前合并 BetRewards 里所有 Type==1（物品）为 1 个随机项。
    ///
    /// 【合并规则】
    ///   - 收集所有 Wager.Type==1 的 reward
    ///   - 若 ≥ 2 个：随机选 1 个保留，其余从列表删除（倒序删除避免索引错位）
    ///   - 若 0 或 1 个：不动
    ///   - 记录保留的那个在新列表里的索引到 <see cref="CricketItemMaskShared.HiddenItemIndices"/>
    ///   - 同时过滤掉对方门派的不传之秘（保密功法书）
    ///
    /// 【索引安全】删除多余 Type==1 后，Type 0/2/3 的相对顺序不变，_selectedReward 仍有效。
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

            // 拿对方角色门派 ID（用于过滤不传之秘）
            sbyte targetOrgId = CricketItemMaskShared.GetTargetOrgId(bettingData);

            // 先过滤掉 Type==1 里的不传之秘（对方门派的保密功法书）——直接从列表删除
            for (int i = rewards.Count - 1; i >= 0; i--)
            {
                var r = rewards[i];
                if (r == null) continue;
                var w = r.Wager;
                if (w.Type == 1 && ModUtils.IsNonPublicBookOfOrg(w.ItemKey, targetOrgId))
                {
                    rewards.RemoveAt(i);
                    AdaptableLog.Info($"[{IncreaseDifficulty.LogTag}] 促织押注：过滤不传之秘 reward（索引 {i}）");
                }
            }

            // 收集过滤后所有 Type==1 的索引
            var itemIndices = new List<int>();
            for (int i = 0; i < rewards.Count; i++)
            {
                if (rewards[i]?.Wager.Type == 1) itemIndices.Add(i);
            }

            if (itemIndices.Count < 2)
            {
                // 0 或 1 个物品：单个也要遮蔽显示
                if (itemIndices.Count == 1) CricketItemMaskShared.HiddenItemIndices.Add(itemIndices[0]);
                return;
            }

            // 随机选 1 个保留，其余删除（倒序删除）
            int keepOriginalIndex = itemIndices[UnityEngine.Random.Range(0, itemIndices.Count)];
            for (int k = itemIndices.Count - 1; k >= 0; k--)
            {
                int idx = itemIndices[k];
                if (idx != keepOriginalIndex) rewards.RemoveAt(idx);
            }

            // 保留的那个在新列表里的索引
            int newIdx = -1;
            for (int i = 0; i < rewards.Count; i++)
            {
                if (rewards[i]?.Wager.Type == 1) { newIdx = i; break; }
            }
            if (newIdx >= 0) CricketItemMaskShared.HiddenItemIndices.Add(newIdx);

            AdaptableLog.Info($"[{IncreaseDifficulty.LogTag}] 促织押注：合并 {itemIndices.Count} 个物品 reward 为 1 个（新索引 {newIdx}）");
        }
    }
}

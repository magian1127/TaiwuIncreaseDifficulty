using System;
using Game.Views.Cricket;
using GameData.Domains.Item;
using GameData.Utilities;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace IncreaseDifficultyFrontend
{
    /// <summary>
    /// 促织押注界面 —— Patch B：显示层遮蔽物品 reward。
    ///
    /// SetData 的 Prefix：对需要遮蔽的 reward 索引（见 <see cref="CricketItemMaskShared.HiddenItemIndices"/>），
    /// 不执行原方法（阻止异步图标加载/品级背景/tooltip），手动设置占位显示
    /// （名字"物品"、隐藏图标、固定品级背景），保留促织显示。
    /// </summary>
    [HarmonyPatch]
    public class CricketBettingRewardItemViewPatch
    {
        /// <param name="index">reward 在列表中的索引（与 HiddenItemIndices 对应）。</param>
        /// <param name="__instance">CricketBettingRewardItemView 实例。</param>
        /// <param name="reward">CricketWagerData 数据。</param>
        /// <returns>false = 不执行原方法。</returns>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CricketBettingRewardItemView), "SetData")]
        internal static bool SetDataPrefix(CricketBettingRewardItemView __instance, int index, CricketWagerData reward)
        {
            // MonthInteraction 触发的促织事件：跳过遮蔽，执行原方法显示真名
            if (CricketItemMaskShared.MaskDisabled) return true;

            if (!CricketItemMaskShared.HiddenItemIndices.Contains(index)) return true; // 非遮蔽目标，执行原方法

            if (CricketItemMaskShared.FiRewardName == null
                || CricketItemMaskShared.FiRewardCardItem == null
                || CricketItemMaskShared.FiGradeBackImage == null) return true;

            try
            {
                // 固定品级背景（不暴露真实品级）
                var gradeBackImage = CricketItemMaskShared.FiGradeBackImage.GetValue(__instance);
                if (gradeBackImage != null)
                {
                    var setSprite = AccessTools.Method(gradeBackImage.GetType(), "SetSprite",
                        new[] { typeof(string), typeof(bool), typeof(object) });
                    setSprite?.Invoke(gradeBackImage, new object[] { "ui_back_cricketcombat_prepare_bet_0", false, null! });
                }

                // 隐藏物品图标
                var cardItemObj = CricketItemMaskShared.FiRewardCardItem.GetValue(__instance) as Component;
                cardItemObj?.gameObject.SetActive(false);

                // 名字改成"物品"（遮蔽具体类型）
                var rewardName = CricketItemMaskShared.FiRewardName.GetValue(__instance) as TextMeshProUGUI;
                if (rewardName != null)
                {
                    // 先用临时 hoverRoot 隐藏（原方法开头会 SetActive(false)，此处跳过原方法需手动）
                    var hoverRootField = AccessTools.Field(__instance.GetType(), "hoverRoot");
                    (hoverRootField?.GetValue(__instance) as Component)?.gameObject.SetActive(false);

                    rewardName.text = "物品";
                }

                // 递增 renderVersion 防止过期异步回调（原方法开头 +1，此处跳过需手动）
                var renderVersionField = AccessTools.Field(__instance.GetType(), "renderVersion");
                if (renderVersionField != null)
                {
                    int v = (int)renderVersionField.GetValue(__instance)!;
                    renderVersionField.SetValue(__instance, v + 1);
                }

                // 保留促织显示（原方法末尾的 RefreshCrickets）
                CricketItemMaskShared.MiRefreshCrickets?.Invoke(__instance, new object[] { reward });
            }
            catch (Exception ex)
            {
                AdaptableLog.Info($"[{IncreaseDifficulty.LogTag}] 促织押注遮蔽显示异常: {ex.Message}");
            }

            return false; // 不执行原方法
        }
    }
}

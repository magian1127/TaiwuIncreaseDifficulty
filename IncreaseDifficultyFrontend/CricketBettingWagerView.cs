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
    /// 促织战斗界面（提闸开斗）—— Patch C：显示层遮蔽押注物品。
    ///
    /// Render 方法的 Postfix：如果当前渲染的是 Type==1（物品类）押注，则：
    ///   - 将名称替换为"物品"（遮蔽具体类型和名称）
    ///   - 隐藏物品图标卡片
    ///   - 仅保留价值数字显示
    /// </summary>
    [HarmonyPatch]
    public class CricketBettingWagerViewPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CricketBettingWagerView), "Render")]
        internal static void RenderPostfix(CricketBettingWagerView __instance, Wager wager)
        {
            // MonthInteraction 触发的促织事件：跳过遮蔽，物品显示真名
            if (CricketItemMaskShared.MaskDisabled) return;

            // 仅处理物品类押注（Type==1）
            if (wager.Type != 1) return;

            if (CricketItemMaskShared.FiInfoNameText == null || CricketItemMaskShared.FiCardItem == null) return;

            try
            {
                // 将名称替换为"物品"
                var nameText = CricketItemMaskShared.FiInfoNameText.GetValue(__instance) as TextMeshProUGUI;
                if (nameText != null)
                {
                    nameText.text = "物品";
                }

                // 隐藏物品图标卡片（cardItem 的 gameObject）
                var cardItemObj = CricketItemMaskShared.FiCardItem.GetValue(__instance) as Component;
                if (cardItemObj != null)
                {
                    cardItemObj.gameObject.SetActive(false);
                }

                // 如有 wagerItemRoot，也设为不激活（它包裹了 item icon）
                if (CricketItemMaskShared.FiWagerItemRoot != null)
                {
                    var wagerItemRootObj = CricketItemMaskShared.FiWagerItemRoot.GetValue(__instance) as GameObject;
                    if (wagerItemRootObj != null)
                    {
                        wagerItemRootObj.SetActive(false);
                    }
                }
            }
            catch (Exception ex)
            {
                AdaptableLog.Info($"[{IncreaseDifficulty.LogTag}] 促织战斗押注遮蔽异常: {ex.Message}");
            }
        }
    }
}

using System.Collections.Generic;
using System.Reflection;
using Game.Views.Cricket;
using GameData.Domains.Item;
using GameData.Utilities;
using HarmonyLib;
using TMPro;

namespace IncreaseDifficultyFrontend
{
    /// <summary>
    /// 促织决斗物品遮蔽 —— 跨 patch 共享的反射缓存与状态。
    ///
    /// 三个 patch 类（<see cref="ViewCricketBettingPatch"/> / <see cref="CricketBettingRewardItemViewPatch"/>
    /// / <see cref="CricketBettingWagerViewPatch"/>）共同实现「促织押注物品随机遮蔽」：
    ///
    /// A. ViewCricketBetting.RefreshRewardList（押注界面）：把所有物品类（Wager.Type==1）reward 合并成 1 个随机项，
    ///    把需要遮蔽的 reward 索引记入 <see cref="HiddenItemIndices"/>。
    /// B. CricketBettingRewardItemView.SetData（押注界面）：按 <see cref="HiddenItemIndices"/> 遮蔽显示（名字改"物品"、隐藏图标）。
    /// C. CricketBettingWagerView.Render（战斗界面）：遮蔽 Type==1 押注的显示。
    ///
    /// 【为什么押注界面改 BetRewards 数据而非只改显示？】
    ///   _selectedReward 是 BetRewards 的真实索引，OnConfirmBtnClicked 用它取 BetRewards[index] 传后端结算。
    ///   只在显示层合并会让索引错位。直接改 BetRewards（删多余 Type==1），索引天然连续，选中/结算零修改。
    ///   保留的 Type==1 仍带真实 ItemKey（玩家赢了真拿这个物品），显示层负责遮蔽。
    /// </summary>
    internal static class CricketItemMaskShared
    {
        #region 反射缓存

        // ——— 押注界面（ViewCricketBetting / CricketBettingRewardItemView） ———
        internal static FieldInfo? FiBettingData;       // ViewCricketBetting._cricketBettingData
        internal static FieldInfo? FiRewardName;         // CricketBettingRewardItemView.rewardName (TMP)
        internal static FieldInfo? FiRewardCardItem;     // CricketBettingRewardItemView.rewardCardItem (GameObject)
        internal static FieldInfo? FiGradeBackImage;     // CricketBettingRewardItemView.gradeBackImage (CImage)
        internal static MethodInfo? MiRefreshCrickets;   // CricketBettingRewardItemView.RefreshCrickets

        // ——— 战斗界面（CricketBettingWagerView） ———
        internal static FieldInfo? FiInfoNameText;       // CricketBettingWagerView.infoNameText (TMP)
        internal static FieldInfo? FiCardItem;           // CricketBettingWagerView.cardItem (CardItem)
        internal static FieldInfo? FiWagerItemRoot;      // CricketBettingWagerView.wagerItemRoot (GameObject)

        /// <summary>
        /// 押注界面中需要遮蔽显示的 reward 索引集合。
        /// <see cref="ViewCricketBettingPatch"/> 设置，<see cref="CricketBettingRewardItemViewPatch"/> 读取。
        /// 每次 RefreshRewardList 清空重填。
        /// </summary>
        internal static readonly HashSet<int> HiddenItemIndices = new();

        /// <summary>初始化反射缓存，在插件加载时调用。</summary>
        internal static void Init()
        {
            FiBettingData = AccessTools.Field(typeof(ViewCricketBetting), "_cricketBettingData");

            var rewardViewType = typeof(CricketBettingRewardItemView);
            FiRewardName = AccessTools.Field(rewardViewType, "rewardName");
            FiRewardCardItem = AccessTools.Field(rewardViewType, "rewardCardItem");
            FiGradeBackImage = AccessTools.Field(rewardViewType, "gradeBackImage");
            MiRefreshCrickets = AccessTools.Method(rewardViewType, "RefreshCrickets");

            var wagerViewType = typeof(CricketBettingWagerView);
            FiInfoNameText = AccessTools.Field(wagerViewType, "infoNameText");
            FiCardItem = AccessTools.Field(wagerViewType, "cardItem");
            FiWagerItemRoot = AccessTools.Field(wagerViewType, "wagerItemRoot");

            AdaptableLog.Info($"[{IncreaseDifficulty.LogTag}] 促织物品遮蔽反射缓存：押注(rewardName={FiRewardName != null}, " +
                $"rewardCardItem={FiRewardCardItem != null}, gradeBackImage={FiGradeBackImage != null}, " +
                $"refreshCrickets={MiRefreshCrickets != null}) | 战斗(infoNameText={FiInfoNameText != null}, " +
                $"cardItem={FiCardItem != null}, wagerItemRoot={FiWagerItemRoot != null})");
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 从 EventCricketBettingData 拿对方角色的门派 ID。
        /// bettingData.TargetCharacter（CharacterDisplayData）.OrgInfo.OrgTemplateId。
        /// </summary>
        internal static sbyte GetTargetOrgId(object bettingData)
        {
            try
            {
                var targetField = AccessTools.Field(bettingData.GetType(), "TargetCharacter");
                var targetChar = targetField?.GetValue(bettingData);
                if (targetChar == null) return 0;
                var orgInfo = AccessTools.Field(targetChar.GetType(), "OrgInfo")?.GetValue(targetChar);
                if (orgInfo == null) return 0;
                var orgTemplateId = AccessTools.Field(orgInfo.GetType(), "OrgTemplateId")?.GetValue(orgInfo);
                return orgTemplateId is sbyte b ? b : (sbyte)0;
            }
            catch { return 0; }
        }

        #endregion
    }
}

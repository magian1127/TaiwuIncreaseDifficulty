using System;
using System.Collections.Generic;
using System.Linq;
using GameData.Domains.Character;
using GameData.Domains.Item;
using GameData.Utilities;
using HarmonyLib;

namespace IncreaseDifficultyBackend
{
    /// <summary>
    /// 促织决斗「额外奖励」过滤不传之秘 —— 后端拦截物品候选池。
    ///
    /// 【背景】促织决斗获胜触发额外奖励时，后端 <c>ItemDomain.SelectAndTransferExtraWager</c> 调
    ///   <c>CalcEnemyWagers(random, enemy)</c> 从敌人背包算出候选物品池，再随机选一个发给太吾。
    ///   候选池来自敌人背包物品，因此对方门派的保密功法书（不传之秘）可能被选中。
    ///
    /// 【方案】Postfix <c>CalcEnemyWagers</c>：它返回 <c>IEnumerable&lt;Wager&gt;</c>（候选池），
    ///   只需把其中的不传之秘剔掉再交还，<c>SelectAndTransferExtraWager</c> 原封不动继续执行。
    ///   不重写任何原版逻辑（随机、发放都走原方法）。
    ///
    /// 【为什么 hook 这里而非 SelectAndTransferExtraWager】
    ///   SelectAndTransferExtraWager 没有物品参数（物品在它内部现算），无法用「Prefix 改参数」模式。
    ///   CalcEnemyWagers 是候选池的唯一数据源，Postfix 改它的返回值最干净。
    ///
    /// 【影响面】CalcEnemyWagers 同时是押注界面候选池的数据源（SelectCricketWagers 也调它），
    ///   一处过滤让额外奖励和押注候选都不再出现不传之秘。前端 ViewCricketBettingPatch 是基于
    ///   已生成列表的二次过滤，叠加安全（过滤后列表里本就没有不传之秘，二次过滤不触发）。
    ///
    /// 【保密判断】书 ItemType==10 → SkillBook→CombatSkill，SectId==敌方门派 && IsNonPublic。
    ///   统一实现见 <see cref="ModUtils.IsNonPublicBookOfOrg(ItemKey, sbyte)"/>。
    /// </summary>
    [HarmonyPatch]
    public class ItemDomainPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemDomain), "CalcEnemyWagers")]
        public static void CalcEnemyWagersPostfix(
            Character character, ref IEnumerable<Wager> __result)
        {
            // __result 为 null 或空直接放行
            if (__result == null) return;

            // 敌方门派 ID（<=0 无门派，无从谈不传之秘）
            sbyte orgId = character.GetOrganizationInfo().OrgTemplateId;
            if (orgId <= 0) return;

            // 包一层过滤：移除该门派的不传之秘（物品类 Wager 才可能是功法书）
            var original = __result;
            __result = original.Where(w => !(w.Type == 1 && ModUtils.IsNonPublicBookOfOrg(w.ItemKey, orgId)));
        }
    }
}

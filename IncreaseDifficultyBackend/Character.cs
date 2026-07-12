using System;
using System.Collections.Generic;
using System.Linq;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.Item;
using GameData.Utilities;
using HarmonyLib;

namespace IncreaseDifficultyBackend
{
    /// <summary>
    /// 「更保密的不传之秘」遗赠过滤 —— 角色死亡后分配遗赠前，把死者本门派的保密功法书
    /// （不传之秘）从遗赠池移除并销毁，使太吾和所有 NPC（义结金兰等关系受益人）都收不到。
    ///
    /// 【为什么 patch GenerateBequestBooks】死亡遗赠的功法书不是死者背包里的实体书，而是
    ///   GenerateBequestBooks 遍历死者【学过的功法】按概率凭空 CreateSkillBook 生成的。
    ///   这些书填进 tempBooks 字典后，由同一个字典引用传给 DivideBequest 统一分配给所有受益人：
    ///     - 太吾 → TaiwuDomain.AddBequest
    ///     - 其他 NPC → 直接 SetOwner + AddInventoryItem 塞进 NPC 背包
    ///   两条分支读的是同一个池，所以在 GenerateBequestBooks 的 Postfix（分配前）过滤 tempBooks，
    ///   一处即可覆盖太吾和所有 NPC。
    ///
    /// 【为什么 Postfix 要自己销毁物品】被过滤的书已由 GenerateBequestBooks 调
    ///   item.SetOwner(BequestBook, -1) 标记为临时书。DivideBequest 末尾会清理「仍在池中」的
    ///   临时书，但被我们从 tempBooks 移除的书不再在池中，DivideBequest 不会清理它们，会泄漏。
    ///   所以这里移除前先 DomainManager.Item.RemoveItem(context, key) 销毁。
    ///
    /// 【保密判断】复用 ModUtils.IsNonPublicBookOfOrg(key, orgTemplateId)，与现有各过滤一致：
    ///   死者本门派（__instance 门派）的保密功法书才算不传之秘。
    /// </summary>
    [HarmonyPatch]
    public class CharacterPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Character), "GenerateBequestBooks")]
        public static void GenerateBequestBooksPostfix(
            Character __instance, DataContext context, Dictionary<ItemKey, int> tempBooks)
        {
            try
            {
                if (tempBooks == null || tempBooks.Count == 0) return;

                // 死者本门派（<=0 无门派，无不传之秘）
                sbyte orgTemplateId = __instance.GetOrganizationInfo().OrgTemplateId;
                if (orgTemplateId <= 0) return;

                // 挑出死者本门派的保密功法书
                var removeKeys = tempBooks.Keys
                    .Where(key => ModUtils.IsNonPublicBookOfOrg(key, orgTemplateId))
                    .ToList();
                if (removeKeys.Count == 0) return;

                // 移除并销毁（避免游离物品泄漏）
                foreach (var key in removeKeys)
                {
                    DomainManager.Item.RemoveItem(context, key);
                    tempBooks.Remove(key);
                }

                AdaptableLog.Info(
                    $"[IncreaseDifficulty] 遗赠移除 {removeKeys.Count} 本保密功法书 " +
                    $"(死者={__instance.GetId()} org={orgTemplateId})");
            }
            catch (Exception ex)
            {
                AdaptableLog.Info(
                    $"[IncreaseDifficulty] GenerateBequestBooks postfix 异常: {ex.Message}");
            }
        }
    }
}

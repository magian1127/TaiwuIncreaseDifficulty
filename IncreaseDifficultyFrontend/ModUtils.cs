using GameData.Domains.Item;
using GameData.Domains.Item.Display;

namespace IncreaseDifficultyFrontend
{
    /// <summary>
    /// 前端通用工具方法集合。供各 patch 共用，替代散落在各处的同名私有方法。
    ///
    /// 【为什么前后端各一份】前端（netstandard2.1）和后端（net8.0）是两个独立项目，各自编译成
    /// 单独 DLL，无法共享 internal 类。两份签名完全一致，将来若合并项目可无缝对接。
    /// </summary>
    internal static class ModUtils
    {
        /// <summary>
        /// 判断一本书是否属于指定门派的保密功法书（不传之秘）。
        /// 规则：ItemType==10（书籍）→ SkillBook→CombatSkill，SectId==门派 && IsNonPublic。
        /// </summary>
        /// <param name="key">物品 Key。</param>
        /// <param name="orgTemplateId">门派模板 ID（&lt;=0 视为无门派，返回 false）。</param>
        internal static bool IsNonPublicBookOfOrg(ItemKey key, sbyte orgTemplateId)
        {
            if (orgTemplateId <= 0) return false;
            if (key.ItemType != 10) return false;  // 只看书籍

            var skillBook = Config.SkillBook.Instance[key.TemplateId];
            if (skillBook == null || skillBook.CombatSkillTemplateId < 0) return false;  // 非功法书

            var combatSkill = Config.CombatSkill.Instance[skillBook.CombatSkillTemplateId];
            if (combatSkill == null) return false;

            return combatSkill.SectId == orgTemplateId && combatSkill.IsNonPublic;
        }

        /// <summary>
        /// 判断交易候选物品是否属于指定门派的保密功法书（不传之秘）。
        /// <see cref="IsNonPublicBookOfOrg(ItemKey, sbyte)"/> 的 ITradeableContent 重载。
        /// </summary>
        internal static bool IsNonPublicBookOfOrg(ITradeableContent item, sbyte orgTemplateId)
            => IsNonPublicBookOfOrg(item.Key, orgTemplateId);
    }
}

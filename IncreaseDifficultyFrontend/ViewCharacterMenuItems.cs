using System.Reflection;
using Game.Components.Item;
using Game.Components.ListStyleGeneralScroll.Item;
using GameData.Domains.Item.Display;
using GameData.Utilities;
using HarmonyLib;

namespace IncreaseDifficultyFrontend
{
    /// <summary>
    /// 「更保密的不传之秘」前端遮蔽 —— 在 NPC 背包列表里遮蔽保密功法书，禁止查看/交换。
    ///
    /// 【★ 关键：新版本 UI 类】Game.Views.CharacterMenu.ViewCharacterMenuItems（旧 UI_CharacterMenuItems 已废弃）。
    ///
    /// 【遮蔽效果】保密功法书（SectId==NPC门派 && IsNonPublic）：
    ///   ① OnRenderItemSingle Postfix：锁定 + 改名「不传之秘」+ 关 tooltip + 禁交互
    ///   ② OnClickItem Prefix：点击保密功法书时直接拦截（return false），
    ///      不弹出操作菜单 → 无法交换/转移/拿取。这是禁止交换的关键拦截点。
    ///
    /// 【为什么 SetInteractable(false) 不够？】渲染时设的 Interactable 会被后续流程覆盖，
    ///   且交换选中可能走别的判断。直接在点击入口 Prefix 拦截最可靠——保密功法书点了没反应。
    ///
    /// 【门派判断来源】__instance.CurrentCharacterDisplayData.OrgInfo.OrgTemplateId
    /// </summary>
    [HarmonyPatch]
    public class ViewCharacterMenuItemsPatch
    {
        private static FieldInfo? _fiItemBack;       // RowItemMain.itemBack
        private static FieldInfo? _fiLockStatus;     // RowItemMain.lockStatus

        internal static void Init()
        {
            var t = typeof(RowItemMain);
            _fiItemBack = AccessTools.Field(t, "itemBack");
            _fiLockStatus = AccessTools.Field(t, "lockStatus");
            AdaptableLog.Info($"[IncreaseDifficulty] 前端反射缓存：itemBack={_fiItemBack != null} lockStatus={_fiLockStatus != null}");
        }

        /// <summary>
        /// ① 渲染时遮蔽：锁定 + 改名 + 关 tooltip + 换图标。
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Game.Views.CharacterMenu.ViewCharacterMenuItems), "OnRenderItemSingle")]
        public static void OnRenderItemSinglePostfix(
            Game.Views.CharacterMenu.ViewCharacterMenuItems __instance,
            ITradeableContent itemData,
            RowItemLine rowItemLine)
        {
            bool isTaiwu = Traverse.Create(__instance).Property("CharacterMenu").Property("CurrentCharacterIsTaiwu").GetValue<bool>();
            if (isTaiwu) return;

            sbyte orgTemplateId = GetNpcOrgId(__instance);
            if (!IsNonPublicBookOfOrg(itemData, orgTemplateId)) return;

            var rowItemMain = rowItemLine.RowItemMain;
            if (rowItemMain == null) return;

            rowItemMain.ShowInteractionStateLocked();
            rowItemMain.SetName("不传之秘");

            // 关闭所有 tooltip（遍历整个物品行，无论挂哪都能关掉）
            var allInvokers = rowItemLine.GetComponentsInChildren<TooltipInvoker>(true);
            if (allInvokers != null)
                foreach (var invoker in allInvokers) invoker.enabled = false;

            // 主图标用锁定图标覆盖
            try
            {
                if (_fiItemBack?.GetValue(rowItemMain) is ItemBack itemBack &&
                    _fiLockStatus?.GetValue(rowItemMain) is CImage lockStatus && lockStatus.sprite != null)
                    itemBack.SetIcon(lockStatus.sprite);
            }
            catch { }
        }

        /// <summary>
        /// ② 点击拦截：点保密功法书时跳过原方法，不弹操作菜单 → 无法交换/转移。
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Game.Views.CharacterMenu.ViewCharacterMenuItems), "OnClickItem")]
        public static bool OnClickItemPrefix(
            Game.Views.CharacterMenu.ViewCharacterMenuItems __instance,
            ITradeableContent content)
        {
            bool isTaiwu = Traverse.Create(__instance).Property("CharacterMenu").Property("CurrentCharacterIsTaiwu").GetValue<bool>();
            if (isTaiwu) return true;

            sbyte orgTemplateId = GetNpcOrgId(__instance);
            if (IsNonPublicBookOfOrg(content, orgTemplateId))
            {
                AdaptableLog.Info($"[IncreaseDifficulty] 拦截点击保密功法书 tmpl={content.Key.TemplateId}");
                return false;  // 跳过原方法，点了没反应
            }
            return true;
        }

        /// <summary>从当前查看的 NPC 拿门派 ID。</summary>
        private static sbyte GetNpcOrgId(Game.Views.CharacterMenu.ViewCharacterMenuItems instance)
        {
            var displayData = Traverse.Create(instance).Property("CurrentCharacterDisplayData")
                .GetValue<GameData.Domains.Character.Display.CharacterDisplayData>();
            return displayData != null ? displayData.OrgInfo.OrgTemplateId : (sbyte)0;
        }

        /// <summary>判断物品是否是当前 NPC 门派的保密功法书。</summary>
        private static bool IsNonPublicBookOfOrg(ITradeableContent item, sbyte orgTemplateId)
        {
            if (orgTemplateId <= 0) return false;
            if (item.Key.ItemType != 10) return false;

            var skillBook = Config.SkillBook.Instance[item.Key.TemplateId];
            if (skillBook == null || skillBook.CombatSkillTemplateId < 0) return false;

            var combatSkill = Config.CombatSkill.Instance[skillBook.CombatSkillTemplateId];
            if (combatSkill == null) return false;

            return combatSkill.SectId == orgTemplateId && combatSkill.IsNonPublic;
        }
    }
}

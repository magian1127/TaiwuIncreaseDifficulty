using System.Collections.Generic;
using GameData.Utilities;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IncreaseDifficultyFrontend
{
    /// <summary>
    /// 运功界面「按门派限制装备功法」前端两处提示，仅主角界面显示。
    ///
    /// 【提示1 · 已装备门派数】「已装备 X 类门派功法(上限 1+精纯)」
    ///   位置：左侧「选择功法」(AreaTitle) 文字后面。
    ///   X = 已装备的门派种类数（前端从 _equipCombatSkillDisplayData.CurrentEquipPlan 算）。
    ///   上限不显示具体数字，用"1+精纯"文字描述（避免前端跨进程读精纯的复杂度）。
    ///
    /// 【提示2 · 相枢影响说明】「受到相枢影响, 无法运功过多(1+精纯)种类的门派功法。」
    ///   位置：「修改战斗界面功法排序」按钮所在 OperateArea 的下方（OperateArea 的父级 EquipedCombatSkillArea 下）。
    ///   静态文字，不随装备变化。
    ///
    /// 【hook】ViewCharacterMenuEquipCombatSkill.Refresh(bool) 的 Postfix。
    /// 【仅主角】CurCharacterId == TaiwuCharId 才显示。
    /// 【坑六】裸 AddComponent&lt;TextMeshProUGUI&gt; 无中文字形，必须复制界面已有 TMP 的
    ///   font / fontSharedMaterial / spriteAsset。
    /// 【坑三】按实例缓存创建的节点，避免每次 Refresh 重复创建。
    /// </summary>
    [HarmonyPatch]
    public class ViewCharacterMenuEquipCombatSkillPatch
    {
        private const string Tip2Text = "受到相枢影响, 无法运功过多(1+精纯)种类的门派功法。";

        /// <summary>每个界面实例对应的提示1节点（"已装备 X"）。</summary>
        private static readonly Dictionary<Object, TextMeshProUGUI> _tip1 = new();
        /// <summary>每个界面实例对应的提示2节点（相枢影响说明）。</summary>
        private static readonly Dictionary<Object, TextMeshProUGUI> _tip2 = new();

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Game.Views.CharacterMenu.ViewCharacterMenuEquipCombatSkill), "Refresh")]
        public static void RefreshPostfix(Game.Views.CharacterMenu.ViewCharacterMenuEquipCombatSkill __instance, bool needRefreshSkillScroll)
        {
            try
            {
                bool isTaiwu = __instance.CharacterMenu.CurCharacterId
                    == SingletonObject.getInstance<BasicGameData>().TaiwuCharId;

                // 提示2（静态文字）—— 创建一次即可
                if (!_tip2.TryGetValue(__instance, out var t2))
                {
                    t2 = CreateTip2(__instance);
                    if (t2 != null) _tip2[__instance] = t2;
                }
                if (t2 != null) t2.gameObject.SetActive(isTaiwu);

                // 提示1（动态文字 X）—— 先确保节点存在
                if (!_tip1.TryGetValue(__instance, out var t1))
                {
                    t1 = CreateTip1(__instance);
                    if (t1 != null) _tip1[__instance] = t1;
                }

                if (!isTaiwu)
                {
                    if (t1 != null) t1.gameObject.SetActive(false);
                    return;
                }

                // 算分子 X（已装备门派种类数）并更新文字
                int equippedSects = CountEquippedSects(__instance);
                if (t1 != null)
                {
                    t1.gameObject.SetActive(true);
                    t1.text = $"已装备 {equippedSects} 类门派功法";
                }
            }
            catch (System.Exception ex)
            {
                AdaptableLog.Info($"[{IncreaseDifficulty.LogTag}] 运功提示 Refresh postfix 异常: {ex.Message}");
            }
        }

        #region 创建节点

        /// <summary>提示1：左侧"选择功法"(AreaTitle) 后面的"已装备 X/Y 类门派功法"。</summary>
        private static TextMeshProUGUI? CreateTip1(Game.Views.CharacterMenu.ViewCharacterMenuEquipCombatSkill instance)
        {
            // leftArea（私有字段）
            var leftAreaField = AccessTools.Field(typeof(Game.Views.CharacterMenu.ViewCharacterMenuEquipCombatSkill), "leftArea");
            var leftArea = leftAreaField?.GetValue(instance) as RectTransform;
            if (leftArea == null)
            {
                AdaptableLog.Info($"[{IncreaseDifficulty.LogTag}] 提示1：未找到 leftArea");
                return null;
            }

            // 找"选择功法"TMP（名为 AreaTitle）
            var areaTitleTr = FindChildRecursive(leftArea, "AreaTitle");
            var fontSrc = areaTitleTr?.GetComponent<TextMeshProUGUI>();
            if (fontSrc == null || fontSrc.font == null)
            {
                // 兜底：leftArea 下任意有效 TMP
                fontSrc = leftArea.GetComponentInChildren<TextMeshProUGUI>(true);
                if (fontSrc == null || fontSrc.font == null)
                {
                    AdaptableLog.Info($"[{IncreaseDifficulty.LogTag}] 提示1：未找到字体模板 TMP");
                    return null;
                }
            }

            var go = new GameObject("EquippedSectsTip", typeof(RectTransform));
            go.transform.SetParent(leftArea, false);
            go.transform.SetAsLastSibling();

            var rect = (RectTransform)go.transform;
            // 放在 AreaTitle「后方」= 同一行右侧。参照 AreaTitle 锚定，水平向右偏移。
            var srcRect = (RectTransform)fontSrc.transform;
            CopyRectTransform(srcRect, rect);
            // X 向右偏移到 AreaTitle 右侧（AreaTitle 宽度 + 间距），Y 保持一致（同一行）
            rect.anchoredPosition = new Vector2(
                srcRect.anchoredPosition.x + srcRect.rect.width + 8,
                srcRect.anchoredPosition.y);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSrc.fontSize - 2;
            tmp.color = new Color(0.86f, 0.62f, 0.42f, 1f);  // 暖橙
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.raycastTarget = false;
            CopyFont(tmp, fontSrc);

            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            tmp.text = "已装备 0 类门派功法";
            AdaptableLog.Info($"[{IncreaseDifficulty.LogTag}] 提示1节点已创建");
            return tmp;
        }

        /// <summary>提示2：OperateArea 下方（EquipedCombatSkillArea 下）的相枢影响说明。</summary>
        private static TextMeshProUGUI? CreateTip2(Game.Views.CharacterMenu.ViewCharacterMenuEquipCombatSkill instance)
        {
            // 通过 changeCombatSkillSortButton 往上找：按钮 → OperateArea → EquipedCombatSkillArea
            // （ViewCharacterMenuEquipCombatSkill 没有 equipedCombatSkillArea 字段，UI 树层级为
            //   EquipedCombatSkillArea > OperateArea > ChangeCombatSkillSortButton）
            var sortBtnField = AccessTools.Field(typeof(Game.Views.CharacterMenu.ViewCharacterMenuEquipCombatSkill), "changeCombatSkillSortButton");
            var sortBtn = sortBtnField?.GetValue(instance) as Component;
            if (sortBtn == null)
            {
                AdaptableLog.Info($"[{IncreaseDifficulty.LogTag}] 提示2：未找到 changeCombatSkillSortButton");
                return null;
            }
            // parent = OperateArea，parent.parent = EquipedCombatSkillArea（提示2 的父节点）
            var area = sortBtn.transform.parent?.parent as RectTransform;
            if (area == null)
            {
                AdaptableLog.Info($"[{IncreaseDifficulty.LogTag}] 提示2：未找到 EquipedCombatSkillArea（按钮祖父节点）");
                return null;
            }

            // 字体模板：EquipedCombatSkillArea 下任意 TMP（OperateArea 里按钮的 Label）
            var fontSrc = area.GetComponentInChildren<TextMeshProUGUI>(true);
            if (fontSrc == null || fontSrc.font == null)
            {
                AdaptableLog.Info($"[{IncreaseDifficulty.LogTag}] 提示2：未找到字体模板 TMP");
                return null;
            }

            var go = new GameObject("SectLimitTip", typeof(RectTransform));
            go.transform.SetParent(area, false);
            go.transform.SetAsLastSibling();  // 排在 OperateArea 之后（下方）

            var rect = (RectTransform)go.transform;
            // 放在 OperateArea 下方
            var operateAreaTr = FindChildRecursive(area, "OperateArea");
            if (operateAreaTr is RectTransform operateArea)
            {
                CopyRectTransform(operateArea, rect);
                rect.anchoredPosition -= new Vector2(0, operateArea.rect.height + 4);
            }
            else
            {
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0, -50);
            }
            rect.sizeDelta = new Vector2(0, 30);  // 宽度靠 ContentSizeFitter

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = Tip2Text;
            tmp.fontSize = fontSrc.fontSize - 4;
            tmp.color = new Color(0.86f, 0.62f, 0.42f, 1f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            CopyFont(tmp, fontSrc);

            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            AdaptableLog.Info($"[{IncreaseDifficulty.LogTag}] 提示2节点已创建");
            return tmp;
        }

        #endregion

        #region 工具

        /// <summary>
        /// 计算已装备的门派种类数（X）。
        /// 从运功界面实例的 EquippedSkills 字段读已装备功法 TemplateId。
        /// EquippedSkills 是 List&lt;short&gt;[]（5 个分类，每个是已装备功法的 TemplateId 列表）。
        /// 比 CurrentEquipPlan 可靠（后者字段语义与预期不符），也比扫 UI 组件可靠
        /// （UI 上有大量隐藏的预制功法项会被误算）。
        /// </summary>
        private static int CountEquippedSects(Game.Views.CharacterMenu.ViewCharacterMenuEquipCombatSkill instance)
        {
            var equippedField = AccessTools.Field(typeof(Game.Views.CharacterMenu.ViewCharacterMenuEquipCombatSkill), "EquippedSkills");
            if (equippedField == null) return 0;
            var equipped = equippedField.GetValue(instance);
            // EquippedSkills 是 List<short>[]，转为 IList[] 遍历
            if (!(equipped is System.Collections.IList[] categories)) return 0;

            var sects = new HashSet<sbyte>();
            foreach (var cat in categories)
            {
                if (cat == null) continue;
                foreach (var item in cat)
                {
                    short tmplId = (short)item;
                    var skill = Config.CombatSkill.Instance[tmplId];
                    if (skill != null && skill.SectId > 0) sects.Add(skill.SectId);
                }
            }
            return sects.Count;
        }

        private static void CopyFont(TextMeshProUGUI dst, TextMeshProUGUI src)
        {
            dst.font = src.font;
            dst.fontSharedMaterial = src.fontSharedMaterial;
            dst.spriteAsset = src.spriteAsset;
        }

        private static void CopyRectTransform(RectTransform src, RectTransform dst)
        {
            dst.anchorMin = src.anchorMin;
            dst.anchorMax = src.anchorMax;
            dst.pivot = src.pivot;
            dst.anchoredPosition = src.anchoredPosition;
            dst.sizeDelta = src.sizeDelta;
        }

        private static Transform? FindChildRecursive(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == name) return child;
                var found = FindChildRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }

        #endregion
    }
}

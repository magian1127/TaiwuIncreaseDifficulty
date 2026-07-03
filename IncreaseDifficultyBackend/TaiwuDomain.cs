using System;
using System.Collections.Generic;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.Item;
using GameData.Domains.Item.Display;
using GameData.Domains.Taiwu;
using GameData.Domains.Taiwu.Display;
using GameData.Utilities;
using HarmonyLib;

namespace IncreaseDifficultyBackend
{
    [HarmonyPatch]
    public class TaiwuDomainPatch
    {
        /*
         * 【以下为旧功能代码，因游戏版本(1.0.44.0) API 变化而失效，注释保留备查】
         *   - 读书历练相关（SetLifeSkillPageComplete / SetCombatSkillPageComplete /
         *     UpdateLifeSkillBookReadingProgress / UpdateCombatSkillBookReadingProgress）
         *   - 突破固定种子（SelectSkillBreakGrid / InitSkillBreakPlate）
         * 失效点示例：TaiwuDomain.GetElement_SkillBreakPlateDict、SkillBreakPlate.CostedStepCount
         * 已不存在；GetTaiwuLifeSkill 签名变化。需要时按新 API 升级后取消注释。
         *
        /// <summary>
        /// 设置技艺书每页进度,修改每读完一页获取的历练
        /// </summary>
        /// <param name="context"></param>
        /// <param name="book"></param>
        /// <param name="readingPage"></param>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(TaiwuDomain), "SetLifeSkillPageComplete")]
        public static void SetLifeSkillPageCompletePrefix(TaiwuDomain __instance, DataContext context, SkillBook book, byte readingPage)
        {
            ReadingBookStrategies strategies;
            bool getV = Traverse.Create(__instance).Method("TryGetElement_ReadingBooks", new object[] { book.GetItemKey(), strategies }).GetValue<bool>();
            bool can = !getV || !strategies.PageContainsStrategy(readingPage, 12);
            if (can)
            {
                short original = Config.SkillGradeData.Instance[book.GetGrade()].ReadingExpGainPerPage;//原来获取到的exp
                int exp = original / IncreaseDifficulty.ExpDivisor;
                int amend = (-original) + exp;//先减这个方法里设置过的exp,然后再加上修改过后的exp
                __instance.GetTaiwu().ChangeExp(context, amend);
            }
        }

        /// <summary>
        /// 设置功法书每页进度,修改每读完一页获取的历练
        /// </summary>
        /// <param name="context"></param>
        /// <param name="book"></param>
        /// <param name="readingPage"></param>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(TaiwuDomain), "SetCombatSkillPageComplete")]
        public static void SetCombatSkillPageCompletePrefix(TaiwuDomain __instance, DataContext context, SkillBook book, byte internalIndex)
        {
            int bookoriginal = Config.SkillGradeData.Instance[book.GetGrade()].ReadingExpGainPerPage;
            int bookexp = bookoriginal / IncreaseDifficulty.ExpDivisor;
            int bookamend = (-bookoriginal) + bookexp;
            __instance.GetTaiwu().ChangeExp(context, bookamend);

            byte readingPage = GameData.Domains.CombatSkill.CombatSkillStateHelper.GetPageId(internalIndex);
            ReadingBookStrategies strategies;
            bool getV = Traverse.Create(__instance).Method("TryGetElement_ReadingBooks", new object[] { book.GetItemKey(), strategies }).GetValue<bool>();
            bool can = !getV || !strategies.PageContainsStrategy(readingPage, 12);
            if (can)
            {
                short original = Config.SkillGradeData.Instance[book.GetGrade()].ReadingExpGainPerPage;//原来获取到的exp
                int exp = original / IncreaseDifficulty.ExpDivisor;
                int amend = (-original) + exp;//先减这个方法里设置过的exp,然后再加上修改过后的exp
                __instance.GetTaiwu().ChangeExp(context, amend);
            }
        }

        /// <summary>
        /// 更新读取技艺书的进度,修改一次性读完书获取的历练
        /// </summary>
        /// <param name="context"></param>
        /// <param name="book"></param>
        /// <param name="strategies"></param>
        /// <param name="isInBattle"></param>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(TaiwuDomain), "UpdateLifeSkillBookReadingProgress")]
        public static void UpdateLifeSkillBookReadingProgressPrefix(TaiwuDomain __instance, DataContext context, SkillBook book, ReadingBookStrategies strategies, bool isInBattle)
        {
            short skillTemplateId = book.GetLifeSkillTemplateId();
            TaiwuLifeSkill lifeSkill = Traverse.Create(__instance).Method("GetTaiwuLifeSkill(", new object[] { skillTemplateId }).GetValue<TaiwuLifeSkill>();
            byte readingPage = Traverse.Create(__instance).Method("GetCurrentReadingPage", new object[] { book, strategies, lifeSkill }).GetValue<byte>();
            if (readingPage == 5)
            {
                int original = Config.SkillGradeData.Instance[book.GetGrade()].ReadingExpGainPerPage / 10;//原来获取到的exp
                int exp = original / IncreaseDifficulty.ExpDivisor;
                int amend = (-original) + exp;//先减这个方法里设置过的exp,然后再加上修改过后的exp
                __instance.GetTaiwu().ChangeExp(context, amend);
            }
        }

        /// <summary>
        /// 更新读取功法书的进度,修改一次性读完书获取的历练
        /// </summary>
        /// <param name="context"></param>
        /// <param name="book"></param>
        /// <param name="strategies"></param>
        /// <param name="isInBattle"></param>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(TaiwuDomain), "UpdateCombatSkillBookReadingProgress")]
        public static void UpdateCombatSkillBookReadingProgressPrefix(TaiwuDomain __instance, DataContext context, SkillBook book, ReadingBookStrategies strategies, bool isInBattle)
        {
            short skillTemplateId = book.GetCombatSkillTemplateId();
            Config.CombatSkillItem skillConfig = Config.CombatSkill.Instance[skillTemplateId];
            TaiwuCombatSkill combatSkill = Traverse.Create(__instance).Method("GetTaiwuCombatSkill", new object[] { skillTemplateId }).GetValue<TaiwuCombatSkill>();
            byte readingPage = Traverse.Create(__instance).Method("GetCurrentReadingPage", new object[] { book, strategies, combatSkill }).GetValue<byte>();
            if (readingPage == 6)
            {
                int original = Config.SkillGradeData.Instance[book.GetGrade()].ReadingExpGainPerPage / 10;//原来获取到的exp
                int exp = original / IncreaseDifficulty.ExpDivisor;
                int amend = (-original) + exp;//先减这个方法里设置过的exp,然后再加上修改过后的exp
                //AdaptableLog.Info("成功修改功法书" + amend);
                __instance.GetTaiwu().ChangeExp(context, amend);
            }
        }

        /// <summary>
        /// 点击突破格子,修改随机种子为固定
        /// </summary>
        /// <param name="context"></param>
        /// <param name="skillId"></param>
        /// <param name="col"></param>
        /// <param name="row"></param>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(TaiwuDomain), nameof(TaiwuDomain.SelectSkillBreakGrid))]
        public static void SelectSkillBreakGridPrefix(TaiwuDomain __instance, ref DataContext context, short skillId, byte col, byte row)
        {
            SkillBreakPlate plate = __instance.GetElement_SkillBreakPlateDict(skillId);
            string costedStep = plate.CostedStepCount.ToString("00");
            costedStep = costedStep.Substring(costedStep.Length - 2);

            string exp = DomainManager.Taiwu.GetTaiwu().GetExp().ToString("000");
            exp = exp.Substring(exp.Length - 3);

            string colS = col.ToString("00");
            colS = colS.Substring(colS.Length - 2);

            string rowS = row.ToString("00");
            rowS = rowS.Substring(rowS.Length - 2);

            string seeds = DomainManager.World.GetCurrDate().ToString() + costedStep + exp + colS + rowS;
            //AdaptableLog.Info($"点击突破格子{seeds}");
            ulong seed;
            ulong.TryParse(seeds, out seed);

            if (seed == 0)
            {
                return;
            }
            context.Random.Reinitialise(seed);
        }

        /// <summary>
        /// 初始化突破格子,修改随机种子为固定
        /// </summary>
        /// <param name="context"></param>
        /// <param name="skillId"></param>
        /// <param name="plate"></param>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(TaiwuDomain), "InitSkillBreakPlate")]
        public static void InitSkillBreakPlatePrefix(ref DataContext context, short skillId, SkillBreakPlate plate)
        {//使用 技能ID+日期+历练后三位 固定随机的种子 防止重复读取记录
            int currDate = DomainManager.World.GetCurrDate();

            string date = currDate.ToString("0000");
            date = date.Substring(date.Length - 4);

            string exp = DomainManager.Taiwu.GetTaiwu().GetExp().ToString("000");
            exp = exp.Substring(exp.Length - 3);

            string seeds = skillId + date + exp;
            //AdaptableLog.Info($"生成突破格子{seeds}");
            ulong seed;
            ulong.TryParse(seeds, out seed);

            var taiwuCombatSkill = Traverse.Create(DomainManager.Taiwu).Method("GetTaiwuCombatSkill", new object[] { skillId }).GetValue<TaiwuCombatSkill>();
            if (taiwuCombatSkill.LastClearBreakPlateTime == currDate)
            {//当月点过重修,所以改一下种子
                seed += 1;
            }

            if (seed <= 1)
            {
                return;
            }
            context.Random.Reinitialise(seed);
        }
        */ // —— 旧功能失效代码结束 ——


        /// <summary>
        /// 「更保密的不传之秘」交换物品过滤 —— 与 NPC 交换物品（ViewExchange）时，
        /// 把对方门派的保密功法书从交换候选列表移除，让玩家无法看到/换到这些书。
        ///
        /// 【为什么放这里】ViewExchange 走的是 TaiwuDomain.GetExchangeDisplayData（EExchangeType.Person 分支），
        ///   其 TargetItemDisplayDataList = Character.GetAllItems(targetId, ...)，不经过 EventHelper，
        ///   也不经过 MerchantDomain.GetTradeBookDisplayData（那个只管 ViewExchangeBook 书籍交换）。
        ///   所以必须在 GetExchangeDisplayData 的 Postfix 里单独过滤。
        ///
        /// 【保密判断】书的 CombatSkill.SectId == 目标 NPC 的门派 && IsNonPublic。
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(TaiwuDomain), nameof(TaiwuDomain.GetExchangeDisplayData))]
        public static void GetExchangeDisplayDataPostfix(ref ExchangeDisplayData __result, DataContext context, int targetId)
        {
            try
            {
                if (__result?.TargetItemDisplayDataList == null || __result.TargetItemDisplayDataList.Count == 0) return;

                // 查目标 NPC 的门派 ID
                Character character = DomainManager.Character.GetElement_Objects(targetId);
                if (character == null) return;
                sbyte orgTemplateId = character.GetOrganizationInfo().OrgTemplateId;
                if (orgTemplateId <= 0) return;

                // 移除该门派的保密功法书
                int removed = __result.TargetItemDisplayDataList.RemoveAll(item => ModUtils.IsNonPublicBookOfOrg(item.Key, orgTemplateId));
                if (removed > 0)
                    AdaptableLog.Info($"[IncreaseDifficulty] 交换物品移除 {removed} 本保密功法书 (NPC={targetId} org={orgTemplateId})");
            }
            catch (Exception ex)
            {
                AdaptableLog.Info($"[IncreaseDifficulty] GetExchangeDisplayData postfix 异常: {ex.Message}");
            }
        }


    }

}

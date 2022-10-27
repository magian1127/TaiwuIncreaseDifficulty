using System;
using GameData.Common;
using GameData.Domains.Item;
using GameData.Domains.Taiwu;
using GameData.Utilities;
using HarmonyLib;

namespace IncreaseDifficultyBackend
{
    [HarmonyPatch(typeof(TaiwuDomain), "SetLifeSkillPageComplete")]
    public class TaiwuDomain_SetLifeSkillPageComplete
    {
        /// <summary>
        /// 修改每读完一页技艺书获取的历练
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="context"></param>
        /// <param name="book"></param>
        /// <param name="readingPage"></param>
        public static void Prefix(TaiwuDomain __instance, DataContext context, SkillBook book, byte readingPage)
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
    }

    [HarmonyPatch(typeof(TaiwuDomain), "SetCombatSkillPageComplete")]
    public class TaiwuDomain_SetCombatSkillPageComplete
    {
        /// <summary>
        /// 修改每读完一页功法书获取的历练
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="context"></param>
        /// <param name="book"></param>
        /// <param name="readingPage"></param>
        public static void Prefix(TaiwuDomain __instance, DataContext context, SkillBook book, byte internalIndex)
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
    }

    [HarmonyPatch(typeof(TaiwuDomain), "UpdateLifeSkillBookReadingProgress")]
    public class TaiwuDomain_UpdateLifeSkillBookReadingProgress
    {
        /// <summary>
        /// 修改一次性读完技艺书获取的历练
        /// </summary>
        /// <param name="context"></param>
        /// <param name="book"></param>
        /// <param name="strategies"></param>
        /// <param name="isInBattle"></param>
        public static void Prefix(TaiwuDomain __instance, DataContext context, SkillBook book, ReadingBookStrategies strategies, bool isInBattle)
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
    }

    [HarmonyPatch(typeof(TaiwuDomain), "UpdateCombatSkillBookReadingProgress")]
    public class TaiwuDomain_UpdateCombatSkillBookReadingProgress
    {
        /// <summary>
        /// 修改一次性读完功法书获取的历练
        /// </summary>
        /// <param name="context"></param>
        /// <param name="book"></param>
        /// <param name="strategies"></param>
        /// <param name="isInBattle"></param>
        public static void Prefix(TaiwuDomain __instance, DataContext context, SkillBook book, ReadingBookStrategies strategies, bool isInBattle)
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
    }

}

using System;
using GameData.Domains;
using GameData.Domains.Character;
using HarmonyLib;

namespace IncreaseDifficultyBackend
{
    [HarmonyPatch(typeof(CharacterDomain), "CalcFavorabilityDelta")]
    public class CharacterDomain_CalcFavorabilityDelta
    {
        /// <summary>
        /// 更改太吾获取亲密度
        /// </summary>
        /// <param name="character">目标角色</param>
        /// <param name="relatedChar">太吾</param>
        /// <param name="baseDelta">亲密度</param>
        public static void Prefix(Character character, Character relatedChar, ref int baseDelta)
        {
            bool isTaiwu = relatedChar.GetId() == DomainManager.Taiwu.GetTaiwuCharId();
            if (isTaiwu && baseDelta > 0)
            {
                baseDelta /= IncreaseDifficulty.favorabilityDivisor;
            }
        }
    }
}

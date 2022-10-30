﻿using System;
using System.Collections.Generic;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Combat;
using GameData.Domains.CombatSkill;
using GameData.Domains.Item;
using GameData.Utilities;
using HarmonyLib;

namespace IncreaseDifficultyBackend
{
    [HarmonyPatch]
    public class CombatDomainPatch
    {
        /// <summary>
        /// 更改距离时,立马判断是否需要更换武器
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="character"></param>
        /// <param name="index"></param>
        /// <param name="context"></param>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CombatDomain), nameof(CombatDomain.SetMoveState))]
        public static bool SetMoveStatePrefix(CombatDomain __instance, byte state, bool isAlly)
        {
            if (!IncreaseDifficulty.ChangeWeapony)
            {
                return true;
            }

            CombatCharacter enemyChar = __instance.GetCombatCharacter(false);

            if (__instance.InAttackRange(enemyChar))
            {
                return true;
            }

            int usingIndex = enemyChar.GetUsingWeaponIndex();
            if (usingIndex < 0)
            {
                return true;
            }

            short targetDistance = __instance.GetCurrentDistance();

            var weapons = enemyChar.GetWeapons();

            int weaponIndex = weapons[usingIndex].Id;

            if (weaponIndex < 0)
            {
                return true;
            }

            Weapon weapon = DomainManager.Item.GetElement_Weapons(weaponIndex);

            if (!(weapon.GetMinDistance() <= targetDistance && targetDistance <= weapon.GetMaxDistance()))
            {
                var dataContext = enemyChar.GetDataContext();

                for (int i = 0; i < weapons.Length - 3; i++)
                {
                    if (usingIndex == i)
                    {
                        continue;
                    }
                    int id = weapons[i].Id;
                    if (id < 0)
                    {
                        continue;
                    }

                    weapon = DomainManager.Item.GetElement_Weapons(id);

                    if (weapon.GetCurrDurability() <= 0)
                    {
                        continue;
                    }

                    CombatWeaponData weaponData = __instance.GetWeaponData(false, weapons[i]);
                    bool canAtt = (weapon.GetMinDistance() <= targetDistance && targetDistance <= weapon.GetMaxDistance())
                        && weaponData.GetCdFrame() == 0 && weaponData.GetExtraCdFrame() == 0;
                    if (canAtt)
                    {
                        __instance.ChangeWeapon(enemyChar, i, dataContext, false, false);
                        //var uWeapon = __instance.GetWeaponData(false, weapons[usingIndex]);
                        //AdaptableLog.Info($"武器{weapon.GetName()} 索引{i} 距离{targetDistance} 武器{weapon.GetMinDistance()}-{weapon.GetMaxDistance()} CD{uWeapon.GetCdFrame()}-{uWeapon.GetExtraCdFrame()}");
                        return true;
                    }

                }
            }

            return true;
        }

        /// <summary>
        /// 阻止乱换武器
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="character"></param>
        /// <param name="weaponIndex"></param>
        /// <param name="context"></param>
        /// <param name="init"></param>
        /// <param name="force"></param>
        /// <returns></returns>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CombatDomain), nameof(CombatDomain.ChangeWeapon), new Type[] { typeof(CombatCharacter), typeof(int), typeof(DataContext), typeof(bool), typeof(bool) })]
        public static bool ChangeWeaponPrefix(CombatDomain __instance, ref CombatCharacter character, int weaponIndex, DataContext context, bool init, bool force)
        {
            if (!IncreaseDifficulty.ChangeWeapony)
            {
                return true;
            }

            if (character.GetId() == DomainManager.Taiwu.GetTaiwuCharId())
            {
                return true;
            }

            int uWeaponsIndex = character.GetUsingWeaponIndex();
            if (uWeaponsIndex == -1 || uWeaponsIndex >= 3)
            {
                return true;
            }

            short targetDistance = __instance.GetCurrentDistance();
            var weapons = character.GetWeapons();
            Weapon weapon = DomainManager.Item.GetElement_Weapons(weapons[uWeaponsIndex].Id);

            if (weapon.GetCurrDurability() <= 0)
            {
                return true;
            }

            if (weapon.GetMinDistance() <= targetDistance && targetDistance <= weapon.GetMaxDistance())
            {
                character.NeedChangeWeaponIndex = -1;
                character.SetUsingWeaponIndex(uWeaponsIndex, context);
                character.SetChangeTrickAttack(false, context);

                __instance.UpdateAllCommandAvailability(character, context);

                //character.SetAnimationToLoop(__instance.GetProperLoopAni(character, false), context);
                //AdaptableLog.Info($"想乱换的武器{weapon.GetName()} 索引{uWeaponsIndex} 距离{targetDistance} 武器{weapon.GetMinDistance()}-{weapon.GetMaxDistance()}");
                return false;
            }
            else
            {
                return true;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CombatDomain), nameof(CombatDomain.ApplyAgileOrDefenseSkill))]
        public static void ApplyAgileOrDefenseSkillPostfix(CombatDomain __instance, CombatCharacter character, Config.CombatSkillItem skillConfig)
        {
            if (!IncreaseDifficulty.TogetherDefendSkill)
            {
                return;
            }

            bool isTaiwu = character.GetId() == DomainManager.Taiwu.GetTaiwuCharId();
            if (!isTaiwu || skillConfig.EquipType != 3)
            {//不是太吾,或者技能不是护体
                return;
            }

            CombatCharacter enemyChar = __instance.GetCombatCharacter(false);
            if (enemyChar.GetAffectingDefendSkillId() >= 0 || enemyChar.GetPreparingSkillId() >= 0)
            {//没有护体在运行,或者没有在释放技能
                return;
            }

            var enemySkillId = enemyChar.GetDefenceSkillList();
            if (enemySkillId.Length <= 0)
            {//没有护体
                return;
            }

            List<short> canUseSkills = new List<short>();
            foreach (var sId in enemySkillId)
            {
                Config.CombatSkillItem config = Config.CombatSkill.Instance.GetItem(sId);
                if (config == null)
                {
                    continue;
                }

                if (__instance.GetElement_EnemySkillDataDict(new CombatSkillKey(enemyChar.GetId(), config.TemplateId)).GetCanUse())
                {
                    canUseSkills.Add(config.TemplateId);
                }
            }

            if (canUseSkills.Count == 0)
            {
                return;
            }

            Random random = new Random();
            __instance.GetAiInfo(enemyChar).IsDefenseRequiredPositively = false;
            __instance.StartPrepareSkill(__instance.Context, canUseSkills.Count == 1 ? canUseSkills[0] : canUseSkills[random.Next(0, canUseSkills.Count - 1)], false);

        }
    }


}

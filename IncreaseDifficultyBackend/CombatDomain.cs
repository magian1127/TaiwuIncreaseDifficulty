using System;
using System.Collections.Generic;
using GameData.Common;
using GameData.DomainEvents;
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
        /// 设置移动状态,添加立马判断是否需要更换武器
        /// </summary>
        /// <param name="state"></param>
        /// <param name="index"></param>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CombatDomain), nameof(CombatDomain.SetMoveState))]
        public static bool SetMoveStatePrefix(CombatDomain __instance, byte state, bool isAlly)
        {
            if (!IncreaseDifficulty.ChangeWeapony)
            {
                return true;
            }

            CombatCharacter character = __instance.GetCombatCharacter(isAlly);

            if (__instance.InAttackRange(character))
            {
                return true;
            }

            int usingIndex = character.GetUsingWeaponIndex();
            if (usingIndex < 0)
            {
                return true;
            }

            short targetDistance = __instance.GetCurrentDistance();

            var weapons = character.GetWeapons();

            int weaponIndex = weapons[usingIndex].Id;

            if (weaponIndex < 0)
            {
                return true;
            }

            Weapon weapon = DomainManager.Item.GetElement_Weapons(weaponIndex);

            if (!(weapon.GetMinDistance() <= targetDistance && targetDistance <= weapon.GetMaxDistance()))
            {
                var dataContext = character.GetDataContext();

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

                    CombatWeaponData weaponData = __instance.GetWeaponData(isAlly, weapons[i]);
                    bool canAtt = (weapon.GetMinDistance() <= targetDistance && targetDistance <= weapon.GetMaxDistance())
                        && weaponData.GetCdFrame() == 0 && weaponData.GetExtraCdFrame() == 0;
                    if (canAtt)
                    {
                        __instance.ChangeWeapon(character, i, dataContext, false, false);
                        //var uWeapon = __instance.GetWeaponData(false, weapons[usingIndex]);
                        //AdaptableLog.Info($"武器{weapon.GetName()} 索引{i} 距离{targetDistance} 武器{weapon.GetMinDistance()}-{weapon.GetMaxDistance()} CD{uWeapon.GetCdFrame()}-{uWeapon.GetExtraCdFrame()}");
                        return true;
                    }

                }
            }

            return true;
        }

        /// <summary>
        /// 更换武器,阻止当前能打到目标的更换武器
        /// </summary>
        /// <param name="character"></param>
        /// <param name="weaponIndex"></param>
        /// <param name="context"></param>
        /// <param name="init"></param>
        /// <param name="force"></param>
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

        /// <summary>
        /// 释放轻功或护体,修改为太吾放护体,敌人就尝试也放护体
        /// </summary>
        /// <param name="character"></param>
        /// <param name="skillConfig"></param>
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

                CombatSkillData combatSkillData;
                if (__instance.TryGetElement_EnemySkillDataDict(new CombatSkillKey(enemyChar.GetId(), config.TemplateId), out combatSkillData) && combatSkillData.GetCanUse())
                {
                    canUseSkills.Add(config.TemplateId);
                }
            }

            if (canUseSkills.Count == 0)
            {
                return;
            }

            var aiInfo = __instance.GetAiInfo(enemyChar);
            if (aiInfo != null)
            {
                aiInfo.IsDefenseRequiredPositively = false;
            }

            Random random = new Random();
            __instance.StartPrepareSkill(__instance.Context, canUseSkills.Count == 1 ? canUseSkills[0] : canUseSkills[random.Next(0, canUseSkills.Count - 1)], false);

        }

        /// <summary>
        /// 更改脚力,锁定脚力
        /// </summary>
        /// <param name="context"></param>
        /// <param name="character"></param>
        /// <param name="addValue"></param>
        /// <param name="changedByEffect"></param>
        /// <param name="changer"></param>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CombatDomain), nameof(CombatDomain.ChangeMobilityValue))]
        public static void ChangeMobilityValuePrefix(CombatDomain __instance, DataContext context, ref CombatCharacter character, ref int addValue, bool changedByEffect, CombatCharacter changer)
        {
            if (!IncreaseDifficulty.DisableMobility)
            {
                return;
            }

            if (addValue < 0)
            {
                addValue = 100;
            }
        }

        /// <summary>
        /// 计算普通攻击,修改轻功可以闪躲普通攻击
        /// </summary>
        /// <param name="character"></param>
        /// <param name="context"></param>
        /// <param name="trickType"></param>
        /// <returns></returns>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CombatDomain), "CalcNormalAttack")]
        public static bool CalcNormalAttackPrefix(CombatDomain __instance, CombatCharacter character, DataContext context, sbyte trickType)
        {
            if (!IncreaseDifficulty.DisableMobility)
            {
                return true;
            }

            CombatCharacter enemyChar = __instance.GetCombatCharacter(!character.IsAlly, true);
            var skillId = enemyChar.GetAffectingMoveSkillId();
            if (skillId == -1)
            {
                return true;
            }
            //CombatSkill skill = DomainManager.CombatSkill.GetElement_CombatSkills(new CombatSkillKey(character.GetId(), skillId));

            if (!context.Random.CheckPercentProb(Config.CombatSkill.Instance[skillId].Grade * 5 + 5))
            {
                return true;
            }

            //AdaptableLog.Info($"{character.GetCharacter().GetGivenName()} 普通攻击被闪躲");

            Events.RaiseNormalAttackBegin(context, character, enemyChar, trickType, (int)character.PursueAttackCount);

            Events.RaiseNormalAttackCalcHitEnd(context, character, enemyChar, character.NormalAttackHitType, (int)character.PursueAttackCount, false, false);

            Events.RaiseNormalAttackEnd(context, character, enemyChar, trickType, (int)character.PursueAttackCount, false, false);

            return false;
        }

    }

}

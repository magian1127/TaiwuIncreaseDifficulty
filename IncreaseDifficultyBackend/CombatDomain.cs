using System;
using System.Collections.Generic;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Combat;
using GameData.Domains.Item;
using GameData.Utilities;
using HarmonyLib;

namespace IncreaseDifficultyBackend
{
    [HarmonyPatch(typeof(CombatDomain), nameof(CombatDomain.SetMoveState))]
    public class CombatDomain_SetMoveState
    {
        /// <summary>
        /// 更改距离时,立马判断是否需要更换武器
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="character"></param>
        /// <param name="index"></param>
        /// <param name="context"></param>
        public static bool Prefix(CombatDomain __instance, byte state, bool isAlly)
        {
            CombatCharacter enemyChar = __instance.GetCombatCharacter(false);
            if (!__instance.InAttackRange(enemyChar))
            {
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
                        CombatWeaponData weaponData = __instance.GetWeaponData(false, weapons[i]);
                        bool canAtt = (weapon.GetMinDistance() <= targetDistance && targetDistance <= weapon.GetMaxDistance())
                            && weaponData.GetCdFrame() == 0 && weaponData.GetExtraCdFrame() == 0;
                        if (canAtt)
                        {
                            var dataContext = enemyChar.GetDataContext();
                            __instance.ChangeWeapon(enemyChar, i, dataContext, false, false);
                            //var uWeapon = __instance.GetWeaponData(false, weapons[usingIndex]);
                            //AdaptableLog.Info($"武器{weapon.GetName()} 索引{i} 距离{targetDistance} 武器{weapon.GetMinDistance()}-{weapon.GetMaxDistance()} CD{uWeapon.GetCdFrame()}-{uWeapon.GetExtraCdFrame()}");
                            return true;
                        }

                    }
                }

            }

            return true;
        }
    }

    [HarmonyPatch(typeof(CombatDomain), nameof(CombatDomain.ChangeWeapon), new Type[] { typeof(CombatCharacter), typeof(int), typeof(DataContext), typeof(bool), typeof(bool) })]
    public class CombatDomain_ChangeWeapon
    {
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
        public static bool Prefix(CombatDomain __instance, CombatCharacter character, int weaponIndex, DataContext context, bool init, bool force)
        {
            if (character.GetId() == DomainManager.Taiwu.GetTaiwuCharId())
            {
                return true;
            }

            int uWeaponsIndex = character.GetUsingWeaponIndex();
            if (uWeaponsIndex == -1)
            {
                return true;
            }

            short targetDistance = __instance.GetCurrentDistance();
            var weapons = character.GetWeapons();
            Weapon weapon = DomainManager.Item.GetElement_Weapons(weapons[uWeaponsIndex].Id);

            if (weapon.GetMinDistance() <= targetDistance && targetDistance <= weapon.GetMaxDistance())
            {
                character.NeedChangeWeaponIndex = -1;
                character.SetUsingWeaponIndex(uWeaponsIndex, context);
                character.SetChangeTrickAttack(false, context);

                __instance.UpdateAllCommandAvailability(character, context);
                character.SetAnimationToLoop(__instance.GetProperLoopAni(character, false), context);
                //AdaptableLog.Info($"想乱换的武器{weapon.GetName()} 索引{uWeaponsIndex} 距离{targetDistance} 武器{weapon.GetMinDistance()}-{weapon.GetMaxDistance()}");
                return false;
            }
            else
            {
                return true;
            }
        }
    }

}

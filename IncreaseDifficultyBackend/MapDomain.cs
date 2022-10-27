using System;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.Map;
using HarmonyLib;
using Redzen.Random;
using System.Collections.Generic;
using GameData.Domains.CombatSkill;
using GameData.Utilities;

namespace IncreaseDifficultyBackend
{
    [HarmonyPatch(typeof(MapDomain), "Move", new Type[] { typeof(DataContext), typeof(short) })]
    public class MapDomain_Move_Patch
    {
        public static void Postfix(MapDomain __instance, DataContext context, short destBlockId)
        {
            Character taiwuChar = DomainManager.Taiwu.GetTaiwu();
            Location srcLocation = taiwuChar.GetLocation();
            Location destLocation = new Location(srcLocation.AreaId, destBlockId);
            //MapAreaData areaData = __instance.GetElement_Areas(srcLocation.AreaId);
            MapBlockData blockData = __instance.GetBlock(destLocation);
            Config.MapBlockItem blockConfig = blockData.GetConfig();
            int costDays = (int)blockConfig.MoveCost * DomainManager.Taiwu.GetMoveTimeCostPercent() / 100;
            if (costDays <= DomainManager.World.GetLeftDaysInCurrMonth())
            {
                List<short> learnedCombatSkills = DomainManager.Taiwu.GetTaiwu().GetLearnedCombatSkills();
                List<CombatSkill> list = new List<CombatSkill>();
                foreach (var item in learnedCombatSkills)
                {
                    GameData.Domains.CombatSkill.CombatSkill element_CombatSkills = DomainManager.CombatSkill.GetElement_CombatSkills(new CombatSkillKey(taiwuChar.GetId(), item));
                    bool canPractice = !element_CombatSkills.GetRevoked() && element_CombatSkills.GetPracticeLevel() < 100;

                    if (canPractice)
                    {
                        var combatSkillItem = Config.CombatSkill.Instance[item];
                        if (combatSkillItem.EquipType == 2)
                        {
                            list.Add(element_CombatSkills);
                        }
                    }

                }

                IRandomSource random = context.Random;

                int skilllCount = list.Count;
                CombatSkill combatSkill = null;
                bool canSet = false;
                if (skilllCount > 0)
                {
                    canSet = true;
                    combatSkill = list[skilllCount == 1 ? 0 : random.Next(0, skilllCount - 1)];
                }

                if (blockConfig.TemplateId == 124)
                {//暗渊
                    taiwuChar.ChangeExp(context, 1000);
                    if (canSet)
                    {
                        var practiceLevel = Math.Min((int)combatSkill.GetPracticeLevel() + random.Next(1, 3), 100);
                        combatSkill.SetPracticeLevel((sbyte)practiceLevel, context);
                    }
                }
                else
                {
                    switch (costDays)
                    {
                        case 0:
                            break;
                        case 1:
                            taiwuChar.ChangeExp(context, 1);
                            if (canSet && random.CheckPercentProb(25))
                            {
                                var practiceLevel = Math.Min((int)combatSkill.GetPracticeLevel() + random.Next(0, 1), 100);
                                combatSkill.SetPracticeLevel((sbyte)practiceLevel, context);
                            }
                            break;
                        case 2:
                            taiwuChar.ChangeExp(context, 10);
                            if (canSet && random.CheckPercentProb(50))
                            {
                                var practiceLevel = Math.Min((int)combatSkill.GetPracticeLevel() + random.Next(0, 2), 100);
                                combatSkill.SetPracticeLevel((sbyte)practiceLevel, context);
                            }
                            break;
                        case 3:
                            taiwuChar.ChangeExp(context, 100);
                            if (canSet && random.CheckPercentProb(75))
                            {
                                var practiceLevel = Math.Min((int)combatSkill.GetPracticeLevel() + random.Next(1, 2), 100);
                                combatSkill.SetPracticeLevel((sbyte)practiceLevel, context);
                            }
                            break;
                        default:
                            break;
                    }
                }

            }

        }

    }
}

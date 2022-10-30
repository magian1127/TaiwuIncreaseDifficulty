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
    [HarmonyPatch]
    public class MapDomainPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapDomain), "Move", new Type[] { typeof(DataContext), typeof(short) })]
        public static void MovePostfix(MapDomain __instance, DataContext context, short destBlockId)
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

                int exp = 0;
                switch (costDays)
                {
                    case 0:
                        break;
                    case 1:
                        exp = 1;
                        taiwuChar.ChangeExp(context, exp);
                        if (canSet && random.CheckPercentProb(25))
                        {
                            var practiceLevel = Math.Min((int)combatSkill.GetPracticeLevel() + 1, 100);
                            combatSkill.SetPracticeLevel((sbyte)practiceLevel, context);
                            if (IncreaseDifficulty.MoveNotification)
                            {
                                DomainManager.World.GetInstantNotificationCollection().AddCombatSkillLearned(taiwuChar.GetId(), combatSkill.GetId().SkillTemplateId);
                            }
                        }
                        break;
                    case 2:
                        exp = random.Next(1, IncreaseDifficulty.ExpDivisor);
                        taiwuChar.ChangeExp(context, exp);
                        if (canSet && random.CheckPercentProb(50))
                        {
                            var practiceLevel = Math.Min((int)combatSkill.GetPracticeLevel() + random.Next(1, 2), 100);
                            combatSkill.SetPracticeLevel((sbyte)practiceLevel, context);
                            if (IncreaseDifficulty.MoveNotification)
                            {
                                DomainManager.World.GetInstantNotificationCollection().AddCombatSkillLearned(taiwuChar.GetId(), combatSkill.GetId().SkillTemplateId);
                            }
                        }
                        break;
                    case 3:
                        exp = random.Next(10, 10 * IncreaseDifficulty.ExpDivisor);
                        taiwuChar.ChangeExp(context, exp);
                        if (canSet && random.CheckPercentProb(75))
                        {
                            var practiceLevel = Math.Min((int)combatSkill.GetPracticeLevel() + random.Next(1, 3), 100);
                            combatSkill.SetPracticeLevel((sbyte)practiceLevel, context);
                            if (IncreaseDifficulty.MoveNotification)
                            {
                                DomainManager.World.GetInstantNotificationCollection().AddCombatSkillLearned(taiwuChar.GetId(), combatSkill.GetId().SkillTemplateId);
                            }
                        }
                        break;
                    case 4://暗渊
                        exp = random.Next(100, 100 * IncreaseDifficulty.ExpDivisor);
                        taiwuChar.ChangeExp(context, exp);
                        if (canSet)
                        {
                            var practiceLevel = Math.Min((int)combatSkill.GetPracticeLevel() + random.Next(1, 5), 100);
                            combatSkill.SetPracticeLevel((sbyte)practiceLevel, context);
                            if (IncreaseDifficulty.MoveNotification)
                            {
                                DomainManager.World.GetInstantNotificationCollection().AddCombatSkillLearned(taiwuChar.GetId(), combatSkill.GetId().SkillTemplateId);
                            }
                        }
                        break;
                    default:
                        break;
                }


            }

        }

    }
}

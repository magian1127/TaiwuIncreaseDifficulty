using CharacterDataMonitor;
using GameData.Domains.Item.Display;
using GameData.Utilities;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;

namespace IncreaseDifficultyFrontend
{
    [HarmonyPatch]
    public class UI_CharacterMenuItemsPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_CharacterMenuItems), "OnRenderItemSingle")]
        public static void OnRenderItemSinglePrefix(UI_CharacterMenuItems __instance, ItemDisplayData itemData, ItemView itemView)
        {
            int taiwuId = SingletonObject.getInstance<BasicGameData>().TaiwuCharId;
            int characterId = __instance.CharacterMenu.CurCharacterId;
            if (characterId == taiwuId)
            {
                return;
            }

            if (itemData.Key.ItemType == 10)
            {
                var orgTemplateId = SingletonObject.getInstance<CharacterMonitorModel>().GetMonitorItem<BasicInfoMonitor>(characterId, 0, false).NameRelatedData.OrgTemplateId;
                var skillBook = Config.SkillBook.Instance[itemData.Key.TemplateId];
                if (skillBook.CombatSkillTemplateId >= 0)
                {
                    var CombatSkillItem = Config.CombatSkill.Instance[skillBook.CombatSkillTemplateId];
                    var sectId = CombatSkillItem.SectId;
                    if (orgTemplateId == sectId && CombatSkillItem.IsNonPublic)
                    {
                        itemView.SetLocked(true);
                        int num = itemView.Names.IndexOf("Name");
                        if (num >= 0)
                        {
                            itemView.CGet<TextMeshProUGUI>("Name").text = "不传之秘";
                        }
                        MouseTipDisplayer mouseTips = itemView.GetComponent<MouseTipDisplayer>();
                        mouseTips.enabled = false;
                    }
                }
            }
        }
    }
}

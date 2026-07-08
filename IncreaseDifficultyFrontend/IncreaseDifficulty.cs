using HarmonyLib;
using TaiwuModdingLib.Core.Plugin;
using GameData.Utilities;

namespace IncreaseDifficultyFrontend
{
    [PluginConfig("IncreaseDifficultyFrontend", "Magian", "0.2.0")]
    public class IncreaseDifficulty : TaiwuRemakePlugin
    {
        public const string LogTag = "IncreaseDifficulty";

        private Harmony? _harmony;

        public override void Initialize()
        {
            _harmony = new Harmony(ModIdStr);

            // 先建立反射缓存（patch 运行时依赖），再 PatchAll
            ViewCharacterMenuItemsPatch.Init();
            CricketItemMaskShared.Init();

            // ★ PatchAll() 扫描所有带 [HarmonyPatch] 特性的类。
            _harmony.PatchAll();

            AdaptableLog.Info($"[{LogTag}] 前端已加载，不传之秘遮蔽 + 促织物品遮蔽(patch已挂载)");
        }

        public override void Dispose()
        {
            CricketItemMaskShared.UnregisterModDisplayEvent();
            _harmony?.UnpatchSelf();
        }
    }
}


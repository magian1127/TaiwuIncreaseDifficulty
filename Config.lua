return {
	Description = [[太吾绘卷难度调整与扩展。

所有功能默认全部开启，各功能之前互相有依赖，不支持单独关闭。

当前功能：
1 更保密的不传之秘 —— 无法查看，交换，哄骗偷窃等；离开门派会被没收。
2 促织决斗物品随机 —— 随机物品无法知道具体是什么；对方门派的不传之秘不参与押注。
3 哄骗偷窃抢夺按聪颖显示 —— 物品选择列表只显示'聪颖/10'个；非物品的资源等不受影响。
4 运功门派功法限制 —— 太吾只能运功(1+精纯)种类的门派功法，无门派功法不受限。

调试模式可在设置中开启，输出详细日志到 Player.log 用于排查问题。

源码
https://github.com/magian1127/TaiwuIncreaseDifficulty
]],
	Cover = "Cover.jpg",
	Author = "Magian",
	FileId = 3756095116,
	Source = 1,
	GameVersion = "1.0.44.0",
	Version = "0.3.0.0",
	Title = "吾觉太易",
	FrontendPlugins = {
		[1] = "IncreaseDifficultyFrontend.dll",
	},
	BackendPlugins = {
		[1] = "IncreaseDifficultyBackend.dll",
	},
	TagList = {
		[1] = "Modifications",
		[2] = "Extensions",
		[3] = "Optimizations",
	},
	DefaultSettings = {
		[1] = {
			SettingType = "Toggle",
			Key = "DebugMode",
			DisplayName = "调试模式",
			Description = "输出详细日志到Player.log用于排查问题",
			DefaultValue = false,
		},
	},
	Visibility = 0,
	ChangeConfig = false,
	HasArchive = false,
	NeedRestartWhenSettingChanged = false,
	WorkshopCover = "Cover.jpg",
}

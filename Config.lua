return {
	Description = [[本模组是综合调整类,目标是增加难度以及合理调整.
适当(唯我:我觉得)增加难度,部分合理性(唯我:我觉得)调整会降低难度.

目前以下功能

1 只要武器能打得到就会换.目前武器换的非常快.
- 目前就先这样,改动很容易让电脑更多犯傻,等之后再研究怎么改.
2 降低亲密度增加速度.
- 哪来的社交达人..随随便便就无数个生死之交!这不合理!
3 降低读书历练获取.
- 为了鼓励和电脑切磋或者其他方式获取,只要读书历练根本用不光.
4 敌人一起放护体
- 只要太吾施展了护体,敌人就会尝试施展随机护体.
5 移动增加历练和随机一个轻功修习度
- 历练是1 10 100 1000. 修习是25%0-1 50%0-2 75%1-2 100%1-3 (暗渊练轻功很合理)
6 更保密的不传之秘
- 看不到电脑的保密功法
7 哄骗偷窃抢夺,根据聪颖显示数量
- 哪里把家如摆摊,任君挑选的,只有聪明的太吾才能知道的越多

建议订阅NPC加强一起享用
(设置NPC,2倍历练,2倍内力,优先读取内功)

后续计划(画饼,如果能实现的话)(因水平问题,随时可能放弃这些计划)
1 更保密的不传之秘,书籍等之后再解决.
2 敌人一起放护体,如果找到好的办法后,会改成针对性的放护体.
3 突破玩法改动,初步考虑突破按照天数来,失败了扣1天,可以一直突破.
4 研究编辑器中.

目前只在测试版中调试过.后续看我玩的情况再改进]],
	Cover = [[Cover.jpg]],
	Author = [[Magian]],
	FileId = 2880390461,
	Source = 1,
	Title = [[吾觉太易v0.0.6]],
	FrontendPlugins = 
	{
		[1] = [[IncreaseDifficultyFrontend.dll]]
	},
	BackendPlugins = 
	{
		[1] = [[IncreaseDifficultyBackend.dll]]
	},
	DefaultSettings = {
		[1] = {
            DisplayName = "快速换武器",
            Description = "只要武器能打得到就会换.目前武器换的非常快.",
			Key = "ChangeWeapony",
			SettingType = "Toggle",
			DefaultValue = true
        },
		[2] = {
            DisplayName = "一起放护体",
            Description = "只要太吾施展了护体,敌人就会尝试施展随机护体",
			Key = "TogetherDefendSkill",
			SettingType = "Toggle",
			DefaultValue = true
        },
		[3] = {
            DisplayName = "降低读书历练",
            Description = "降低读书历练的倍数,原来数值除以该数,2=原来的一半",
            Key = "ExpDivisor",
			SettingType = "Slider",
            DefaultValue = 10,
			MinValue = 2,
			MaxValue = 10,
			StepSize = 1
        },
		[4] = {
            DisplayName = "降低好感度",
            Description = "降低亲密度的倍数,原来数值除以该数,2=原来的一半",
            Key = "FavorabilityDivisor",
			SettingType = "Slider",
            DefaultValue = 10,
			MinValue = 2,
			MaxValue = 10,
			StepSize = 1
        }
	}
}
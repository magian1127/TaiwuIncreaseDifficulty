return {
	Description = [[本模组主要是围绕难度调整.
适当(唯我:我觉得) 增加难度,部分合理性(唯我:我觉得)调整会降低难度.

目前以下功能

增加难度(设置里可以调整)
1 只要武器能打得到就会换.目前武器换的非常快.
  因为是反编译看代码,策略部分看的脑子疼.如果再改的话,NPC可能会更多的犯傻.等之后再研究怎么改.
2 降低亲密度增加速度.
  哪来的社交达人..随随便便就无数个生死之交! 这不合理!
3 降低读书历练获取.
  为了鼓励和电脑切磋或者其他方式获取..不然只要读书历练根本用不光

降低难度
1 移动增加历练和随机一个轻功修习度
  历练是1 10 100 1000. 修习是25%0-1 50%0-2 75%1-2 100%1-3 (暗渊练轻功很合理)

建议订阅NPC加强一起享用
(设置NPC,2倍历练,2倍内力,优先读取内功)

后续计划(因水平问题,随时可能放弃这些计划)
1 自己放反伤 敌人就放同类型反伤  (画饼,如果能实现的话)
2 更保密的不传之秘,背包以及偷窃等,看不到不传之秘的书籍  (画饼,如果能实现的话)

目前只在测试版中调试过.后续看我玩的情况再改进]],
	Cover = [[Cover.jpg]],
	Author = [[Magian]],
	FileId = 2880390461,
	Source = 1,
	Title = [[吾觉太易v0.0.4]],
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
            DisplayName = "降低读书历练",
            Description = "降低读书历练的倍数,原来数值除以该数,2=原来的一半",
            Key = "ExpDivisor",
			SettingType = "Slider",
            DefaultValue = 10,
			MinValue = 2,
			MaxValue = 10,
			StepSize = 1
        },
		[3] = {
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
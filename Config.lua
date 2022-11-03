return {
	Description = [[本模组是综合调整类,目标是增加难度以及合理调整.

目前以下功能

1 只要武器能打得到就会换.
- 目前就先这样,改动很容易让电脑更多犯傻,等之后再研究怎么改.
2 调整送礼亲密度.
- 给最低级人物送礼不变,级别越高的人相对就越少.
3 降低读书历练获取.
- 为了鼓励和电脑切磋或者其他方式获取,只要读书历练根本用不光.
4 敌人一起放护体
- 只要太吾施展了护体,敌人就会尝试施展随机护体.
5 移动增加历练和随机一个轻功修习度
- 历练是1 1-10 10-100 100-1000. 修习是25%1-1 50%1-2 75%1-3 100%1-5 (暗渊练轻功很合理)
6 更保密的不传之秘
- 看不到电脑的保密功法和书籍,离开门派也会被没收.
7 哄骗偷窃抢夺,根据聪颖显示数量
- 哪里把家当如摆摊,任君挑选的,只有聪明的太吾才能知道的越多
8 过滤交换书籍
- 你没学过的技能且无法交换的不显示
9 突破按规律固定随机数种子
- 读档不会改变成功率和格子,需要做其他事改变种子,比如过月重修打架等.

建议订阅NPC加强一起享用
(设置NPC,2倍历练,2倍内力,优先读取内功)

后续计划(如果能实现的话)
1 敌人更有针对性的放护体.
2 突破改动,听说官方在改,那我就暂时不改突破了.
3 原来的制作改成村名造诣,太吾制作改成花费天数而不是等待
4 研究编辑器中.

目前只在测试版中调试过.后续看我玩的情况再改进]],
	Cover = [[Cover.jpg]],
	Author = [[Magian]],
	FileId = 2880390461,
	Source = 1,
	Title = [[吾觉太易v0.0.8]],
	FrontendPlugins = 
	{
		[1] = [[IncreaseDifficultyFrontend.dll]]
	},
	BackendPlugins = 
	{
		[1] = [[IncreaseDifficultyBackend.dll]]
	},
	TagList =
	{
		[1] = "Modifications",
		[2] = "Extensions",
		[3] = "Optimizations"
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
            DisplayName = "移动修练提醒",
            Description = "成功增加轻功修习度时会有通知,目前无法自动定义通知,所以是显示学会了新的",
			Key = "MoveNotification",
			SettingType = "Toggle",
			DefaultValue = false
        },
	}
}
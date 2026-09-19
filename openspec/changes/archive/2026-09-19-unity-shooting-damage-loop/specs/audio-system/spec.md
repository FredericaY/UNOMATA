## MODIFIED Requirements

### Requirement: SoundId 枚举预留所有音效类型

`SoundId` enum SHALL 保留 `Footstep`、`Land`、`GunShot`、`HitSurface`、`HitEnemy`、`UIClick`。脚步/落地与枪声、表面/敌人命中音 SHALL 实装；`UIClick` 保持预留。新增射击音效 SHALL 不改变既有枚举值的对应关系。

#### Scenario: 枚举值齐全
- **WHEN** 阅读 SoundId.cs 源码
- **THEN** 存在 `Footstep`、`Land`、`GunShot`、`HitSurface`、`HitEnemy`、`UIClick` 六个枚举值

#### Scenario: 射击相关音效可播放
- **WHEN** 有效射击或表面/敌人命中事实请求对应音效
- **THEN** SHALL 播放配置声音，枪声位于枪口，命中音位于实际命中点；同一事实不重复请求

## ADDED Requirements

### Requirement: 战斗声音有独立配置和有界播放

系统 SHALL 从项目配置接入实际枪声及表面/敌人命中音，复用音频请求链并由场景播放端出声。枪声 SHALL 使用已导入武器音效；音量和空间衰减可配置并响应总音量。连发播放 SHALL 使用有界并发/明确替换策略，不能误用脚步短片段筛选或与脚步/落地共用截断通道。

#### Scenario: 连发同时移动和落地
- **WHEN** 持续射击期间行走、跳跃并落地
- **THEN** SHALL 保持枪声节奏和空间位置，脚步/落地继续按其原有相位规则各自播放，不被枪声停止或重复叠加

#### Scenario: 总音量和并发上限
- **WHEN** 调整总音量至零/恢复，或持续开火超过单段声音长度并触及配置并发上限
- **THEN** 所有战斗音 SHALL 响应总音量，播放实例不无限增长，超限按明确策略处理，实际连发听感无明显削波或失控堆叠

#### Scenario: 缺失音效引用
- **WHEN** 必需战斗声音配置为空、缺失或无可播放片段
- **THEN** SHALL 给出一次具体缺项诊断，无空引用/每帧刷错；射击结算仍仅一次，配置未修好不通过视听验收

### Requirement: 战斗声音启停与场景隔离

战斗播放端 SHALL 成对订阅/退订并持有自身的有界播放资源；停用、销毁、失焦或场景退出 SHALL 停止并释放本上下文声音。重新启用 SHALL 不重放停用期间请求。原 Audio 对象的两个脚步/落地播放端及配置 SHALL 保留，新增战斗播放资源不能把运行辅助对象写回场景。

#### Scenario: 连发中失焦和重新启用
- **WHEN** 连发中失焦、停用/启用三次或退出运行
- **THEN** SHALL 无残留持续声音、重复回调或自动补播；恢复后的每次请求只有一次播放

#### Scenario: 保存重载正式场景
- **WHEN** 保存重载 SampleScene 并执行两次独立 Play/Stop
- **THEN** 脚步/落地与战斗引用 SHALL 完整，无缺失脚本、运行临时 AudioSource 残留或范围内未解释警告

## MODIFIED Requirements

### Requirement: Base Layer 动画素材切到 RifleGirl + FemaleRunnerAnimset 风格

角色 SHALL 保留 RifleGirl/FemaleRunnerAnimset 风格的基础站立、走跑与跳跃表现，并支持独立的瞄准方向运动。项目动画拓扑、混合参数和播放速率 SHALL 以实际方向、速度及过渡验收为依据，不再要求与供应商控制器完全一致。供应商源动画 SHALL 不被修改。

#### Scenario: Idle Walk Run BlendTree 三档 Motion 切换正确
- **WHEN** 角色在非瞄准状态从静止加速到行走、奔跑
- **THEN** SHALL 使用现有 Idle/Walk/Run 或其项目自有适配，按实际速度自然混合，保留站走跑三档能力，不因瞄准改造失效

#### Scenario: 瞄准八方向行走与纯向前奔跑
- **WHEN** 瞄准时执行八方向行走与纯向前奔跑
- **THEN** 腿部 SHALL 播放对应实际位移方向与速度的步态，不以普通向前步态横向滑动代替；上身和枪口满足 over-shoulder-aim

### Requirement: 跳跃链路三状态 Motion 切换正确

角色 SHALL 保留起跳、空中、落地的完整链路，复用项目裁定的连续空中动作与三类落地素材，允许为瞄准方向和动作衔接进行项目内适配。SHALL 不改变既定跳跃物理以迁就动画，也不要求固定状态数量或完全复制供应商拓扑。

#### Scenario: JumpStart / InAir / JumpLand 三状态 Motion 引用正确
- **WHEN** 原地、走步或奔跑时起跳并落地
- **THEN** SHALL 分别出现及时的起跳/空中/适当速度落地姿态，现有素材或可追溯的项目适配引用有效，不在物理落地后补播未完成起跳动作

#### Scenario: Base Layer 仅含 4 个 State 无 Fly
- **WHEN** 检查本次项目动画状态机的可达状态
- **THEN** SHALL 包含完整地面/空中/落地能力且不接入 Fly 行为；允许为瞄准运动增加必要状态，不以旧“四状态”限制阻止修复

#### Scenario: 状态机参数与拓扑无变化
- **WHEN** 对比修改前后的参数消费者与状态切换
- **THEN** 原 Speed/Jump/Grounded/FreeFall/MotionSpeed 行为 SHALL 保持兼容或成对迁移并回归；必要新增参数/拓扑必须与生产代码、音频及验证同步，无悬空引用

#### Scenario: 不引用 fbx 内 __preview__ 副本
- **WHEN** 检查项目动画引用
- **THEN** SHALL 只引用正式主 clip 或项目适配 clip，不引用 __preview__ 或仅存于编辑预览的临时片段

### Requirement: Play Mode 下 RifleGirl 风格基础动画正确播放

角色 SHALL 在非瞄准及瞄准的站立、行走、奔跑、起跳、空中、落地状态下正确播放对应动画，无 Avatar/绑定错误、严重变形或无接收器事件。瞄准动作的枪口及握把误差 SHALL 按 over-shoulder-aim 验收。

#### Scenario: 站立播 R_Idle
- **WHEN** 非瞄准且无移动输入
- **THEN** SHALL 播放基础站立动画；进入瞄准后平滑进入持枪姿态而非沿用错误的侧偏补偿

#### Scenario: WASD 移动播 R_Walk / R_Run
- **WHEN** 非瞄准按 WASD 并切换 Shift
- **THEN** SHALL 保持走跑和朝移动方向转身；瞄准时则使用与方向、速度匹配的瞄准步态

#### Scenario: 空格跳跃播完整跳跃链路
- **WHEN** 原地或按住任一方向，在非瞄准/瞄准下按空格
- **THEN** SHALL 完成起跳、空中、落地及恢复地面运动，瞄准枪口不会因持续方向输入发生动作相关偏差，按住空格跨落地不连跳

#### Scenario: Console 无红色错误与 Avatar 警告
- **WHEN** 独立运行覆盖完整动作矩阵并检查 Animator 绑定状态与 Console
- **THEN** SHALL 无未解释的角色动画错误/警告，包括无接收器、Avatar、绑定和重定向异常，已发现疑点具有修复或排除证据

### Requirement: PlayerArmature 提供 SwitchSocket 动画事件接收器

角色 SHALL 安全兼容现有素材携带的 SwitchSocket 事件，避免无接收器错误；本次固定步枪的实际挂点与手部姿态 SHALL 由项目瞄准方案统一管理。供应商事件 SHALL NOT 重新夺取武器姿态或开启第二套 IK。脚步/落地 SHALL 不依赖这些供应商事件重复出声。

#### Scenario: 接收器组件存在
- **WHEN** 检查交付角色使用的动画事件及兼容接收入口
- **THEN** 每个保留事件 SHALL 有有效接收或项目副本中的显式清理，正式动画播放无悬空回调

#### Scenario: SwitchSocket 方法被反射调用不抛错
- **WHEN** 站立、移动、AimJog 等素材触发已有挂点/IK 字符串事件
- **THEN** SHALL 无无接收器异常，且不会把枪重挂回旧右手父约束、覆盖新握把或造成跳变

### Requirement: JumpStart State Speed = 3.0 与其他 3 State 默认 1.0

角色动画播放速率 SHALL 与当前运动/物理节奏一致；原 JumpStart=3.0、其他=1.0 是历史起点，不再是不可更改的验收数值。调整 SHALL 限于项目资产并记录依据，不改变跳高、重力或走跑速度来使动作通过。

#### Scenario: JumpStart speed = 3.0
- **WHEN** 检查起跳播放速率与运行中的离地/落地时点
- **THEN** SHALL 使用经过复验的速率，避免起跳/落地卡顿；保留 3.0 或调整均必须有同一动作矩阵证据

#### Scenario: 其他 3 个 State Speed 均为 1.0
- **WHEN** 检查地面走跑、空中、落地及新增瞄准方向状态
- **THEN** 播放速率 SHALL 与各自实际节奏匹配，允许合理配置化调速，不强制全为 1.0，不产生明显滑步或加速失真

### Requirement: 5 条 transition 与 SA 原 controller 完全一致

项目过渡 SHALL 保障地面、起跳、空中和落地行为连续，允许根据实际素材时长、方向和瞄准状态修改过渡条件、时间及偏移；原五条 transition 与供应商逐字段一致的限制被替代。修改 SHALL 同步参数消费者和落地音检测。

#### Scenario: 全 5 条 transition 字段一致
- **WHEN** 覆盖地面到起跳/下落、起跳到空中、空中到落地、落地到地面的原有路径及瞄准变体
- **THEN** 每条路径 SHALL 正常到达，无滞留、迟发落地或重复事件；过渡差异有记录并经实际运行验证，而非依靠与 SA 数值一致判定通过

### Requirement: AnimatorAimBridge MoveX/Y 从 PlayerInputModel 读取

移动意图 SHALL 继续来自权威输入 Model，动画方向/速度 SHALL 从该意图驱动的实际运动状态获得并转换到身体局部坐标，不新增直接读取旧输入 API 的入口。SHALL NOT 把按键值或素材名称直接当作实际局部运动方向，撞墙停住、加减速和方向切换均应正确表现。

#### Scenario: 瞄准移动时 BlendTree 参数由 PlayerInputModel 驱动
- **WHEN** 瞄准时按 W+A，随后释放或被墙阻挡
- **THEN** 运动 SHALL 来自现有输入链，动画表现为真实局部斜向速度并随实际速度停止/减速，不强制数值为(-1,1)或指定 FL 文件

#### Scenario: AnimatorAimBridge 无 Input.GetAxis 调用
- **WHEN** 检查角色生产输入及动画更新路径
- **THEN** SHALL 无 Input.GetAxis/Input.GetButton 旧输入采集，没有独立于既有输入适配的第二路按键状态

### Requirement: AnimatorAimBridge 上半身 Yaw 补偿 + Pitch 叠加

角色 SHALL 通过有限且自然的躯干姿态配合武器和双手对准，不再要求手写脊椎世界旋转或固定三轴 bias。最终准确性 SHALL 以实际枪管衡量，不以胸骨/手骨方向衡量。下半身 SHALL 不参与视角 Pitch 整体倾倒；完整验收范围为当前相机 Euler pitch [-30°,70°]，替代旧文档未与场景一致的 ±50° 数字。

#### Scenario: 静止死区内上半身扭腰跟枪
- **WHEN** 稳定瞄准静止，相机相对身体水平偏转约 8°且未超过配置起转阈值
- **THEN** 脚 SHALL 保持方向，上身自然跟随，实际枪管满足误差门槛，不出现过度侧倾或依赖手骨 forward 的假对齐

#### Scenario: 上半身随相机俯仰、下半身不参与
- **WHEN** 在完整允许俯仰范围内上下转动相机
- **THEN** 上身与手臂 SHALL 协调跟随，下半身竖直，不翻转、拉伸或以缩窄允许视角规避问题

### Requirement: PlayerArmature.Animator 使用项目自有 Controller

正式场景角色 SHALL 使用项目自有的 `Assets/_Project/Animations/Player/Aiming/ShoulderAim.controller`，通过唯一受控动画图求值，不与供应商或旧瞄准控制器双重驱动。旧 UnomataPlayer.controller 保留为历史资产，不能作为当前角色生产引用。

#### Scenario: Animator Controller 字段已切换
- **WHEN** 检查保存重载后的 SampleScene 角色与表现组件
- **THEN** Controller 引用 SHALL 指向 ShoulderAim；运行时受控图使用同一资产，不依赖仅存于内存的临时引用

#### Scenario: B1a 已立的 Animator 契约不退化
- **WHEN** 检查角色 Avatar、旧 Geometry 和模型布料
- **THEN** SHALL 保留 Humanoid_FAvatar、禁用的旧 Geometry 与启用且有效的 MagicaCloth2

### Requirement: PlayerInput Behavior 改为 Invoke C# Events 并接线 PlayerController

正式角色 PlayerInput SHALL 使用项目 UnomataPlayer.inputactions、Player Map 与 Invoke CSharpEvents。业务输入经 PlayerController、Command、Model 和现有适配层到达唯一项目运动消费者；SHALL NOT 恢复供应商 SendMessages 或同时运行第二个运动控制器。

#### Scenario: PlayerInput Behavior 已切换
- **WHEN** 检查正式场景 PlayerInput
- **THEN** SHALL 为 Invoke CSharpEvents，项目动作资产与 Player Map 引用正确

#### Scenario: 移动输入经 QF 链路完整流通至 TPC
- **WHEN** Play Mode 下按 W 键
- **THEN** 业务输入 SHALL 经 PlayerInput → PlayerController → Command → PlayerInputModel → SAInputAdapter → 输入缓冲 → 项目 PlayerMotor 同帧消费；旧 TPC 已被项目适配替代，角色正常向前移动

## ADDED Requirements

### Requirement: 角色移动瞄准不依赖互相覆盖的姿态写入

角色 SHALL 在最终身体朝向确定后形成同帧一致的动画/武器姿态；同一根朝向、武器或手部结果 SHALL 不由旧新两套控制重复覆盖。瞄准时跑步和跳跃能力 SHALL 保留。

#### Scenario: 方向快速切换
- **WHEN** 持续瞄准并在 W/S/A/D 与斜向间切换、同时改变速度
- **THEN** SHALL 无先朝移动方向转身再被拉回导致的枪口跳变，无固定脚步面朝前而身体横滑，无重复位移或根旋转

#### Scenario: 保存重载后仍可复现交付状态
- **WHEN** 保存项目资产、退出运行、重新加载场景并独立启动
- **THEN** 当前模型/Avatar/布料、禁用的旧外观和新角色接线 SHALL 保持正确，无仅在内存有效的绑定或新增缺失引用


### Requirement: 非瞄准奔跑的武器跟随基础持枪姿态

非瞄准站立、走跑和跳落时，武器 SHALL 跟随当帧基础动画的持枪姿态，不被根空间固定位置拉入身体。进出瞄准 SHALL 连续过渡，不能回读已经受自身约束的手部造成循环或位置跳变。

#### Scenario: 非瞄准奔跑完整周期
- **WHEN** 松开瞄准并以当前跑速奔跑，覆盖完整动作周期和各转向
- **THEN** 武器 SHALL 保持合理握持，不穿入躯干中央，不与手臂脱节

#### Scenario: 奔跑中举枪和放下
- **WHEN** 保持奔跑并连续进入、退出瞄准
- **THEN** 武器和手臂 SHALL 从当前基础姿态平滑衔接，不瞬移到固定低持枪点

### Requirement: 瞄准步态连续混合并使用真实奔跑下半身

瞄准站立、走动、快跑和换向 SHALL 连续混合。仅纯向前允许举枪快跑，下半身 SHALL 复用当前普通奔跑动作，不能以仅加速瞄准走路冒充快跑；上半身与枪口仍满足原瞄准验收指标。

#### Scenario: 按下和释放移动
- **WHEN** 保持瞄准，从静止开始移动并停止，或反向/斜向切换
- **THEN** 动作幅度与实际速度 SHALL 平滑变化，不从零瞬间切成满幅步态，不因换向重开瞄准过渡豁免误差

#### Scenario: 举枪仅纯向前快跑
- **WHEN** 在瞄准状态按住 Shift 并切换八方向输入
- **THEN** 仅纯向前 SHALL 使用普通 Run 下半身与 5.335m/s 跑速；侧向、后退及四个斜向 SHALL 使用对应走路与 2m/s 走速，枪口/握把仍满足原精度门槛


### Requirement: 起跳腿部姿态与即时离地同步

跳跃动画 SHALL 在物理起跳同帧参与腿部姿态混合，并按上升/顶点/下落连续推进，避免身体明显升起后才从地面姿态收腿，以及空中切换相反起手片段导致重新蹬腿。SHALL 保持跳高、重力、即时输入与单次落地音，不以延迟物理或音频驱动跳跃掩盖时序。

#### Scenario: 原地与移动起跳
- **WHEN** 非瞄准/瞄准下原地、走步或允许的奔跑起跳
- **THEN** 首个离地帧 SHALL 已开始腿部动作，上升及顶点无重播起手，下降衔接落地且只有一次落地音

#### Scenario: 帧率与保存重载
- **WHEN** 在 30/60/120fps 与保存重载后复验同一跳跃
- **THEN** 姿态 SHALL 跟随相同物理阶段，跳高与输入消费不变，无持续按住自动连跳

# DECISIONS

## D-001 · Unity版本策略

- 状态：ACCEPTED
- 决定：采用现有工程已经固定的Unity `6000.5.1f1`，不得在未完成兼容性Spike和新决策前升级或降级。
- 理由：用户已用该版本初始化工程，`ProjectSettings/ProjectVersion.txt` 与本机Editor安装均可核验；T000不为追随原计划建议版本而迁移现有工程。
- 替代：若T110证明官方微信方案不兼容，比较更换补丁、切换已验证版本或最小embedded补丁；必须另写决策。

## D-002 · 配置唯一真相源

- 状态：ACCEPTED
- 决定：Excel为内容源，稳定JSON为构建快照，Runtime不读xlsx。
- 理由：可审查、可验证、适合Web和微信，避免Inspector双主库。

## D-003 · Unity对象引用

- 状态：ACCEPTED
- 决定：AssetRegistrySO只映射assetKey到Prefab、Sprite、Audio和VFX，不保存平衡数值。

## D-004 · 敌人架构

- 状态：ACCEPTED
- 决定：通用EnemyController、状态机和策略注册表，不为每个怪物建立空壳子类。

## D-005 · MVP平台能力

- 状态：ACCEPTED
- 决定：MVP只接存储、震动、生命周期和日志；广告、支付、登录、分享和排行榜不在范围内。

## D-006 · 横屏与参考坐标

- 状态：ACCEPTED
- 决定：横屏，1920×1080参考坐标；输入阈值按Safe Area缩放后的参考像素计算，不依赖Screen.dpi。

## D-007 · Unity工程目录

- 状态：ACCEPTED
- 决定：仓库根目录同时作为唯一Git根和Unity工程根，`Assets/`、`Packages/`、`ProjectSettings/` 不再放入 `game/` 子目录。
- 理由：当前目录已经是初始化完成的Unity 2D工程；避免移动资产产生额外GUID、路径和工具链风险。

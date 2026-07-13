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

## D-008 · T020 Unity包与渲染基线

- 状态：ACCEPTED
- 决定：Unity 6000.5.1f1使用URP 17.5.0、Input System 1.19.0、uGUI/TMP 2.5.0和Test Framework 1.7.0；Unity MCP固定commit `11836003a5e2ffcb7715ecec7e1fbb9d9cdb5bb8`。
- 决定：Graphics默认管线与Low/High质量档统一引用 `Assets/Settings/UniversalRP.asset`，其默认Renderer为 `Renderer2D.asset`。
- 理由：消除Graphics空管线与Git浮动依赖，确保Editor、测试和后续构建使用同一可复现基线。
- 细节：完整直接依赖、质量和输入测试入口见 `docs/PACKAGE_BASELINE.md`。

## D-009 · T110 微信转换 SDK 固定与 Unity 6000.5 补丁

- 状态：ACCEPTED
- 决定：采用微信官方 `wechat-miniprogram/minigame-tuanjie-transform-sdk` 的 `v0.1.33` 发布线，固定 commit `ed4ad28f433c6b52b5fd3f22a6fa155a0c98c228`，不使用浮动分支或已禁用的旧仓库。
- 决定：SDK由 Unity Package Manager embedded 到 `Packages/com.qq.weixin.minigame`。只允许 `WXRuntimeExtDef.cs` 在 `UNITY_6000_5_OR_NEWER` 使用 `GetEntityId` 的单点补丁；较早 Unity 保持上游实现。
- 理由：未修改上游在 Unity 6000.5.1f1 因 `GetInstanceID()` 的 CS0619 无法编译；替代 API 已通过 Unity 反射核验，补丁后全工程编译与回归通过。embedded 使补丁和完整上游快照可复现。
- 许可证：SDK根许可证为 MIT；随包保留 Brotli MIT-style 与 Binaryen 103.0.0 Apache-2.0 许可证。
- 移除条件：官方不可变版本修复该调用，并在 Unity 6000.5.1f1 通过 T110 同等编译与测试矩阵后，删除 embedded 包并恢复纯 Git 依赖。
- 限制：该决定不确认 G2转换、G3 DevTools、G4真机，也不授权迁移 Unity。

## D-010 · T120 可重复微信转换入口与Brotli策略

- 状态：ACCEPTED
- 决定：G2统一通过项目自有 `WechatBuildEntry` 与 `Tools/CI/build-wechat.sh` 调用固定SDK的 `WXConvertCore.DoExport`；输出限定在忽略目录 `Builds/WeChat/**`，Spike配置使用空AppID、横屏、256MB、关闭渲染线程和性能分析。
- 决定：macOS Unity `6000.5.1f1` 使用SDK公开的 `brotliMT=true` 路径。默认单线程路径因错误定位 `Unity.app/PlaybackEngines` 无法运行；这不是新增SDK源码差异。
- 决定：构建包装器在运行前备份、退出后恢复ProjectSettings、embedded SDK配置/元数据、URP构建期字段和SDK临时Assets，避免平台Spike污染可审查基线。
- 理由：第一次完整转换证明默认Brotli路径阻断G2；启用随SDK提供的压缩实现后，Builder、Converter、JSON、产物清单和 `.br` 均通过，并且仓库无平台设置残留。
- 限制：G2为 `PASS WITH KNOWN ISSUES`，不替代G3/G4；93条未匹配替换规则保持BUG-0006，只有实际DevTools和真机可以缩小风险。
- 移除条件：官方固定版本修复macOS路径并在同一Unity版本通过完整G2～G4后，可恢复默认压缩路径并删除兼容策略。

## D-011 · 平台阻塞期间优先推进主内容链

- 状态：ACCEPTED
- 决定：按用户明确指示，T120保持`BLOCKED`并保留现有G2证据，T130保持`BACKLOG`；暂不处理微信开发者工具、真机和打包问题，依赖T040且可独立执行的T200成为唯一`READY`任务。
- 理由：G3/G4需要本机之外的登录工具与设备条件，而P2配置系统到大部分玩法主链不依赖这些运行门；继续主内容可以产生有效进展，同时不伪造平台结论。
- 限制：这是执行顺序延期，不是范围裁剪。`MVP_SCOPE`中的微信四级验证仍必须完成，T120不得改为DONE，G3/G4不得改为PASS。
- 恢复条件：具备已登录微信开发者工具和至少一台可用手机后可恢复T120；无论设备何时到位，平台任务最迟在T640或T750开始前恢复并满足其依赖。

# T540 Verification

## 追溯

- 日期：2026-07-14
- 任务与范围：完成`lv_003_boss`的配置驱动混合前置波、镇墓玄甲王三阶段/处决、Victory/Defeat和失败后全新实例重试回路。
- 明确不做：不增加第二Boss，不制作正式过场，不实现T550结算/存档/重开入口、T600 HUD、T620表现、T630正式资源、T650教程UI，也不恢复T120/T130平台工作。
- 分支/提交：`main`；任务提交信息`T540: complete configured boss level`。
- 任务开始Git基线：见`baseline-commit.txt`与`baseline-status.txt`。
- Unity精确版本与执行环境：`6000.5.1f1`。主工程已有Editor进程但macOS登录会话锁定，无法安全使用UI/MCP且未强行结束用户进程；把当前`Assets/Packages/ProjectSettings`与测试需要的只读工作流文件复制到`artifacts/tmp/T540-unity-project`，用同一Unity版本隔离批处理编译和测试。
- 配置Schema/内容版本/hash：schema `4` / content `0.5.4-sample` / `9fbd5fa97b812cb965eff60104cbf16ef5f3699480298a4e8e96c566cfd717a0`；28表、692条记录。

## 改动审查

- 预计白名单：见`change-whitelist.md`。
- 实际改动：双工作簿及四个同源生成物；通用`BossLevelCoordinator/IBossLevelWorld`；T540 EditMode/PlayMode测试与受影响冻结断言；配置导出测试冻结值；T540证据和权威文档/索引。未改Schema、FieldDictionary、DTO、导出规则、场景、Prefab、AssetRegistry、Packages、ProjectSettings或微信SDK。
- 用户已有改动保护：开始基线工作树干净；没有覆盖、还原或暂存任务外用户文件。
- `git diff --check`：PASS，见提交前终审命令输出。
- 暂存白名单审查：PASS；暂存路径逐项落在预计或条件性白名单，条件性产品改动只包含不按内容ID分支的通用Levels协调器。

## 自动验证

- 工作簿：正式源与镜像字节一致，SHA-256 `71fc222d94e86057d47b57a47dccbf19186156529b1d096e56b703607b2f36c5`；29个Sheet全部渲染并经5张联系表视觉复核，公式错误0，见`workbook-after/`、`workbook-final/`和`workbook-all-sheets.log`。
- 静态/导出校验：严格配置校验PASS；JSON/hash/ConfigIds只读漂移门PASS；ConfigExporter构建0 warning/0 error；.NET测试56/56；Levels、EditMode和PlayMode Roslyn静态编译均0错误。受管JSON 181,598字节，27组344个ID常量，SHA记录见`generated-config-sha256.txt`。
- 专项EditMode XML：总数3 / 通过3 / 失败0 / `editmode-results.xml`。
- 专项PlayMode XML：总数2 / 通过2 / 失败0 / `playmode-results.xml`。
- 全量EditMode XML：总数162 / 通过162 / 失败0 / `editmode-full-results.xml`。
- 全量PlayMode XML：总数41 / 通过41 / 失败0 / `playmode-full-results.xml`。
- Console新增Error/Warning：T540代码与最终测试日志新增Error 0、编译错误0、崩溃0、未处理异常0；隔离工程首次导入记录的是既有微信SDK及Config异常类编译warning，没有T540所属文件warning，缓存后的最终全量日志warning 0。

## 玩家与平台证据

- 自动化玩家路径和可断言值：Bootstrap加载真实配置和AssetRegistry；实际生成11个前置敌人，覆盖火鱼、符蝠、石龟、骷髅幽魂和摄魂道傀，再生成唯一Boss；存活Boss的伪击败通知被拒绝。Boss按配置依次执行落石、封印波、冲撞，发布3次阶段事件与3次进入VFX，第三段提示含“处决”，实际死亡后在240秒上限内Victory；世界总出生12、活动池泄漏0。
- 失败/重试路径：玩家受到当前全部HP伤害后Defeat，阶段控制器被释放；结算后继续伤害旧Boss不再产生阶段事件。释放首局实体后用全新协调器和世界从头完成三阶段并Victory，阶段事件仍精确3次、池泄漏0。详细断言见`player-path.md`。
- 手工UI声明：NOT RUN。macOS会话锁定，未伪造Scene视图、截图或人工操作；本任务结论只覆盖隔离Unity批处理自动化原型路径，不外推最终视觉可读性。
- 标准Web：NOT RUN（沿用T100既有基线，本任务未构建）。
- 微信转换：NOT RUN（按用户决定继续延期平台工作）。
- DevTools：BLOCKED（沿用T120缺少微信开发者工具的阻碍，本任务未重试）。
- 真机：BLOCKED（沿用T120缺少DevTools与可用设备的阻碍，本任务未重试）。
- 截图/日志/产物：`workbook-after/`、`editmode-unity.log`、`playmode-unity.log`、`editmode-full-unity.log`、`playmode-full-unity.log`及对应四份NUnit XML。

## 结论

- 已知问题：微信SDK/DevTools/真机阻碍保持既有记录；T550结果、保存和面向玩家的重开入口不在本任务范围。主工程UI因锁屏未人工复核，但同版本隔离批处理的专项及全量测试均通过。
- 结论：PASS。T540验收已由配置审计、专项与全量Unity测试、失败后全新实例重试和零池泄漏证据覆盖。

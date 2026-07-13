# T520 Verification

## 追溯

- 日期：2026-07-14（Asia/Shanghai）。
- 任务与范围：完成`lv_001_tutorial`的6波事件驱动教学，覆盖普通斩、弱点、同笔三目标、切弹、架势切换与配置终极，并与T500/T510协调。
- 明确不做：T530/T540/T550、T600/T650正式UI、复杂剧情、精确书法、场景/Prefab/正式资源、T120/T130微信平台工作。
- 分支/提交：`main`；开始提交`d9a4f456010e7d723a9f9fd38b1b4104e21c1eb9`；目标提交`T520: complete event-driven tutorial level`。
- Unity精确版本与MCP实例：`6000.5.1f1`；`onedraw_2@272e911286835fad`；项目根与当前工作区一致。
- 配置：schema `4` / content `0.5.2-sample` / content hash `f666feb2c6a94439b0bdeee0c8f939b61728d04a04ec5ed62a23bdf100a98e92`。
- 工作簿：正式源与中文镜像字节一致，SHA-256均为`e4d6d382982c83fd7a094f929dfd469ca4fcde37098d13da4a6a74edebe1ff02`。

## 改动审查

- 预计白名单：见`change-whitelist.md`；基线工作树干净，无需合并或覆盖用户未提交改动。
- 配置闭环：29个Sheet保持原结构；Global与README内容版本同步；Waves/Spawns/Tutorials/Texts更新后导出28表668条、174,474字节JSON、27组320个ID常量。Schema、FieldDictionary、DTO和导出规则未变。
- 运行时：新增纯C#教程定义/序列/协调器；T500 Level/Wave只增加外部波次完成门；T510设置只额外公开配置终极手势。产品代码不含关卡、波次、敌人ID或教学数值。
- 测试/文档：新增T520 EditMode/PlayMode，旧测试只同步配置冻结值与教学关路径；TASKS、PROGRESS、project-index、CONFIG_SCHEMA、DECISIONS、TEST_PLAN同步。
- `git diff --check`：PASS；`ProjectSettings/EditorSettings.asset`测试副作用已通过Unity SerializedObject API恢复，最终无ProjectSettings差异。
- 暂存白名单审查：提交前执行并记录于`final-whitelist-review.txt`。

## 自动验证

- 工作簿：artifact-tool导入/修改/导出；29/29 Sheet均渲染，目标Sheet图片保留于`workbook-final-all/`；README/Global版本一致，公式错误扫描0，详见`workbook-final-verify.log`。
- 配置：`Tools/CI/verify-config.sh --skip-unity --results-root artifacts/tmp/T520`只读漂移门PASS；ConfigExporter build 0 warning/0 error；.NET 56/56；详见`config-verification-final.log`。
- ConfigPipeline Unity：EditMode 19/19/0（`config-pipeline-editmode.xml`）；PlayMode 3/3/0（`config-pipeline-playmode.xml`）。
- T520专项：EditMode 5/5/0（`editmode-specialty.xml`）；PlayMode 1/1/0（`playmode-specialty.xml`）。首轮PlayMode在`0.4f`/`0.4d`配置边界前提交而被正确拒绝，测试改为直接读取配置边界；失败证据保留为`playmode-specialty-attempt1.xml`。
- 受影响回归T500-T520：EditMode 21/21/0（`editmode-affected.xml`）；PlayMode 5/5/0（`playmode-affected.xml`）。
- 最终全量：EditMode 155/155/0（`editmode-full.xml`）；PlayMode 38/38/0（`playmode-full.xml`）。文档完成后又执行全量EditMode 155/155/0（job `fb89f063fe554a6fb7319378694f79f6`），最后完成项为工作流文档契约测试。
- Unity最终状态：idle、未编译、未Play；Console Error/Warning 0。

## 玩家与平台证据

- 玩家路径：见`player-path.md`。Bootstrap加载正式配置后，按6个实际动作推进6步；计时、错误/未来事件和命中数2不能越门；实际切换符架势；配置Circle终极扣除100能量、击败末波4目标并最终Victory。
- 配置可断言值：6波、6条出生行、15次出生、180秒上限；StepStarted 6、StepCompleted 6、TutorialCompleted 1；实际关卡用时低于配置上限。
- 标准Web：NOT RUN（T520不要求构建，既有T100证据不外推）。
- 微信转换：NOT RUN（按用户要求继续绕过T120/T130）。
- DevTools：BLOCKED（既有T120缺少开发者工具；非T520阻塞）。
- 真机：BLOCKED（既有T120缺少工具/设备；非T520阻塞）。

## 结论

- 已知限制：T520证明配置化原型玩家路径和事件门，不声称T600/T650正式HUD/教程遮罩、T630正式资源、最终新手可读性或真机约3分钟体验已经完成。
- 结论：PASS。T520原子任务完成，T530可转READY；本提交不包含T530实现。

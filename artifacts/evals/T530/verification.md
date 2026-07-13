# T530 Verification

## 追溯

- 日期：2026-07-14（Asia/Shanghai）。
- 任务与范围：把`lv_002_cave`完成为8波配置驱动混合怪普通关，覆盖六种非Boss原型、精英与危险目标组合、节奏递进及可解性。
- 明确不做：代码型敌人、T540 Boss整关、T550结算、T600 HUD、T620表现、T630正式资源、T650教程UI，以及T120/T130微信平台工作。
- 分支/提交：`main`；开始提交`7fb86e7cd60cbf10e34c1b85c9e3f3c159c0674a`；目标提交`T530: complete configured mixed-enemy level`。
- Unity精确版本与MCP实例：`6000.5.1f1`；`onedraw_2@272e911286835fad`。
- 配置：schema `4` / content `0.5.3-sample` / content hash `50ff7874f59f60092fe9eae71a8184c88828e78a6dd66cf04ea4abc066c611fe`。
- 工作簿：正式源与中文镜像字节一致，SHA-256均为`c97eda688c060b55cf49dbedab89246bd971f73d17f18cf58e9bf7b585401bdc`。

## 改动审查

- 预计白名单：见`change-whitelist.md`；基线工作树干净，无需合并或覆盖用户未提交改动。
- 配置闭环：29个Sheet保持原结构；Levels/Waves/Spawns与README/Global内容版本同步；导出28表689条、180,658字节JSON、27组341个ID常量。Schema、FieldDictionary、DTO和导出规则未变。
- 产品运行时：无修改。新增测试只验证T500/T510与既有T430/T450/T440运行时可完整消费新表内容；旧测试只同步配置冻结值。
- 文档：TASKS、PROGRESS、project-index、CONFIG_SCHEMA、DECISIONS、TEST_PLAN同步；T530置DONE，首个依赖满足任务T540置READY但未实现。
- `git diff --check`：PASS；测试产生的EditorSettings副作用已恢复，最终无ProjectSettings差异。
- 暂存白名单审查：提交前执行并记录于`final-whitelist-review.txt`。

## 自动验证

- 工作簿：29/29 Sheet重渲染并视觉复核，目标Sheet原图和8张contact sheet已归档；公式错误0，详见`workbook-verification.md`。
- 配置：`Tools/CI/verify-config.sh --skip-unity --results-root artifacts/tmp/T530-static-final`通过；ConfigExporter build 0 warning/0 error；.NET 56/56；三生成物漂移门通过，详见`config-verification-final.log`。
- T530专项：EditMode 4/4/0（`editmode-specialty.xml`）；PlayMode 1/1/0（`playmode-specialty.xml`）。初次EditMode精确比较二进制浮点失败，改为明确容差后通过，详见`unity-mcp-jobs.md`。
- 最终全量：EditMode 159/159/0（`editmode-full.xml`）；PlayMode 39/39/0（`playmode-full.xml`）。四份NUnit XML均由`Tools/CI/check-unity-test-results.py`判定PASS。
- Unity最终状态：强制Refresh与脚本编译完成、idle、未Play；Console Error 0。

## 玩家与平台证据

- 玩家路径：见`player-path.md`。正式配置实际出生并击败45怪，六种原型各执行首次攻击，三次精英修饰请求到达世界边界，八波完成后Victory且池零泄漏。
- 可断言值：8波、23条出生行、45个敌人、六种原型、3次精英修饰请求、8次波开始、8次波完成、1次关卡完成、210秒内Victory。
- 标准Web：NOT RUN（T530不要求构建，既有T100证据不外推）。
- 微信转换：NOT RUN（按用户要求继续绕过T120/T130）。
- DevTools：BLOCKED（既有T120缺少开发者工具；非T530阻塞）。
- 真机：BLOCKED（既有T120缺少工具/设备；非T530阻塞）。

## 结论

- 已知限制：精英修饰器已由关卡请求完整透传并测试，但既有T450 Actor池不会在T530范围内改写演员基础统计；正式视觉、HUD、教程表现与真机体验仍属后续任务。
- 结论：PASS。T530原子任务完成，T540可转READY；本提交不包含T540实现。

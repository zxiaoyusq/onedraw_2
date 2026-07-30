# T699 closeout

## 起始状态

- 分支：`main`
- 起始提交：`3e041a41c06cba251bd613ce9ad11aa116572668`
- 起始Git状态：`## main...origin/main`，工作树干净。
- 起始时用户已有改动：无。
- 任务执行过程中于2026-07-30 22:43出现4个未跟踪Unity资源：`Assets/_Game/Art/Enemies/Animated/FireFish/11.anim(.meta)`和`fire_fish_idle_001.controller(.meta)`。它们不在T699白名单，未由本任务创建、修改、删除或暂存。

## 最终白名单

- `docs/CONTENT_AUTHORING_GUIDE.md`
- `docs/TASKS.md`
- `docs/PROGRESS.md`
- `docs/BUGS.md`
- `project-index.yaml`
- `artifacts/evals/T699/closeout.md`

`docs/BUGS.md`是在核对Windows可执行命令时发现BUG-0009后新增的事实性收尾路径；没有扩大到修复Shell换行或修改产品产物。

## 产出

- 新增面向内容作者的单一手册，覆盖画笔、主角、静态/动画敌人、怪物死亡VFX的素材准备、配置、Unity导入/生成、Atlas、Registry、测试和目视验收。
- 明确T630通用工具必须先于专用动画和T698运行，防止`vfx_slash.prefab`被通用单图VFX覆盖。
- 区分同类型资源替换、Sprite升级Prefab和新增玩法敌人，避免把Prefab/Inspector变成第二数值库。
- 采用`unity-import-sprite-animations`的manifest预检、自然帧序、坐标转换、GUID保留和批次验收原则。
- 说明用户手动Unity菜单与Codex/Unity MCP调用同一作者入口的关系。
- 登记BUG-0009，并为Windows内容作者提供Unity随附.NET 8与Test Runner/MCP替代流程。

## 验证摘要

- 关键工程路径：19/19存在。
- 文档相对链接：6/6存在。
- Unity菜单：8/8与`MenuItem`或T698菜单常量一致。
- 测试分类：5/5存在（T690、T694、T695、T698、AssetImport）。
- 画笔配置字段：13/13存在于当前受管配置。
- 作者合同：确认T630跳过`/Animated/`专用VFX但会重建非动画VFX；确认T694/T695归档计划仍含历史绝对源路径。
- Unity随附.NET SDK：`8.0.318`；ConfigExporter CLI帮助可运行并列出`validate/export/generate/verify`。
- `bash Tools/CI/run-unity-tests.sh --help`：在当前Windows CRLF工作树按预期于解析阶段失败，已登记BUG-0009；不作为产品测试失败。
- Unity EditMode/PlayMode：`NOT RUN`。本任务只改Markdown/YAML索引和证据，不改代码、配置、Unity资源或运行时语义。

## 保留与边界

- 未修改`Design/Config/GameConfig.xlsx`、镜像、Schema、JSON/hash/ConfigIds。
- 未修改C#、场景、Prefab、动画、Controller、Atlas、Registry、`.meta`、ProjectSettings或Packages。
- 保留任务进行期间出现的4个未跟踪FireFish动画/Controller文件，T699提交不包含它们。
- 未运行T700或其他后续任务。

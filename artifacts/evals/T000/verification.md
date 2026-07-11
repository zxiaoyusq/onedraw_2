# T000 Verification

- 日期：2026-07-11
- Git分支：`main`（仓库首次提交，提交信息 `T000 establish project contracts`）
- 范围：统一玩法、MVP范围、Unity版本、工程目录、配置路径、任务状态和完成定义；未实现业务代码，未修改Unity序列化资产，未安装微信SDK。
- 开始基线：仓库已初始化但尚无提交；所有既有文件均为未跟踪文件；仓库内只有根目录一个 `.git`。
- Unity版本：`ProjectSettings/ProjectVersion.txt` 为 `6000.5.1f1 (0d9463e84828)`，本机同版本Editor已安装并正在打开当前工程。
- 工程布局：接受仓库根目录同时作为Git根和Unity工程根；配置生成路径统一为 `Assets/_Game/Config/Generated/gameplay_config.json`。
- 已安装模块：AndroidPlayer、WebGLSupport、MacStandaloneSupport、WindowsStandaloneSupport。
- Editor/MCP：Unity Editor与工程作用域MCP服务进程存在；当前Codex会话没有Unity MCP调用入口，未执行active instance、Console或Play Mode检查。这不是T000验收项，转入T010。
- 文档校验：权威文档中的Unity `6000.3 / UNPINNED` 与 `game/Assets` 冲突已消除；无未裁决TBD。
- 配置/结构校验：`project-index.yaml`、`tasks/task-index.json`、`tasks/task-index.csv`执行基础解析检查。
- EditMode：未执行；T000无代码或纯规则变更。
- PlayMode：未执行；T000不包含Unity冒烟，T010负责验证。
- 真实玩家路径：不适用；本轮不实现玩法。
- 人工前置：微信SDK、转换、开发者工具和真机均未验证，相关manual gate保持pending。
- 已知问题：根 `.gitignore` 尚未建立；Unity生成目录仍未跟踪；Unity MCP依赖仍引用浮动 `#main`。均未在T000越界修复。
- 结论：PASS。T000合同验收完成，T010置为READY；未开始T010。

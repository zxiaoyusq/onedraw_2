# T100 Verification

## 追溯

- 日期：2026-07-13。
- 任务与范围：构建并运行标准Unity WebGL，验证本地HTTP、Unity canvas输入、音频启动、中文UTF-8 interop与PlayerPrefs持久化，并记录体积/哈希/日志。
- 明确不做：不接微信SDK/转换，不运行DevTools或真机，不实现玩法，不宣称TMP中文字体通过，不把G1外推为G2-G4。
- 分支/提交：`main`；基线`812f411f T040: establish build and verification workflow`。
- 任务开始Git基线：见`baseline-commit.txt`与`baseline-status.txt`。
- Unity精确版本与MCP实例：`6000.5.1f1 (0d9463e84828)`；构建前MCP instance为`onedraw_2@272e911286835fad`，长构建后的重启未自动恢复，见BUG-0003。
- 配置Schema/内容版本/hash：schema 1 / content 0.1.0-sample；T100未修改或加载玩法配置。

## 改动审查

- 预计白名单：见`change-whitelist.md`。
- 实际改动：显式Smoke构建定义、Web运行时探针/JS bridge、Brotli HTTP服务器、合同测试、G1基线/BUG/证据文档，以及Unity自动迁移的`DefaultVolumeProfile.filter`字段。
- 用户已有改动保护：开始时工作树干净。构建自动改写的URP预过滤缓存、GlobalSettings runtime列表、batching条目和Unity Connect设置均通过临时Unity Editor序列化入口恢复；临时入口及meta已删除。
- `git diff --check`：PASS。
- 暂存白名单审查：PASS，只暂存T100文件、Unity生成meta、证据与相关文档；Builds和原始日志未暂存。

## 自动验证

- 静态/导出校验：Bash、Python、JS、asmdef JSON、YAML索引、HTTP headers、构建文件存在性/字节/SHA-256均通过。
- 专项EditMode：2/2 PASS；Smoke定义仅显式启用，探针文件与Brotli服务器合同存在。
- EditMode XML：8 / 8 / 0，`editmode-results.xml`。
- PlayMode XML：2 / 2 / 0，`playmode-results.xml`。
- 初次回归：7/8；AssemblyDependencyTests错误地把外部`Unity.InputSystem`引用并入项目依赖集合。修正为只比较`OneStrokeDemon.*`后最终全绿，循环检查保留。
- Unity错误：最终batch编译/测试日志无`error CS`、编译失败或测试失败；Editor.log过滤无脚本异常。MCP Console因BUG-0003不可用，未伪造该层结论。

## 玩家与平台证据

- 构建：`WEB_BUILD_PASS`；耗时`00:09:09.0607100`；总字节`12,433,772`；Brotli data/framework/wasm与index/loader齐全。
- HTTP：index、wasm、framework均HTTP 200；wasm MIME=`application/wasm`，`.br`响应包含`Content-Encoding: br`。
- 真实玩家路径：浏览器加载Unity canvas并进入MainMenu；点击后`input=pass`、`audio=pass`；中文显示`标准网页中文`；存储`pass run=1`，重载后`run=2`，第二次点击再次输入/音频PASS。
- 浏览器Console：Error 0；两类运行warning分别登记BUG-0001/BUG-0002。
- 标准Web：`PASS WITH KNOWN ISSUES`。
- 微信转换：`NOT RUN`。
- DevTools：`NOT RUN`。
- 真机：`NOT RUN`。
- 截图/日志/产物：`browser-smoke.jpg`、`browser-smoke.json`、`build-summary.log`、`http-summary.log`、`build-manifest.sha256`、测试XML与摘要、`warnings-summary.log`。

## 结论

- 已知问题：BUG-0001 WebGL EASU shader warning；BUG-0002 PlayerPrefs手动同步弃用warning；BUG-0003 MCP重连工具问题。
- 结论：PASS WITH KNOWN ISSUES。T100完成，T110置为READY；未开始T110。

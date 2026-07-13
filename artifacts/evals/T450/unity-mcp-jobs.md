# T450 Unity MCP Jobs

- Unity实例：`onedraw_2@272e911286835fad`
- Unity版本：`6000.5.1f1`
- Build Target：`WebGL`
- 测试方式：已连接Editor的Unity Test Runner；每轮完成后把Unity原生`TestResults.xml`归档到本任务，并用`Tools/CI/check-unity-test-results.py`重新解析。

## 最终专项

- EditMode category `T450`：job `162b85eff1a249768aefb4ee89240fcc`，3/3通过、0失败、0跳过；`editmode-results.xml`。
- PlayMode category `T450`：job `b5368f9ab431451690481f6cb75e6545`，1/1通过、0失败、0跳过；`playmode-results.xml`。

## 最终全量

- EditMode：job `68c92d1846c44aa38393c5a558d1e79f`，130/130通过、0失败、0跳过；`full-editmode-results.xml`。
- PlayMode：job `9a485ad5f7ff4e418d1fcb851f3cbffa`，32/32通过、0失败、0跳过；`full-playmode-results.xml`。

## 过程审计

- 首次请求只触发编译而未导入新脚本，job `a6e6fb28a15840c4864e71aaea15e997`返回0测试；该结果已拒绝，没有当作PASS。
- 强制Asset Refresh生成`.meta`后暴露一处NUnit版本不支持`Is.AnyOf`的编译错误；改为兼容约束后首次有效专项EditMode 3/3、PlayMode 1/1均通过。
- 全量PlayMode会临时把`ProjectSettings/EditorSettings.asset` Enter Play Mode选项从0改为1；已按干净基线恢复，未纳入任务diff。
- 清空预期负例日志并最终强制刷新/编译后，Console Error=0、Warning=0。

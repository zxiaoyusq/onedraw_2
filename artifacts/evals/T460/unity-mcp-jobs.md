# T460 Unity MCP Jobs

- Unity实例：`onedraw_2@272e911286835fad`
- Unity版本：`6000.5.1f1`
- Build Target：`WebGL`
- 测试方式：已连接Editor的Unity Test Runner；最终每轮Unity原生`TestResults.xml`均归档并通过`Tools/CI/check-unity-test-results.py`解析。

## 最终专项

- EditMode category `T460`：job `114ac3a46cd04c88a50d9fcf62cf6aca`，4/4通过、0失败、0跳过；`editmode-results.xml`。
- PlayMode category `T460`：job `715a11cfd5fb4f26baf8da6931aa40ab`，1/1通过、0失败、0跳过；`playmode-results.xml`。

## 最终全量

- EditMode：最终文档落盘后复跑job `130d28a6ff9349a4a6538cb2567d423f`，134/134通过、0失败、0跳过；`full-editmode-results.xml`。
- PlayMode：job `bf9cc852861e4e12ba5a865bda300de3`，33/33通过、0失败、0跳过；`full-playmode-results.xml`。

## 过程审计

- 首次刷新暴露`nameof`对子表达式的两处编译错误；改用DTO字段名后重新编译，Console 0 Error/Warning。
- EditMode过程job `0021f0bcfd104e3097a0c7e6123e78e6`和`acb83690e4c34b058b4a8cda41eed94d`只因JSON浮点进入C#后的测试容差过严失败；统一用足以覆盖float表示误差、远小于玩法阈值的容差后最终4/4。
- PlayMode过程job `399b8f0f5adb431db678096d41aea5e7`在表内`0.8f`前摇的精确双精度边界采样过早；把测试采样移到边界后`0.0001s`，产品逻辑和配置未修改，最终1/1。
- 首次全量EditMode job `10e5b07292354c45ad1a0083ff13905e`发现两条旧冻结记录数仍为660；同步受影响T230/T250断言为662后最终全量通过。
- 全量PlayMode临时把`ProjectSettings/EditorSettings.asset`的Enter Play Mode选项从0改为1；已按干净基线精确恢复，未纳入diff。
- 最终清空Console并强制刷新全部资产/编译后，新增Error=0、Warning=0。

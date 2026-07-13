# PACKAGE_BASELINE：T020 Unity包与质量基线

## 引擎与核心包

| 能力 | 包或程序集 | 固定版本 |
|---|---|---|
| Unity Editor | Unity | 6000.5.1f1 (0d9463e84828) |
| URP / URP 2D | com.unity.render-pipelines.universal | 17.5.0 |
| Input System | com.unity.inputsystem | 1.19.0 |
| uGUI / TextMeshPro | com.unity.ugui / Unity.TextMeshPro | 2.5.0 |
| Unity Test Framework | com.unity.test-framework | 1.7.0 |
| 2D Animation | com.unity.2d.animation | 15.1.0 |
| 2D PSD Importer | com.unity.2d.psdimporter | 14.0.3 |
| 2D SpriteShape | com.unity.2d.spriteshape | 15.0.3 |
| 2D Tilemap Extras | com.unity.2d.tilemap.extras | 8.0.3 |
| Runtime JSON | com.unity.nuget.newtonsoft-json / Newtonsoft.Json | 3.2.2 / 13.0.2 |
| Unity MCP | com.coplaydev.unity-mcp | commit 11836003a5e2ffcb7715ecec7e1fbb9d9cdb5bb8 |

`Packages/manifest.json` 是直接依赖声明，`Packages/packages-lock.json` 是包含传递依赖的机器可读解析结果。Git依赖禁止使用浮动分支。

T230把Unity官方 `com.unity.nuget.newtonsoft-json 3.2.2` 提升为Runtime直接依赖，而不依赖Unity MCP的传递引用。该包同步上游 Newtonsoft.Json `13.0.2`，包封装适用 Unity Companion License；包内 Newtonsoft.Json、Json.Net.Unity3D、Newtonsoft.Json-for-Unity 和 com.newtonsoft.json 第三方组件均记录为MIT。Runtime需要严格区分“属性缺失”和可空数值/布尔的 `null`，因此不使用无法表达该合同的 `JsonUtility`。

## 渲染基线

- Graphics默认管线：`Assets/Settings/UniversalRP.asset`。
- URP默认Renderer：`Assets/Settings/Renderer2D.asset`，类型为Renderer2DData。
- Low与High质量档均显式引用同一个URP Asset。
- 模板保留其他质量档；性能任务T730再根据设备证据调整参数，不在T020猜测最终预算。

## 输入与测试基线

- `ProjectSettings` 使用新Input System。
- `Assets/InputSystem_Actions.inputactions` 的UI/Point与UI/Click同时覆盖Mouse和Touchscreen。
- EditMode测试验证Graphics、Quality、Renderer2D、Input Actions、TMP和NUnit程序集。
- PlayMode测试使用Input System Test Framework注入Mouse与Touchscreen事件，验证两者可驱动同一个Pointer Action。

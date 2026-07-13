# 微信小游戏转换 SDK 上游记录

核验日期：2026-07-13

## 固定依赖

| 项 | 固定值 |
|---|---|
| SDK | 微信小游戏 Unity/团结引擎 SDK（WXSDK） |
| Unity Package ID | `com.qq.weixin.minigame` |
| 官方仓库 | `https://github.com/wechat-miniprogram/minigame-tuanjie-transform-sdk` |
| SDK 发布线 | `v0.1.33` |
| 固定 commit | `ed4ad28f433c6b52b5fd3f22a6fa155a0c98c228` |
| commit 日期 | 2026-06-22 14:48:10 +0800 |
| Git tree | `22b3686bdf558fa34c7d204c381281c721fe0b70` |
| 许可证 | MIT，Copyright (c) 2021 wechat-miniprogram |
| 安装形态 | manifest 保留完整 commit；Package Manager embedded 快照 |
| 本地补丁 | `Runtime/WXRuntimeExtDef.cs` 单点 Unity 6000.5 条件补丁 |

`Packages/manifest.json` 保留完整上游 commit，不引用会移动的 `main` 或发布分支；`Packages/packages-lock.json` 的实际解析源为 `embedded`：

```text
https://github.com/wechat-miniprogram/minigame-tuanjie-transform-sdk.git#ed4ad28f433c6b52b5fd3f22a6fa155a0c98c228
```

## 官方性与版本判定

- 仓库位于微信官方 `wechat-miniprogram` GitHub 组织；仓库 README 将其命名为“微信小游戏Unity/团结引擎SDK”，并同时说明 Unity 与团结引擎可经 Package Manager 安装。
- 微信官方的 `minigame-unity-wechat-preview` 仓库也把该 Git URL列为 WXSDK 前置依赖。
- 旧仓库 `wechat-miniprogram/minigame-unity-webgl-transform` 仍被 GitHub 禁用，只把其文档站作为上游说明入口，不作为依赖源。
- 固定 commit 同时位于上游 `main` 与 `v0.1.33` 分支；上游没有对应的不可变 `v0.1.33` tag，因此必须固定 commit。
- `CHANGELOG.md` 将该版本标为 `2026-6-22 v0.1.33`，`WXPluginVersion.pluginVersion` 为 `202606220647`。
- 上游 `package.json` 的 `version` 仍为 `0.1.1`，与发布线不一致。这是上游元数据缺陷；本项目以 commit、CHANGELOG 和插件构建版本三者共同追溯，不把 `0.1.1`误写为实际 SDK 发布线。

## 完整性记录

| 文件或集合 | SHA-256 |
|---|---|
| `LICENSE` | `9223170188b2dfafcafc2b89d96da21ffa8396fd97485671fc579756999aeb21` |
| `package.json` | `4ccaa7aadc4495a286df7cb57b2c2734803fdef6b65d91cf029466e5ff0873fe` |
| `CHANGELOG.md` | `eed643c82858380ace863ad67c5f3e60f9747ccebbb2a84571aee9673c6c8648` |
| 固定 commit 全文件 SHA-256 清单 | `bcad271229e1fbd671e87471e9af9a22ade700a0aeec35d1480f767a000551f8` |
| embedded 补丁后全文件清单 | `ec78b208e3c759b766d78b85a2719a66aadbd43aa6725c6d6dba45c429eabf44` |
| embedded `WXRuntimeExtDef.cs` | `0732d2c95f47bf6f9fa6fbd59846f1092cdac0f4c374c1e533d6b8822d21bf6e` |
| Brotli `LICENSE.txt`（MIT-style） | `3d180008e36922a4e8daec11c34c7af264fed5962d07924aea928c38e8663c94` |
| Binaryen 103.0.0 `LICENSE`（Apache-2.0） | `c5accbbd8546e94c34aed24afe689a617627d18eed5a6c48277e48db57c23851` |

原始查询和 Unity 导入结果见 `artifacts/evals/T110/`。`Library/PackageCache` 与独立上游 clone 不进入版本控制；embedded 包进入版本控制以保证补丁可重放。Unity embed 自动格式化 `package.json`、加入上游 `_fingerprint`，并不复制上游 `.gitignore`；这些不是人工 SDK 逻辑改动。

## Unity 6000.5 最小补丁

未修改的官方 commit 在 Unity 6000.5.1f1 编译失败：

```text
WXRuntimeExtDef.cs(135,28): error CS0619: 'Object.GetInstanceID()' is obsolete: 'GetInstanceID is deprecated. Use GetEntityId instead.'
```

上游程序集未生成后，Burst 另报告无法解析 `WxEditor`；该条是级联错误。原始条目保存在 `artifacts/evals/T110/sdk-import-original-errors.log`。

补丁仅在 `UNITY_6000_5_OR_NEWER` 使用 Unity 6000.5 提供的 `Object.GetEntityId()` 和 `EntityId.ToULong()`；早期 Unity 继续执行上游 `GetInstanceID()`。补丁后全工程编译、EditMode 10/10、PlayMode 2/2 通过，Console Error/Exception 为0。包内 `UPSTREAM.md`带有相同来源与移除条件。

## 升级与补丁策略

1. 升级前重新核验官方仓库、发布分支、commit、许可证和变更记录。
2. 先在独立任务中替换为新的完整 commit，再执行全工程编译、EditMode、PlayMode、G2 转换与后续平台门。
3. 当前补丁决策为 `EMBEDDED_MINIMAL_PATCH`。只允许已记录的 `GetInstanceID` 条件分支；发现其他差异必须停止并审查。
4. 官方不可变版本修复该错误并通过同一矩阵后，删除 embedded 副本并恢复纯 Git 解析；这是补丁移除条件。
5. 不通过切换 Unity 版本绕过 SDK 问题；任何引擎迁移必须新增决策记录。

# ConfigExporter third-party packages

版本以同目录及 `Tests/` 下的 `packages.lock.json` 为准。所有直接 `PackageReference` 同时使用精确版本约束；这些包只服务于独立 `.NET 8` 工具和测试，不属于 Unity Runtime 依赖。

## Runtime tool dependencies

| Package | Version | License | Upstream |
|---|---:|---|---|
| DocumentFormat.OpenXml | 3.5.1 | MIT | https://github.com/dotnet/Open-XML-SDK |
| DocumentFormat.OpenXml.Framework | 3.5.1 | MIT | https://github.com/dotnet/Open-XML-SDK |
| System.IO.Packaging | 8.0.1 | MIT | https://github.com/dotnet/runtime |

## Test-only dependencies

| Package family / package | Version | License | Upstream |
|---|---:|---|---|
| Microsoft.NET.Test.Sdk | 17.14.1 | MIT | https://github.com/microsoft/vstest |
| Microsoft.CodeCoverage | 17.14.1 | MIT | https://github.com/microsoft/vstest |
| Microsoft.TestPlatform.ObjectModel | 17.14.1 | MIT | https://github.com/microsoft/vstest |
| Microsoft.TestPlatform.TestHost | 17.14.1 | MIT | https://github.com/microsoft/vstest |
| Newtonsoft.Json | 13.0.3 | MIT | https://github.com/JamesNK/Newtonsoft.Json |
| System.Collections.Immutable | 8.0.0 | MIT | https://github.com/dotnet/runtime |
| System.Reflection.Metadata | 8.0.0 | MIT | https://github.com/dotnet/runtime |
| xunit | 2.9.3 | Apache-2.0 | https://github.com/xunit/xunit |
| xunit.assert | 2.9.3 | Apache-2.0 | https://github.com/xunit/xunit |
| xunit.core | 2.9.3 | Apache-2.0 | https://github.com/xunit/xunit |
| xunit.extensibility.core | 2.9.3 | Apache-2.0 | https://github.com/xunit/xunit |
| xunit.extensibility.execution | 2.9.3 | Apache-2.0 | https://github.com/xunit/xunit |
| xunit.abstractions | 2.0.3 | Apache-2.0 | https://github.com/xunit/xunit |
| xunit.analyzers | 1.18.0 | Apache-2.0 | https://github.com/xunit/xunit.analyzers |
| xunit.runner.visualstudio | 3.1.5 | Apache-2.0 | https://github.com/xunit/visualstudio.xunit |

许可证表达式与上游地址取自还原后的 NuGet `.nuspec` 元数据；完整许可条款以各包链接的上游仓库及 NuGet 包内容为准。

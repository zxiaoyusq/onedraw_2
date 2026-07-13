# T530 Unity MCP Jobs

- Unity：`6000.5.1f1`。
- MCP实例：`onedraw_2@272e911286835fad`。
- T530 EditMode最终专项：job `a364e177a5ce41d28e1cac0297ace763`，4/4通过，XML为`editmode-specialty.xml`。
- T530 PlayMode最终专项：job `25c5abd69b044843919b5cbe8c11e45c`，1/1通过，XML为`playmode-specialty.xml`。
- 文档与索引完成后的最终全量EditMode：job `75a979551e5340a58a75ec4e0a034a56`，159/159通过，最后完成项为工作流文档合同测试，XML为`editmode-full.xml`。
- 最终全量PlayMode：job `12368be4db7446d1ba336bea23e48d12`，39/39通过，XML为`playmode-full.xml`。
- 首次T530 EditMode专项使用二进制浮点值精确比较`0.4d`，配置重载结果为`0.40000000596046448`而失败；断言改为明确的`0.000001d`容差后通过，不改变产品实现或配置。
- 最终强制Refresh与脚本编译后Editor为idle；Console Error查询返回0条。Unity测试临时把`ProjectSettings/EditorSettings.asset`的Enter Play Mode选项改为1，已恢复基线0，最终无ProjectSettings差异。

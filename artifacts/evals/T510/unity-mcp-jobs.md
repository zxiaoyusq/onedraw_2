# T510 Unity MCP Jobs

- Unity实例：`onedraw_2@272e911286835fad`，Unity `6000.5.1f1`，WebGL，非batch Editor。
- 首轮EditMode诊断：`44401392be984369a685015229c801c2`，8项中6项通过；两项只暴露配置float转double尾差和测试断言缺少容差，已修复并重跑。
- 最终专项EditMode：`0f4c0607932c448599c5317a4b4d80f7`，8/8通过；原生NUnit XML为`editmode-results.xml`。
- 最终专项PlayMode：`37ddfdde2db34d31807d49b40fa0ccb5`，2/2通过；原生NUnit XML为`playmode-results.xml`。
- 最终全量EditMode：`506046afea6b4f94aaa0b624090b52a7`，150/150通过；原生NUnit XML为`full-editmode-results.xml`。
- 最终全量PlayMode：`0d644b37ce7d4a40a2827a3437b44a78`，37/37通过；原生NUnit XML为`full-playmode-results.xml`。
- 前台Editor占用工程，批处理入口被Unity正常拒绝；测试由同一Unity Test Runner API经MCP执行，Test Runner生成的原生`TestResults.xml`在每轮结束后归档到上述证据路径，并由`Tools/CI/check-unity-test-results.py`复核。

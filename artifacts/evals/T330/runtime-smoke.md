# T330 Runtime Smoke

- Unity：6000.5.1f1，实例`onedraw_2@272e911286835fad`，活动场景`Assets/_Game/Scenes/Bootstrap.unity`。
- 玩家路径：真实Mouse按下→水平移动→抬起，经`InputSystemPointerAdapter`、`StrokeInputCollector`、`StrokeGeometry.Process`、`GestureClassifier`得到唯一结果。
- 断言：`strokeId=1`、`ruleId=stroke_horizontal`、类型Horizontal、长度90～100参考像素、无向角0度、归一化曲率0、置信度0.5～1、指针抬起后不再活动。
- Runtime规则：采样和几何使用真实`stroke_any`配置，分类器使用配置服务暴露的全部七条只读StrokeRules；该路径长度低于Charged的100参考像素最小长度，因此不依赖机器帧时长规避误判。
- Bootstrap日志：schema 1 / content 0.1.1-sample / 28表645条、Registry 76键、Mouse/Touch参考空间1920×1080。
- 脚本Refresh并完成域重载后的独立Console检查：Error 0、Warning 0。T330详细玩家路径无业务Error/Warning；Test Runner基础设施消息单列于`regression-notes.md`。

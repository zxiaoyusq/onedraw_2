# T530 Workbook Verification

- 正式源`Design/Config/GameConfig.xlsx`、中文镜像和导出中间件在格式化后字节一致；正式源与镜像SHA-256均为`c97eda688c060b55cf49dbedab89246bd971f73d17f18cf58e9bf7b585401bdc`。
- 工作簿仍为29个Sheet；全量重渲染由8张contact sheet覆盖，README、Levels、Waves、Spawns另保留原尺寸目标截图。
- 目标表复核值：`lv_002_cave`时限210秒、星级6500/9500/13000；8波；23条出生行；45个敌人；第5/7/8波使用`modifier_elite`。
- 项目总计16波、32条出生行、67个敌人实例；README/Global内容版本均为`0.5.3-sample`。
- 公式错误搜索0；Levels/Waves/Spawns受影响秒数字段统一使用`0.00`格式；`workbook-final/inspect.log`记录最终29/29 Sheet检查。
- 构建与格式化记录见`workbook-build.log`、`workbook-format.log`；修改前截图见`workbook-before/`，最终视觉证据见`workbook-final/`。

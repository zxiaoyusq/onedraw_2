# T500 Unity MCP Jobs

- Unity实例：`onedraw_2@272e911286835fad`
- Unity版本：`6000.5.1f1`
- Build Target：`WebGL`
- 测试方式：已连接Editor的Unity Test Runner；最终四轮Unity原生`TestResults.xml`均归档，并由`Tools/CI/check-unity-test-results.py`解析为PASS。

## 最终专项

- EditMode category `T500`：最终规则审计后复跑job `ea1e264695bf4b5e9ae4f5357acdc5fc`，8/8通过、0失败、0跳过；`editmode-results.xml`。
- PlayMode category `T500`：最终规则审计后复跑job `a2d4e5306cdf42468908f5d8e13b9b5f`，2/2通过、0失败、0跳过；`playmode-results.xml`。

## 最终全量

- EditMode：最终规则与文档落盘后复跑job `bf7b4fa38b6d4d3b84b1d3acde08bbe1`，142/142通过、0失败、0跳过；`full-editmode-results.xml`。
- PlayMode：最终规则落盘后复跑job `f105ff4289b44150a95e32eededbee7d`，35/35通过、0失败、0跳过；`full-playmode-results.xml`。

## 过程审计

- 首次刷新发现T500 PlayMode测试缺少Core命名空间，导致`SceneNames`三处编译错误；补齐程序集已存在的Core using后刷新编译Error=0。
- 首轮EditMode job `3f80f39a217a4a2e881a693f0143fcb8`为5/8：表内`float 0.2`进入double后略大于精确字面量，三个到点边界晚一帧。时间比较统一加入`0.000001s`表示容差后，过程job `7795a0a8a9704025b4755d5d1435b327`及最终专项均8/8；玩家动作仍必须收到显式确认，容差不代替动作门。
- 过程PlayMode job `13719fbb1f5b45bfbbc72e9d86963344`已2/2；事件时间戳进一步改为实际关卡时刻后重跑得到最终job。
- PlayMode测试把`ProjectSettings/EditorSettings.asset`的Enter Play Mode选项从0临时改为1；已精确恢复干净基线，未纳入diff。

# T697 收尾

## 范围

- 基线中的用户改动：`Design/Config/~$GameConfig.xlsx`为删除状态，保持原状且不纳入提交。
- 白名单：死亡反馈运行时代码、对应PlayMode回归、T697证据、`docs/TASKS.md`与`docs/PROGRESS.md`。
- 未修改配置表、受管配置生成物、Registry、死亡动画资源、Prefab、场景或ProjectSettings。

## 根因与修复

生产`Reference Pixel World`对XY使用参考像素缩放、对Z保持1。旧实现使用Sprite世界边界XYZ中的最大值计算特效缩放，Z厚度因此占主导，把配置为96参考像素的死亡特效压缩到约10px。修复后只使用Sprite二维XY边界；正常致死事件、死亡位置快照、敌人回收和池化复用流程不变。

生产路径测试新增真实Battle相机像素尺寸断言，防止均匀缩放的测试夹具再次掩盖同类问题。

## 验证

- T695定向PlayMode：2/2通过。
- 全量PlayMode：55/55通过，耗时42.30秒。
- 真实Bootstrap→MainMenu→Battle相机：`enemy_death_004`测得最大屏幕边长86.31px，符合当前Game相机相对96参考像素配置的换算结果。
- 最终恢复`Assets/_Game/Scenes/Bootstrap.unity`，清理测试预期日志后Console Error/Warning为0。
- 首次定向运行在Unity Test Framework初始化阶段抛出`PlayModeRunTask`空引用，未进入任何产品测试；清理框架卡住任务并恢复Bootstrap后，相同定向测试2/2和全量测试55/55通过，该无效运行不计入产品结论。

## 视觉证据

- `enemy-death-battle-fixed.png`：真实Battle背景与相机下，白色烟圈死亡特效清晰可见。

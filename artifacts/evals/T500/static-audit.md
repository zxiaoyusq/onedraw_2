# T500 Static Audit

- `Assets/_Game/Scripts/Levels`新增规则不依赖`MonoBehaviour`、`UnityEngine.Time`、`Task.Run`、托管线程或xlsx解析；Unity 6000.5.1f1刷新编译通过。
- 产品Levels代码没有关卡、波次、敌人、出生点或修饰器内容ID列表；内容只通过`IConfigProvider`和不可变定义进入运行时。代码中的枚举解析、稳定排序、确定性采样与时间容差均为规则策略，不是第二数值库。
- 13条Spawn按`spawnTime + index × interval`展开为35次出生；同到期时刻使用spawnId Ordinal与行内序号裁决。世界拒绝前不提交游标，maxAlive容量释放后重试。
- `PlayerConfirmed`只由显式`ConfirmPlayerAction`消费当前门；暂停时确认和delta均无效。LevelRunner不自行读取Unity全局时间，也不裁决T510 Victory/Defeat。
- `git diff --check`通过；ProjectSettings测试临时差异已恢复。没有修改`.unity`、`.prefab`、Registry、Input Actions、Packages、ProjectSettings、微信SDK或Builds。

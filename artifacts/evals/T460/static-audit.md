# T460 Static Audit

- 产品Boss阶段源码中搜索当前样例阈值`0.67/0.34`、阶段护甲`120/60`、速度`20/32/48`、阶段ID及二/三阶段移动ID：0命中；这些值只存在配置、生成物、测试和证据。
- `BossPhaseDefinition`与`BossPhaseController`中搜索Animator/Animation/normalizedTime、`Task.Run`、线程、微信SDK静态API：0命中。
- 阶段纯状态机不依赖`MonoBehaviour`；Boss控制器通过`IConfigProvider`、`IEnemyAttackWorld`和`ISkillEffectWorld`组合现有端口，不直接调用平台SDK。
- 阶段切换由HP事件和配置阈值驱动，状态切到`Move`后重建策略；动画长度不是规则真相。
- `git diff --check`：PASS。

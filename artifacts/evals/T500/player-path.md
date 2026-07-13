# T500 Player Path

- 环境：Unity `6000.5.1f1`，WebGL Build Target；PlayMode从Bootstrap真实加载受管配置并等待MainMenu，随后创建T500世界端口的GameObject实体。
- 教学关`lv_001_tutorial`：暂停后推进60秒，关卡时钟仍为0、状态仍为Ready、出生数为0；恢复后按表运行3波，共生成3+4+3=10个实体，逐波回报AllEnemiesDefeated后各按配置结束延迟推进，最终恰好一次LevelCompleted。
- 教学关事实：WaveStarted 3次、EnemySpawned 10次、WaveCompleted 3次、LevelCompleted 1次；每个实体位置均落在归一化`[0,1] × [0,1]`，朝向来自SpawnPoint请求，结束后活动实体为0。
- Boss关`lv_003_boss`：第一波按表生成3个符火鱼妖和3个飞行符蝠，全部击败并经过配置延迟后进入第二波；第二波只生成`boss_tomb_king` 1个，普通前置敌人死亡不能结束Boss波，回报该Boss实体死亡后关卡完成。总出生6+1=7。
- 边界说明：T500验证的是Bootstrap真实配置、纯时间轴和显式世界回执；具体T450敌人池、玩家战斗、清场、暂停/失焦UI和胜负互斥仍由T510及具体关卡任务接线，不能把本证据外推为完整可玩关卡。

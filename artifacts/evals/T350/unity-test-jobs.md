# T350 Unity Test Evidence

- 脚本标准验证：9个新增C#各0 warning / 0 error；Unity导入、域重载和Console Error/Warning均为0。
- 专项EditMode：6/6 PASS，job `2fbbc3cc958a41489fa0f5f76108200c`。
- 专项PlayMode：2/2 PASS，job `8f7c8a8acbcf4133b54f2f9348aae0bb`。
- 状态同步前全量EditMode：78/78 PASS，job `7b0fc76dc3364daba9ae7ce7d74590ce`。
- 状态同步前全量PlayMode：22/22 PASS，job `0c3069940be64777baf3d51598ec4417`。
- 状态同步后最终全量EditMode：78/78 PASS，job `3ef2b6b309864938af5a7d84bf18f28e`。
- 状态同步后最终全量PlayMode：22/22 PASS，job `dfec745528b249fcb6ceee0be33280fb`。

专项覆盖真实配置半径/容量、路径排序、同笔跨段去重、主体/弱点聚合、禁用目标、稳定同距裁决、无匹配与规则失配、确定性回放、共享视觉点集、真实Mouse多目标玩家路径，以及纯解析和真实Physics2D热路径0 B托管分配。

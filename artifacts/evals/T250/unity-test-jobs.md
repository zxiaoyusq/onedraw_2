# T250 Unity Test Evidence

- T250专项EditMode：2/2 PASS，job `c7ddd97a8b2e49068adfbceba61e6dde`。
- ConfigPipeline EditMode：19/19 PASS，job `3dc28d2da27045e2984dd36ee580c0ae`。
- ConfigPipeline PlayMode：3/3 PASS，job `e32dc9d5e2294636a329478c64b57f97`。
- 全量EditMode：32/32 PASS，job `12e3c8fbeabe4c3ba57bff6cd01a9234`。
- 全量PlayMode：5/5 PASS，job `19f966940db94363aab03f061ad0bb81`。
- 隔离工程默认一键入口：EditMode 19/19、PlayMode 3/3，最终`CONFIG_PIPELINE_PASS`。

专项断言覆盖生成元数据与Runtime JSON/hash一致、脚本程序集归属、27个集合/306个ID常量的稳定性与唯一性，以及Player/Level/Enemy/Skill/Asset代表性类型化查询和76键Registry一致性。

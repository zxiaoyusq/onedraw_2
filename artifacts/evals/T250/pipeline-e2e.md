# T250 Config Pipeline E2E

## 默认完整入口

在隔离的当前工作树副本执行默认`Tools/CI/verify-config.sh`，避免与已打开的GUI Unity争用同一工程锁。最终结果：

```text
TEST_RESULTS result=Passed total=19 passed=19 failed=0 skipped=0 .../editmode-results.xml
TEST_RESULTS result=Passed total=3 passed=3 failed=0 skipped=0 .../playmode-results.xml
CONFIG_PIPELINE_PASS dotnet=PASS drift=PASS editmode=PASS playmode=PASS
```

同一命令在进入Unity前已完成导出器构建、临时三生成物生成、受管生成物只读校验、逐字节diff以及.NET 54/54。

## 漂移故障注入

1. 用`apply_patch`在受管hash旁车临时加入`-drift`。
2. 执行`Tools/CI/verify-config.sh --skip-unity`。
3. 结果为退出码3、`CFG013`，并输出期望值与实际值的统一diff；命令没有重写受管文件。
4. 执行显式`--update --skip-unity`恢复工具生成内容；随后三生成物diff和.NET 54/54重新PASS。

`--skip-unity`明确只输出`CONFIG_PIPELINE_PARTIAL`，不能作为完整任务通过证据。

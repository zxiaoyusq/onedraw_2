# WORKFLOW：原子任务、测试、构建与证据

## 1. 一个任务一个提交

每次只执行`docs/TASKS.md`中第一个依赖全部为`DONE`的`READY`任务。开始时记录Git状态和基线提交，先写预计改动白名单；完成后只暂存白名单内文件，审查后创建一个以任务ID开头的可回滚提交。不得在同一提交顺手开始下一任务。

推荐从仓库根目录执行：

```bash
Tools/CI/new-task-evidence.sh T100
```

命令会创建`artifacts/evals/T100/verification.md`、`change-whitelist.md`、`baseline-status.txt`和`baseline-commit.txt`，已有证据目录不会被覆盖。创建后必须先填写白名单再修改工程。

## 2. Unity批处理测试

Unity Editor不能同时以图形界面和批处理方式打开同一个工程。运行下列命令前先正常关闭该工程的Editor实例；脚本从`ProjectSettings/ProjectVersion.txt`解析精确版本，也可用`UNITY_EDITOR`或`--unity`显式指定可执行文件。

```bash
Tools/CI/run-unity-tests.sh \
  --mode EditMode \
  --results artifacts/evals/T100/editmode-results.xml \
  --log artifacts/tmp/T100-editmode-unity.log

Tools/CI/run-unity-tests.sh \
  --mode PlayMode \
  --results artifacts/evals/T100/playmode-results.xml \
  --log artifacts/tmp/T100-playmode-unity.log
```

EditMode和PlayMode必须分开执行并各自产生非空NUnit XML。`check-unity-test-results.py`会再次解析根`test-run`：结果不是`Passed`、失败数非0、总数为0、XML缺失或损坏时均返回非零；因此不能只依赖Unity进程退出码。

原始Unity日志可能包含本机路径、License会话或机器信息，只保存在已忽略的`artifacts/tmp/`。提交证据前生成过滤摘要，不提交包含凭据的原始日志。

## 3. 标准Web构建入口

```bash
Tools/CI/build-web.sh --output Builds/WebGL
```

该命令调用`OneStrokeDemon.Editor.Build.WebBuildEntry.BuildFromCommandLine`，使用Build Settings中所有启用场景，强制build index 0为Bootstrap并固定目标为WebGL。Unity构建失败或输出缺少`index.html`时命令返回非零。

T040只建立并编译此入口，不执行Web构建。标准Web实际构建、运行和证据属于T100；微信转换、DevTools和真机必须继续分别记录，不能由标准Web结果代替。

## 4. 最小反馈环

```text
小范围修改
→ 静态检查/配置导出
→ Unity Refresh与编译
→ Console Error检查
→ 专项EditMode
→ 专项PlayMode
→ 真实玩家路径
→ 任务需要时全量回归
→ git diff --check / status / 白名单审查
→ verification与证据
→ 仅暂存白名单并审查cached diff
→ TASK-ID提交
```

不需要某一层时在verification中写`NOT RUN`和原因；缺Editor、SDK、DevTools或真机时写`BLOCKED`或`KNOWN ISSUE`，不得伪造PASS。

## 5. Harness自检

```bash
Tools/CI/test-harness-smoke.sh
```

自检不启动Unity：它验证通过/失败XML退出码、非法参数、证据初始化和防覆盖行为。Unity测试命令仍必须按任务要求真实运行，不能用该自检替代。

## 6. 收尾清单

1. 运行专项验证和必要回归，保存XML及过滤摘要。
2. 从真实入口执行玩家路径；记录可断言的场景、状态或数值。
3. 检查Console Error及场景/Prefab缺失引用。
4. 对照`change-whitelist.md`逐项解释`git status --short`。
5. 执行`git diff --check`和敏感信息扫描。
6. 更新`docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`与`verification.md`。
7. 仅暂存本任务文件，执行`git diff --cached --check`并审查stat/diff。
8. 创建一个`TASK-ID: imperative summary`提交，然后停止。

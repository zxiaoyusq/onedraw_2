# T040 Verification

## 追溯

- 日期：2026-07-13。
- 任务与范围：建立批处理测试、标准Web构建入口、证据模板、白名单与一任务一提交工作流；未执行标准Web构建或任何微信平台门。
- 分支/基线：`main` / `ad942bd0 T030: establish scene and assembly skeleton`；开始时工作树干净。
- Unity：`6000.5.1f1`；Unity MCP active instance `onedraw_2@272e911286835fad`。

## 实现

- `run-unity-tests.sh`分别运行EditMode/PlayMode并输出NUnit XML；Unity进程非零直接传播，进程为0后仍由Python检查结果、计数和XML完整性。
- `check-unity-test-results.py`只允许非空、`Passed`且失败数为0的结果返回0。
- `build-web.sh`调用`WebBuildEntry.BuildFromCommandLine`；入口固定WebGL、使用启用场景并要求Bootstrap为build index 0，构建失败通过`BuildFailedException`传播。
- `new-task-evidence.sh`从模板创建verification/白名单并记录Git基线，拒绝覆盖既有证据。
- `docs/WORKFLOW.md`记录最小反馈环、日志卫生、平台分层和一个任务一个提交步骤。

## 验证

- Harness自检：PASS。通过XML退出0；失败XML退出1；非法或缺值的测试/构建参数退出64；证据首次创建和帮助退出0、重复覆盖退出73。
- Unity编译/Console：Editor Refresh与编译通过。重开后曾保留主动关闭期间的两个AssetImportWorker EOF旧记录；清空并强制Refresh/编译后未重现，最终Console Error为0。
- 专项EditMode：最终MCP job `984d736707fa4e7a81d47bf0131ae810`，2/2 PASS；验证WebGL参数不修改Build Settings以及工作流文件合同。
- 独立批处理EditMode：命令退出0，6/6 PASS，XML为`editmode-results.xml`。
- 独立批处理PlayMode：命令退出0，2/2 PASS，XML为`playmode-results.xml`。
- 真实玩家路径：Editor从Bootstrap进入Play Mode，实际MainMenu层级包含Main Camera、Global Light 2D和MainMenuGraybox；退出后Console Error为0。
- Web入口：编译与合同测试PASS；实际Web构建`NOT RUN`，`Builds/WebGL`未创建。
- 标准Web/微信转换/DevTools/真机：`NOT RUN`，分别属于T100/T110/T120后续任务，不声称PASS。

## 改动与结论

- 白名单：见`change-whitelist.md`；原始Unity日志只保存在被忽略的`artifacts/tmp/`。
- 静态检查：Shell/Python/XML语法、脚本可执行权限、敏感信息扫描、`git diff --check`和白名单审查均通过。
- 结论：PASS。T040完成，T100置为READY；未开始T100。

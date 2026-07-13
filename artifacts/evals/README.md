# 验证证据目录

每个任务建立`artifacts/evals/TASK-ID/verification.md`和`change-whitelist.md`，并把与该任务直接相关的截图、测试XML、过滤后的构建日志和性能摘要放在同一目录。使用`Tools/CI/new-task-evidence.sh TASK-ID`可从`templates/`初始化文件并记录Git基线，已有目录不会被覆盖。

原始Unity日志放在已忽略的`artifacts/tmp/`，提交前过滤凭据和机器/License信息。不要把大体积构建产物无选择地提交Git；至少保存可复核摘要、哈希、版本和产物位置。平台验证必须区分Editor、标准Web、微信转换、开发者工具和真机。完整流程见`docs/WORKFLOW.md`。

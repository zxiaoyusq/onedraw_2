# RELEASE_CHECKLIST：微信小游戏发布候选

## 代码与配置

- [ ] 工作树干净，发布commit已打tag。
- [ ] Unity精确版本、包版本和微信SDK版本或commit已记录。
- [ ] 配置Excel、生成JSON、schemaVersion、contentVersion、contentHash一致。
- [ ] 全量EditMode和PlayMode通过。
- [ ] Console无新增阻断Error，Warning已分类。
- [ ] 三关完整路径通过，失败、胜利和重开互斥且幂等。

## 性能与资源

- [ ] 低端真机普通战斗目标60fps，最差不持续低于30fps。
- [ ] 热路径GC接近0，无持续内存增长。
- [ ] 纹理、音频、字体、图集和构建体积符合项目预算。
- [ ] 中文无缺字，Safe Area和常见横屏比例通过。

## 平台四级门

- [ ] 标准Unity Web构建。
- [ ] 微信转换。
- [ ] 微信开发者工具。
- [ ] Android和iOS至少各一台真机。
- [ ] 前后台、锁屏、恢复、音频、存储和触摸取消通过。

## 发布资料

- [ ] 版本说明、已知问题、回滚包和回滚步骤。
- [ ] 隐私与平台能力清单和实际接入一致。
- [ ] AppSecret等敏感信息不在仓库和日志中。
- [ ] 所有未执行项明确写BLOCKED或KNOWN ISSUE。

# 05_RELEASE_CANDIDATE：候选版本收敛

目标是验证已有范围，不新增功能。

## 冻结

- 锁定Unity精确版本、微信SDK版本/commit、配置Schema和内容版本。
- 锁定MVP功能范围。
- 非阻断需求进入BACKLOG，不在候选版本临时扩展。

## 回归

- 配置导出、校验、JSON diff和启动加载。
- 全量EditMode/PlayMode。
- 三个关卡各完整通关和失败一次。
- 暂停、恢复、重开、下一关、存档恢复。
- 连续三次完整重开。
- 对象池压力、弹幕峰值、伤害数字和VFX峰值。
- 中文字体、Safe Area、音频、前后台。
- Web、转换、DevTools、至少一台真机。

## 产物

- 构建号、commit、配置哈希。
- 构建日志、包体、耗时、性能摘要。
- 已知问题和回滚点。
- 隐私/权限/资源授权检查。
- `artifacts/evals/RC-*/verification.md`。

除非所有强制门通过，否则不能标记Release Candidate PASS。

# T110 Upstream Source Snapshot

- Queried: 2026-07-13 (Asia/Shanghai)
- Official repository: `https://github.com/wechat-miniprogram/minigame-tuanjie-transform-sdk`
- `HEAD` / `main`: `ed4ad28f433c6b52b5fd3f22a6fa155a0c98c228`
- Release branch containing commit: `v0.1.33`
- Commit subject: `Auto-publish release WXSDK.`
- Commit time: `2026-06-22 14:48:10 +0800`
- Git tree: `22b3686bdf558fa34c7d204c381281c721fe0b70`
- CHANGELOG release: `2026-6-22 v0.1.33`
- Plugin build version: `202606220647`
- UPM package ID/version metadata: `com.qq.weixin.minigame` / `0.1.1` (known upstream metadata mismatch)
- License: MIT
- License SHA-256: `9223170188b2dfafcafc2b89d96da21ffa8396fd97485671fc579756999aeb21`
- Package JSON SHA-256: `4ccaa7aadc4495a286df7cb57b2c2734803fdef6b65d91cf029466e5ff0873fe`
- CHANGELOG SHA-256: `eed643c82858380ace863ad67c5f3e60f9747ccebbb2a84571aee9673c6c8648`
- Full-file checksum-list SHA-256: `bcad271229e1fbd671e87471e9af9a22ade700a0aeec35d1480f767a000551f8`
- Deprecated source check: `wechat-miniprogram/minigame-unity-webgl-transform` remains disabled and was not used as an install source.

Query commands:

```text
git ls-remote https://github.com/wechat-miniprogram/minigame-tuanjie-transform-sdk.git HEAD refs/heads/main refs/tags/'*'
git checkout --detach ed4ad28f433c6b52b5fd3f22a6fa155a0c98c228
git rev-parse HEAD^{tree}
shasum -a 256 LICENSE package.json CHANGELOG.md
```

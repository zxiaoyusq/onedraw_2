# T630 Source Asset Audit

- 日期：2026-07-14
- 任务：T630
- Git基线：`1fa1d465c5c6bd8a81ea1933e56763928f64fd9a`

## 权威描述

- `reference/PSD_ASSET_NOTES.md`描述一份2868×1320、约125MB的横版战斗概念PSD，包含红色幽冥洞穴、黄衣灵猫主角、五种普通怪、UI与战斗特效。
- `docs/ASSET_INTEGRATION.md`要求只把确认过来源与授权的透明PNG接入Runtime，并特别指出轮车僵妖图层带生成图标记，正式使用前必须核对来源和授权。

## 已检查位置

- 当前工作区递归文件；只发现Unity Package Cache自带的无关示例PSD/PSB。
- Unity工程父目录。
- Spotlight索引、Downloads、Desktop、Documents、`~/.codex`与`/tmp`。
- 当前Git全部可达历史及未引用对象；没有目标PSD/PSB的文件历史。
- 2026-07-11创建开发包的Codex会话用户消息；没有图像/文件附件记录或可复用本地路径。
- 原始下载包`一笔镇妖_Unity微信小游戏_ClaudeCodex开发包.zip`；只含`reference/PSD_ASSET_NOTES.md`和配置预览PNG，不含PSD、PSB或战斗素材导出图。

## 初始结论（后续已解决）

当前只有文字摘要，无法：

1. 从指定PSD准确导出背景、主角、怪物、UI和特效；
2. 验证透明边界、图层归属、原始尺寸、Pivot依据或像素内容；
3. 核对轮车僵妖及其他生成图层的来源与授权；
4. 将新生成图片诚实标记为“PSD解析所得”。

继续T630需要以下任一明确输入：

- 推荐：提供原始PSD/PSB，或提供按图层导出的透明PNG目录，并附来源/授权说明；
- 替代：明确授权不再要求复用该PSD，改由图像生成工具创建一套全新的原型位图；这会作为“新生成原型图”记录，不能声称是PSD导出物。

## 2026-07-14输入补充与解决

- 用户随后直接提供外部文件`一笔镇妖 主视觉 测试 03.psd`，并明确允许在需要时使用ImageGen生成和编辑。
- 文件经本地只读核验为Adobe PSD、2868×1320、RGB 8-bit、130,855,476字节，SHA-256为`e6a2552a69270899b59d6236958767678a4025e402484e16c0ac0d7d4031fb34`。原PSD未复制到仓库或Runtime Assets。
- `Tools/ArtPipeline/inspect_psd.py`导出`psd-layer-manifest.json`和预览证据；选择的背景、主角、五种PSD敌人、UI与VFX图层均可追溯到该总文件hash。
- PSD不含配置所需的魂偶和镇墓玄甲王独立角色图。两者按用户授权由ImageGen补齐，原始生成图SHA-256为`425a8eb7036ba39b7ca66e7c877d8507151c56186eda2a4c667bb4d5af45a0c8`，提示词和透明化过程分别记录于`imagegen-prompt.md`、`generated-actor-sheet-alpha.png`与`source-hashes.txt`；不得声称二者来自PSD。
- 轮车僵妖来自PSD图层7，图层名为`Gemini_Generated_Image_725q0r725q0r725q`。用户提供行为足以授权当前项目原型使用，但其上游生成平台、账号和商用条款仍未知，因此只能标为`APPROVED_PROTOTYPE`。
- 最终Runtime输出为28个RGBA PNG，逐文件hash见`asset-output-manifest.sha256`；`Assets/`内不存在PSD/PSB/JPG/JPEG。所有输入和输出均满足T630原型用途，均未达到`APPROVED_RELEASE`。

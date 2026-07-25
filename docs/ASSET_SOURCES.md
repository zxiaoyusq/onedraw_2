# ASSET_SOURCES：美术来源与授权登记

## 状态值

- `MISSING`：只知道描述，源文件不可用。
- `PENDING_REVIEW`：文件已取得，但来源、作者、平台条款或商用范围尚未核对。
- `APPROVED_PROTOTYPE`：允许用于本项目原型和测试构建，不代表可发布商用。
- `APPROVED_RELEASE`：来源、授权和发布范围已核对，可进入发布候选。
- `REJECTED`：不得进入Runtime或构建。

## T630输入登记

| source_id | 内容 | 文件/位置 | SHA-256 | 来源/作者/工具 | 授权范围 | 状态 | 备注 |
|---|---|---|---|---|---|---|---|
| `concept_battle_psd` | 2868×1320红色幽冥洞穴战斗概念、黄衣灵猫、五怪、UI与特效 | 外部文件`一笔镇妖 主视觉 测试 03.psd`，不提交到仓库 | `e6a2552a69270899b59d6236958767678a4025e402484e16c0ac0d7d4031fb34` | 用户于2026-07-14直接提供；PSD内图层作者未另行标注 | 本项目原型、测试构建与内部评审；不推定商用发布权 | `APPROVED_PROTOTYPE` | 130,855,476字节、Adobe PSD、RGB 8-bit。运行时只提交派生RGBA PNG。 |
| `wheel_zombie_layer` | 轮车僵妖 | 上述PSD图层7，名称`Gemini_Generated_Image_725q0r725q0r725q` | 由PSD总文件hash及`psd-layer-manifest.json`共同追溯 | 用户提供的PSD内生成图层；具体上游平台、账号及条款未知 | 仅随本项目原型输入使用；发布前必须补上游授权链 | `APPROVED_PROTOTYPE` | 可用于当前原型，不能据此升级为`APPROVED_RELEASE`。 |
| `t630_generated_actor_sheet` | 补齐PSD未含的魂偶与镇墓玄甲王 | `artifacts/evals/T630/generated-actor-sheet-original.png` | `425a8eb7036ba39b7ca66e7c877d8507151c56186eda2a4c667bb4d5af45a0c8` | OpenAI ImageGen；提示词见`artifacts/evals/T630/imagegen-prompt.md` | 用户明确允许为本任务生成/编辑；仅本项目原型、测试构建与内部评审 | `APPROVED_PROTOTYPE` | 去绿幕版本hash `6f30a54b6a226ddd5834d74a465c62f50efec5d962a7db43366b5baffb2103ca`；最终两个角色PNG分别为`2cecfb2f8b38889620719f56355085260a9291873b07a38bd43b651def0053b4`与`345ec8d4d84d00d8a18e47fd174e760db1dbf201fae368fbd41760dbab194c70`。不得声称来自PSD。 |
| `t630_runtime_outputs` | 2背景、1主角、7敌人/Boss、7 UI、7图标/投射物、4 VFX源Sprite | `Assets/_Game/Art/**` | 逐文件见`artifacts/evals/T630/asset-output-manifest.sha256` | 由前述两类已登记输入确定性导出、裁切或去绿幕 | 与各上游输入相同，仅原型 | `APPROVED_PROTOTYPE` | Runtime共28个RGBA PNG；26个非背景资源包含透明像素；无PSD/PSB/JPG/JPEG。 |

## T694输入登记

| source_id | 内容 | 文件/位置 | SHA-256 | 来源/作者/工具 | 授权范围 | 状态 | 备注 |
|---|---|---|---|---|---|---|---|
| `t694_moyan_idle_sheet` | 主角墨衍待机3×3图集与九帧坐标JSON | 用户于2026-07-25直接提供的外部目录`video-frames-20260716-160532_sheet`；运行时副本位于`Assets/_Game/Art/Characters/Animated/Moyan/` | PNG `f573a2a20e4d79f8efae11009cb483408d1a767cef20ae3c64269b6e427a8db9`；JSON `f6758197cf255eaa67e7fe56936a0695e23136edbe7d97dc5f931a5c14ebaae8` | 用户直接提供；具体作者与生成工具未另行标注 | 本项目原型、测试构建与内部评审；不推定商用发布权 | `APPROVED_PROTOTYPE` | 768×768 RGBA，9个256×256未旋转/未裁边帧；源文件与Runtime副本hash一致。 |
| `t694_moyan_attack_sheet` | 主角墨衍攻击4×3图集与十二帧坐标JSON | 用户于2026-07-25直接提供的外部目录`video-frames-20260716-164241_sheet`；运行时副本位于`Assets/_Game/Art/Characters/Animated/Moyan/` | PNG `4ab9574e619860f747e72216764da156d07289b9150a816316f81ffca72b875d`；JSON `a58afe876514fb6bd34906a64d9a50cae9cff744a611f17fdef9cbc43ab17b22` | 用户直接提供；具体作者与生成工具未另行标注 | 本项目原型、测试构建与内部评审；不推定商用发布权 | `APPROVED_PROTOTYPE` | 1024×768 RGBA，12个256×256未旋转/未裁边帧；源文件与Runtime副本hash一致。 |

## 审计结论

截至2026-07-25，T630与T694运行时位图的全部直接输入均达到`APPROVED_PROTOTYPE`，允许进入本项目原型与测试构建。没有任何登记美术达到`APPROVED_RELEASE`：发布候选前必须确认用户提供PSD、主角动画图集及各生成图层的作者/商用授权，尤其是轮车僵妖生成图层的上游平台与条款，并重新评审ImageGen成品的发布范围。

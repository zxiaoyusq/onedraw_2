# T630 Verification

## 追溯

- 日期：2026-07-14
- 任务与范围：T630；从用户提供PSD导出背景、主角、五怪、UI和VFX，ImageGen补齐魂偶与Boss，统一Importer/Atlas/Sorting Layer，并把Registry视觉键替换为实际原型资源。
- 明确不做：不提交原始PSD，不虚构骨骼拆件或正式动画，不新增玩法数值/配置，不制作音频，不提前实现T640/T650，不恢复T120/T130微信平台工作。
- 分支/提交：`main`；任务提交以`T630:`开头。
- 任务开始Git基线：见`baseline-commit.txt`与`baseline-status.txt`。
- Unity精确版本与MCP实例：6000.5.1f1；本任务未配置或使用Unity MCP，所有Unity资源写入均通过本机Unity Editor批处理API完成。
- 配置Schema/内容版本/hash：schema 5 / content `0.6.1-sample` / `152b9faa81ba66e29469d7a4a48227f8fb7ef0f969f1cb13679d6fe0ce0786f8`；未修改配置真相源或生成物。

## 改动审查

- 预计白名单：见`change-whitelist.md`。
- 实际改动：28张RGBA PNG、5个SpriteAtlas v2、2个Actor Prefab、38个VFX Prefab、T630 Unity作者工具与5项EditMode合同测试；Canonical Registry的18个Sprite/40个Prefab键改绑实际资源；TagManager仅新增并排序`Background/Actors/Projectiles/VFX`；补充可重复PSD审计/导出/派生/画廊工具和完整来源证据；`.gitattributes`仅把Unity生成的`.prefab/.spriteatlasv2`空标量尾空格纳入既有Unity YAML例外。
- 用户已有改动保护：基线工作树干净，无需合并或覆盖用户已有改动；TMP材质自动脏写和根目录崩溃诊断均在提交前排除。
- `git diff --check`：PASS（提交前最终执行）。
- 暂存白名单审查：PASS；仅包含`change-whitelist.md`列出的T630路径（提交前最终执行）。

## 自动验证

- 静态/导出校验：`art-static-validation.txt`为`ART_STATIC_VALIDATION_PASS`；28个PNG均为RGBA、26个非背景资源有透明像素、38个VFX Prefab、2个Actor Prefab、5个Atlas，Runtime PSD/PSB/JPG/JPEG为0。Unity作者日志为`T630_ART_AUTHORING_PASS`，Registry共76键（Prefab 40/Sprite 18/AudioClip 17/Scene 1）。`config-verify.log`为`CONFIG_PIPELINE_PASS`，生成物漂移0、.NET 58/58、配置EditMode 19/19、配置PlayMode 3/3。
- EditMode XML：T630专项5/5/0，`editmode-results.xml`；最终全量192/192/0，`full-editmode-results.xml`。
- PlayMode XML：最终全量45/45/0，`full-playmode-results.xml`。
- Console新增Error/Warning：最终专项、全量和配置日志扫描均为0。

## 玩家与平台证据

- 真实玩家路径和可断言值：本任务未运行场景级真实玩家或设备路径。确定性1920×1080资产画廊已人工复核两张背景、主角、六怪、Boss、UI、投射物和VFX的最终RGBA内容，无洋红占位、透明边界裁切或资源错位；自动化测试证明Registry与可渲染Prefab接线。
- 标准Web：NOT RUN（用户要求暂时绕过平台/打包工作）
- 微信转换：NOT RUN（同上）
- DevTools：NOT RUN（T120仍因缺工具BLOCKED）
- 真机：NOT RUN（缺设备与DevTools路径，且用户要求延期）
- 截图/日志/产物：`prototype-art-review.png`（1920×1080，SHA-256 `2e7cb0886c597ea39ec905fb565d39f8f7132f7708b17d7997976cff674948e3`）；`source-preview.png`、前景/背景图层联系表、`psd-layer-manifest.json`、`source-hashes.txt`和`asset-output-manifest.sha256`。

## 结论

- 已知问题：生成的魂偶/Boss细节密度高于PSD单帧角色，所有资源仅获原型授权，轮车僵妖的上游生成条款仍待发布前核验。尝试的`-nographics` RenderTexture捕获发生原生崩溃，Metal批处理捕获出现纹理复用/颜色损坏；对应日志保留为`unity-visual-smoke.log`、`unity-offscreen-capture-invalid.log`和`unity-visual-smoke-no-atlas-diagnostic.log`，结果全部标为INVALID且未作为验收证据。
- 结论：PASS（T630原型素材导入范围）；发布授权、标准Web/微信/DevTools/真机与正式动画品质均未宣称通过。

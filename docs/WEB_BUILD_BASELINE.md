# WEB_BUILD_BASELINE：标准Unity Web G1基线

## 结论

- Gate：G1 Unity Web Build。
- 任务：T100。
- 结论：`PASS WITH KNOWN ISSUES`。
- Unity：`6000.5.1f1 (0d9463e84828)`。
- 构建目标：标准Unity WebGL；未安装、调用或验证微信转换SDK。
- 构建入口：`Tools/CI/build-web.sh --smoke --output Builds/WebGL`。
- 构建耗时：`00:09:09.0607100`。
- 输出总字节：`12,433,772`；其中Build目录`12,414,288`字节。
- 哈希清单：`artifacts/evals/T100/build-manifest.sha256`。

## 构建产物

输出包含`index.html`、Unity loader，以及Brotli压缩的data、framework和wasm。主要文件：

| 文件 | 字节 | SHA-256 |
|---|---:|---|
| `Build/WebGL.data.br` | 3,853,938 | `5e579d0a3c8cad70e880e6a7607995e4db17b215ee9c8bcd98e78af4597a6fd6` |
| `Build/WebGL.framework.js.br` | 73,847 | `824ce470b1d3ea51e16fcf7762926ef490957124eda809c75486f2a66bb9e757` |
| `Build/WebGL.loader.js` | 26,982 | `56cda4b919221dbb218c344f1c0bb5d120b6818b036b382f86e3287dd79b331f` |
| `Build/WebGL.wasm.br` | 8,459,521 | `7508efd2a7ff1e92efeaf14d65457f4fadbd8bd1d742c2c204ea166cfab13124` |
| `index.html` | 5,588 | `69516b26793593ea3b472e7a8aea305d6292adc6d4ce29215b5ef1cbfeb395f5` |

`Builds/WebGL`被Git忽略，不作为源码提交；证据只保存版本、字节、哈希、日志摘要与运行截图。

## 本地HTTP与浏览器

- `Tools/CI/serve-web-build.py`为`.br`产物发送正确的`Content-Encoding: br`。
- WASM响应为`application/wasm`，framework响应为`application/javascript`。
- `http://127.0.0.1:8123/`首次加载和重载均返回HTTP 200。
- Unity运行时进入`MainMenu`，画布实际渲染MainMenu灰盒。
- 浏览器Console Error：0。

## T100技术探针

探针只在构建显式包含`T100_WEB_SMOKE`时编译，默认构建不包含它。

| 项目 | 可断言结果 | 边界 |
|---|---|---|
| 输入 | 点击Unity canvas后Input System报告`input=pass` | 单次鼠标/指针点击；不等于真机触摸延迟验收 |
| 音频 | 同一用户手势触发合成AudioClip，`AudioSource.isPlaying`报告`audio=pass` | 证明WebAudio播放路径启动；未做主观响度/后台恢复 |
| 中文 | C# UTF-8字符串经WebGL bridge显示`标准网页中文` | 证明UTF-8 interop/DOM显示；不等于TMP中文字体覆盖，TMP仍属T610/后续平台Spike |
| 存储 | PlayerPrefs版本化JSON写读`storage=pass`；重载后`storageRun`从1变2 | 证明本地Web持久化；不等于微信平台存储API |

## 已知问题与平台边界

- `BUG-0001`：WebGL运行时警告URP Edge Adaptive Spatial Upsampling shader不支持；当前灰盒不依赖后处理，后续视觉基线前必须处理。
- `BUG-0002`：`PlayerPrefs.Save`触发persistentDataPath手动同步API未来弃用 warning；T130平台服务/模板需切换自动同步。
- `BUG-0003`：长Web构建后Unity MCP实例桥接未自动恢复；本任务用批处理XML和日志完成最终Unity验证。
- G2微信转换、G3 DevTools、G4真机均为`NOT RUN`。G1通过不能证明任何后续门通过。

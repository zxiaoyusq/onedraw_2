# T310 Stroke Sampling Contract

- 输入坐标：只消费T300已经转换好的参考像素坐标，不读取屏幕DPI或设备分辨率。
- 配置边界：`StrokeSamplingSettingsFactory`从调用方选中的`StrokeRuleConfig`读取`minPointDistanceRefPx`、`maxStrokeLengthRefPx`和`maxPointCount`；Input程序集不依赖Config，也不硬编码某个规则ID或玩法数值。
- 最小距离：相对最后一个已接受点计算；距离小于阈值时忽略，等于阈值时接受。
- 最大长度：最小距离过滤之后，若新段跨越剩余长度，则在该段上线性插值终点，使结果路径总长精确等于配置上限。
- 最大点数：首点计入容量；收满最后一个合法点时以`MaximumPointCount`终止，后续移动或抬起不再生成第二份结果。
- 完成与取消：正常抬起、最大长度、最大点数都会生成一份不可变`StrokeData`；生命周期取消只生成`StrokeCanceledEvent`并丢弃采样中数据。
- 分配：固定点缓冲在`StrokeSampler`构造时分配；合法收点热路径不分配托管内存；只在终止时复制一次精确长度数组并创建只读快照。
- 延后范围：RDP、最大点数几何重采样、面积/闭合/曲率、识别、视觉和命中分别留给T320及后续任务。

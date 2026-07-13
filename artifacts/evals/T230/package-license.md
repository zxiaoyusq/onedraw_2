# T230 Runtime JSON Package Record

- Unity包：`com.unity.nuget.newtonsoft-json 3.2.2`，registry直接依赖，lock depth 0。
- 包描述同步上游：Newtonsoft.Json `13.0.2`。
- package revision：`d8e49aef8979bef617144382052ec2f479645eaf`；package fingerprint：`4dfd81071c6475bb9c114f920bfb4e3fc5e28c6a`。
- Unity包封装许可证：Unity Companion License，`LICENSE.md` SHA-256 `a5177bfecfea04d225e4a26a631db86721a03b4080350867847e71dc26b44f85`。
- `package.json` SHA-256：`29c4661a6f9d6839813f2ead6a232c890aaf67eaf44faf8ea2da85e816368558`。
- 包内第三方通知列出 Newtonsoft.Json、Json.Net.Unity3D、Newtonsoft.Json-for-Unity、com.newtonsoft.json，许可证均为MIT。
- 选择原因：冻结Schema包含可空数值/布尔；Runtime必须区分缺失属性和显式null，Unity `JsonUtility`不能忠实表达该严格合同。

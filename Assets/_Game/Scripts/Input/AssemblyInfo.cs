using System.Runtime.CompilerServices;

// 向输入专项测试开放内部处理器，便于直接验证指针所有权和生命周期边界。
[assembly: InternalsVisibleTo("OneStrokeDemon.Tests.EditMode")]
[assembly: InternalsVisibleTo("OneStrokeDemon.Tests.PlayMode")]

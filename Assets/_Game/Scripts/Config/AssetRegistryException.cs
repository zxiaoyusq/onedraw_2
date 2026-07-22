using System;

namespace OneStrokeDemon.Config
{
    /// <summary>
    /// 表示资源注册表装载或查询失败，并携带稳定错误码与定位上下文。
    /// </summary>
    public sealed class AssetRegistryException : Exception
    {
        /// <summary>创建包含来源、上下文和可选内部异常的注册表异常。</summary>
        internal AssetRegistryException(
            string code,
            string message,
            string source,
            string context,
            Exception innerException = null)
            : base($"{code} [source={source}, context={context}]: {message}", innerException)
        {
            Code = code;
            Source = source;
            Context = context;
        }

        /// <summary>获取可供测试和日志识别的稳定错误码。</summary>
        public string Code { get; }

        /// <summary>获取发生错误的注册表来源。</summary>
        public new string Source { get; }

        /// <summary>获取发生错误的字段或生命周期上下文。</summary>
        public string Context { get; }
    }
}

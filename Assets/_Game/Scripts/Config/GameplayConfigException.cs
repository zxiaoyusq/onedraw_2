using System;

namespace OneStrokeDemon.Config
{
    /// <summary>
    /// 表示玩法配置解析、兼容性、结构或查询失败，并保留稳定错误上下文。
    /// </summary>
    public sealed class GameplayConfigException : Exception
    {
        /// <summary>创建包含错误码、来源、上下文和可选内部异常的配置异常。</summary>
        public GameplayConfigException(
            string code,
            string message,
            string source,
            string context = "",
            Exception innerException = null)
            : base(FormatMessage(code, message, source, context), innerException)
        {
            Code = code;
            Source = source ?? string.Empty;
            Context = context ?? string.Empty;
        }

        /// <summary>获取可供测试和日志识别的稳定错误码。</summary>
        public string Code { get; }

        /// <summary>获取配置数据来源。</summary>
        public string Source { get; }

        /// <summary>获取出错的表、字段或生命周期上下文。</summary>
        public string Context { get; }

        /// <summary>把来源和上下文整理为统一、可检索的异常消息。</summary>
        private static string FormatMessage(string code, string message, string source, string context)
        {
            string location = string.IsNullOrEmpty(context) ? source : $"{source}::{context}";
            return $"{code} [{location}]: {message}";
        }
    }
}

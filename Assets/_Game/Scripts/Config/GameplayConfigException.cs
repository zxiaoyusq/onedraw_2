using System;

namespace OneStrokeDemon.Config
{
    public sealed class GameplayConfigException : Exception
    {
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

        public string Code { get; }

        public string Source { get; }

        public string Context { get; }

        private static string FormatMessage(string code, string message, string source, string context)
        {
            string location = string.IsNullOrEmpty(context) ? source : $"{source}::{context}";
            return $"{code} [{location}]: {message}";
        }
    }
}

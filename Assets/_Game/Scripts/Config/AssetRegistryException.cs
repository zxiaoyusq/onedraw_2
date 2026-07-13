using System;

namespace OneStrokeDemon.Config
{
    public sealed class AssetRegistryException : Exception
    {
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

        public string Code { get; }

        public string Source { get; }

        public string Context { get; }
    }
}

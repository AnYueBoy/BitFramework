using UnityEngine.TestTools;

namespace BitFramework.Exception
{
    /// <summary>
    /// 未能解析服务的异常
    /// </summary>
    [ExcludeFromCoverage]
    public class UnresolvableException : RuntimeException
    {
        public UnresolvableException()
        {
        }

        public UnresolvableException(string message) : base(message)
        {
        }

        public UnresolvableException(string message, System.Exception innerException) : base(message, innerException)
        {
        }
    }
}
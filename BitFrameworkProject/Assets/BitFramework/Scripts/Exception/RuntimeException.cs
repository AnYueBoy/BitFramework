using System.Runtime.Serialization;
using JetBrains.Annotations;
using SException = System.Exception;

namespace BitFramework.Exception
{
    public class RuntimeException : SException
    {
        public RuntimeException()
        {
        }

        public RuntimeException(string message) : base(message)
        {
        }

        public RuntimeException(string message, SException innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// 构造函数 
        /// </summary>
        /// <param name="info">保存有关引发的异常的序列化对象数据</param>
        /// <param name="context">包含有关源或目标的上下文信息</param>
        protected RuntimeException([NotNull] SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
using System.Runtime.Serialization;
using JetBrains.Annotations;
using UnityEngine.TestTools;

namespace BitFramework.Exception
{
    /// <summary>
    /// 表示执行期间遇到的逻辑异常。
    /// </summary>
    /// 将此属性放置在类或结构上会从代码覆盖率信息的集合中排除该类或结构的所有成员。
    [ExcludeFromCoverage]
    public class LogicException : RuntimeException
    {
        public LogicException()
        {
        }

        public LogicException(string message) : base(message)
        {
        }

        public LogicException(string message, System.Exception innerException) : base(message, innerException)
        {
        }

        /// <inheritdoc/> 
        protected LogicException([NotNull] SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
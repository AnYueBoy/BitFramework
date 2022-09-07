using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace BitFramework.Exception
{
    public class AssertException : RuntimeException
    {
        public AssertException()
        {
        }

        public AssertException(string message) : base(message)
        {
        }

        public AssertException(string message, System.Exception innerException) : base(message, innerException)
        {
        }

        ///<inheritdoc />
        protected AssertException([NotNull] SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
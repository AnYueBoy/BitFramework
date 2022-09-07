using System;
using System.Collections.Generic;
using SException = System.Exception;
using System.Reflection;

namespace BitFramework.Util
{
    public class Guard
    {
        public delegate SException ExtendException(string message, SException innerException, object state);

        private static Guard that;
        private static IDictionary<Type, ExtendException> exceptionFactory;

        public static Guard That
        {
            get
            {
                if (that == null)
                {
                    that = new Guard();
                }

                return that;
            }
        }

        private static SException CreateExceptionInstance(Type exceptionType, string message, SException innerException,
            object state)
        {
            if (!typeof(SException).IsAssignableFrom(exceptionType))
            {
                throw new ArgumentException($"Type: {exceptionType} must be inherited from: {typeof(SException)}",
                    nameof(exceptionType));
            }

            VerifyExceptionFactory();

            if (exceptionFactory.TryGetValue(exceptionType, out ExtendException factory))
            {
                var ret = factory(message, innerException, state);
                if (ret != null)
                {
                    return ret;
                }
            }

            var exception = Activator.CreateInstance(exceptionType);
            if (!string.IsNullOrEmpty(message))
            {
                SetFiled(exception, "_message", message);
            }

            if (innerException != null)
            {
                SetFiled(exception, "_innerException", innerException);
            }

            return (SException)exception;
        }

        private static void VerifyExceptionFactory()
        {
            if (exceptionFactory == null)
            {
                exceptionFactory = new Dictionary<Type, ExtendException>();
            }
        }

        [System.Diagnostics.DebuggerNonUserCode]
        public static void Extend<T>(ExtendException factory)
        {
            Extend(typeof(T), factory);
        }

        [System.Diagnostics.DebuggerNonUserCode]
        public static void Extend(Type exception, ExtendException factory)
        {
            VerifyExceptionFactory();
            exceptionFactory[exception] = factory;
        }

        /// <summary>
        /// 检查参数不为空
        /// </summary>
        /// 此属性禁止显示调试器窗口中的这些辅助类型和成员，
        /// 并自动执行设计器提供的代码，而不是单步执行。
        /// 当调试器在单步执行用户代码时遇到此属性时，用户体验是看不到设计器提供的代码，并单步执行下一个用户提供的代码语句。
        [System.Diagnostics.DebuggerNonUserCode]
        public static void ParameterNotNull(object argumentValue, string message = null,
            SException innerException = null)
        {
            if (argumentValue != null)
            {
                return;
            }

            message = message ??
                      $"Parameter {nameof(argumentValue)} not allowed for null. please check the function input.";
            var exception = new ArgumentException(nameof(argumentValue), message);
            if (innerException != null)
            {
                SetFiled(exception, "_innerException", innerException);
            }

            throw exception;
        }

        /// <summary>
        /// 验证条件，并在条件为false时抛出异常
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCode]
        public static void Requires<TException>(bool condition, string message = null, SException innerException = null,
            object state = null)
            where TException : SException, new()
        {
            Requires(typeof(TException), condition, message, innerException, state);
        }

        /// <inheritdoc cref="Requires{TException}"/>
        [System.Diagnostics.DebuggerNonUserCode]
        public static void Requires(Type exception, bool condition, string message = null,
            SException innerException = null, object state = null)
        {
            if (condition)
            {
                return;
            }

            throw CreateExceptionInstance(exception, message, innerException, state);
        }

        private static void SetFiled(object obj, string filed, object value)
        {
            var flag = BindingFlags.Instance | BindingFlags.NonPublic;
            var filedInfo = obj.GetType().GetField(filed, flag);
            if (filedInfo != null)
            {
                filedInfo.SetValue(obj, value);
            }
        }
    }
}
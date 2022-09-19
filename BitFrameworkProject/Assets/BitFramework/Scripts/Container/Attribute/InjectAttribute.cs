using System;

namespace BitFramework.Container
{
    [AttributeUsage(AttributeTargets.Property)]
    public class InjectAttribute : Attribute
    {
        /// <summary>
        /// 获取或设置一个值，该值指示该属性是否被依赖
        /// </summary>
        public bool Required { get; set; } = true;
    }
}
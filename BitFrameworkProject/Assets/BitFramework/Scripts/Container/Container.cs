using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using BitFramework.Util;

namespace BitFramework.Container
{
    public class Container : IContainer
    {
        /// <summary>
        /// 禁止字符
        /// </summary>
        private static readonly char[] ServiceBanChars = { '@', ':', '$' };

        // 服务-绑定数据映射
        // 为何不直接使用Type作为key，因为框架中提供了别名逻辑，通过别名反映射到Type Name后，通过反射获取对应的Type
        private readonly Dictionary<string, BindData> bindings;

        // 服务-单例映射
        private readonly Dictionary<string, object> instances;

        // 单例-服务反向映射
        private readonly Dictionary<object, string> instancesReverse;

        // 别名-服务映射
        private readonly Dictionary<string, string> aliases;

        // 服务-别名列表反映射
        private readonly Dictionary<string, List<string>> aliasesReverse;

        // 服务-tag列表映射
        private readonly Dictionary<string, List<string>> tags;

        // 全局解析的回调列表
        private readonly List<Action<IBindData, object>> resolving;

        // 全局解析后的回调列表
        private readonly List<Action<IBindData, object>> afterResolving;

        // 全局释放的回调列表
        private readonly List<Action<IBindData, object>> release;

        // 服务的扩展闭包
        private readonly Dictionary<string, List<Func<object, IContainer, object>>> extenders;

        // 将字符转换成服务类型
        private readonly SortSet<Func<string, Type>, int> findType;

        // 已找到的类型缓存
        private readonly Dictionary<string, Type> findTypeCache;

        // 已解析的服务的哈希集
        private readonly HashSet<string> resolved;

        // 单例服务构建时间列表
        private readonly SortSet<string, int> instanceTiming;

        // 所有已注册的回弹回调。
        private readonly Dictionary<string, List<Action<object>>> rebound;

        // 方法的ioc容器
        private readonly MethodContainer methodContainer;

        // 表示跳过的对象以跳过某些依赖项注入
        private readonly object skipped;

        // 容器是否正在刷新
        private bool flushing;

        // 唯一Id用于标记全局生成顺序
        private int instanceId;

        /// <summary>
        /// 获取当前正在生成的具体化的堆栈
        /// </summary>
        protected Stack<string> BuildStack { get; }

        /// <summary>
        /// 获取正在生成的用户参数的堆栈
        /// </summary>
        protected Stack<object[]> UserParamsStack { get; }

        public Container(int prime = 64)
        {
            prime = Math.Max(8, prime);
            tags = new Dictionary<string, List<string>>((int)(prime * 0.25));
            aliases = new Dictionary<string, string>(prime * 4);
            aliasesReverse = new Dictionary<string, List<string>>(prime * 4);
            instances = new Dictionary<string, object>(prime * 4);
            instancesReverse = new Dictionary<object, string>(prime * 4);
            bindings = new Dictionary<string, BindData>(prime * 4);
            resolving = new List<Action<IBindData, object>>((int)(prime * 0.25));
            afterResolving = new List<Action<IBindData, object>>((int)(prime * 0.25));
            release = new List<Action<IBindData, object>>((int)(prime * 0.25));
            extenders = new Dictionary<string, List<Func<object, IContainer, object>>>((int)(prime * 0.25));
            resolved = new HashSet<string>();
            findType = new SortSet<Func<string, Type>, int>();
            findTypeCache = new Dictionary<string, Type>(prime * 4);
            rebound = new Dictionary<string, List<Action<object>>>(prime);
            instanceTiming = new SortSet<string, int>();
            BuildStack = new Stack<string>(32);
            UserParamsStack = new Stack<object[]>(32);
            methodContainer = new MethodContainer(this);
            flushing = false;
            instanceId = 0;
        }

        public string TypeConvertToService(Type type)
        {
            return type.ToString();
        }
    }
}
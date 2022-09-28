using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using BitFramework.Container;
using BitFramework.Core;
using BitFramework.EventDispatcher;
using BitFramework.Exception;
using BitFramework.Util;
using IServiceProvider = BitFramework.Core.IServiceProvider;

namespace BitFramework.Scripts
{
    public class BitApplication : Container.Container, IApplication
    {
        private static string version;
        private readonly IList<IServiceProvider> loadedProviders;
        private readonly int mainThreadId;
        private readonly IDictionary<Type, string> dispatchMapping;
        private bool bootstrapped;
        private bool inited;
        private bool registering;
        private long incrementId;
        private DebugLevel debugLevel;
        private IEventDispatcher dispatcher;

        public static BitApplication New(bool global = true)
        {
            var bitApplication = new BitApplication();
            if (global)
            {
                // 全局设置
            }

            return bitApplication;
        }

        public BitApplication()
        {
            loadedProviders = new List<IServiceProvider>();
            mainThreadId = Thread.CurrentThread.ManagedThreadId;

            RegisterBaseBindings();

            dispatchMapping = new Dictionary<Type, string>()
            {
                { typeof(AfterBootEventArgs), BitApplicationEvents.OnAfterBoot },
                { typeof(AfterInitEventArgs), BitApplicationEvents.OnAfterInit },
                { typeof(AfterTerminateEventArgs), BitApplicationEvents.OnAfterTerminate },
                { typeof(BeforeBootEventArgs), BitApplicationEvents.OnBeforeBoot },
                { typeof(BeforeInitEventArgs), BitApplicationEvents.OnBeforeInit },
                { typeof(BeforeTerminateEventArgs), BitApplicationEvents.OnBeforeTerminate },
                { typeof(BootingEventArgs), BitApplicationEvents.OnBooting },
                { typeof(InitProviderEventArgs), BitApplicationEvents.OnInitProvider },
                { typeof(RegisterProviderEventArgs), BitApplicationEvents.OnRegisterProvider },
                { typeof(StartCompletedEventArgs), BitApplicationEvents.OnStartCompleted },
            };

            // 根据服务的字符串类型获取对应的实际类型
            OnFindType(finder => { return Type.GetType(finder); });

            DebugLevel = DebugLevel.Development;
            Process = StartProcess.Construct;
        }

        private void RegisterBaseBindings()
        {
            // 将BitApplication注册进入容器
            this.Singleton<IApplication>(() => this).Alias<BitApplication>().Alias<IContainer>();
            // 将事件系统进行注册
            SetDispatcher(new EventDispatcher.EventDispatcher());
        }

        #region Frame Info

        public StartProcess Process { get; private set; }

        public static string Version => version ??
                                        (version = FileVersionInfo
                                            .GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion);

        public bool IsMainThread => mainThreadId == Thread.CurrentThread.ManagedThreadId;

        public DebugLevel DebugLevel
        {
            get => debugLevel;
            set
            {
                debugLevel = value;
                this.Instance<DebugLevel>(debugLevel);
            }
        }

        public long GetRuntimeId()
        {
            // 多线程原子操作
            return Interlocked.Increment(ref incrementId);
        }

        #endregion

        #region Life Cycle

        /// <summary>
        /// 引导给定的引导程序组
        /// </summary>
        public virtual void Bootstrap(params IBootstrap[] bootstraps)
        {
            Guard.Requires<ArgumentNullException>(bootstraps != null);

            if (bootstrapped || Process != StartProcess.Construct)
            {
                throw new LogicException($"Cannot repeatedly trigger the {nameof(Bootstrap)}");
            }

            Process = StartProcess.Bootstrap;

            bootstraps = Raise(new BeforeBootEventArgs(bootstraps, this)).GetBootstraps();

            Process = StartProcess.Bootstrapping;

            var existed = new HashSet<IBootstrap>();
            foreach (var bootstrap in bootstraps)
            {
                if (bootstrap == null)
                {
                    continue;
                }

                if (existed.Contains(bootstrap))
                {
                    throw new LogicException($"The bootstrap already exists : {bootstrap}");
                }

                existed.Add(bootstrap);

                var skipped = Raise(new BootingEventArgs(bootstrap, this)).IsSkip;
                if (!skipped)
                {
                    bootstrap.Bootstrap();
                }
            }

            Process = StartProcess.Bootstraped;
            bootstrapped = true;
            Raise(new AfterBootEventArgs(this));
        }

        public virtual void Init()
        {
            if (!bootstrapped)
            {
                throw new LogicException($"You must call {nameof(Bootstrap)}() first.");
            }

            if (inited || Process != StartProcess.Bootstraped)
            {
                throw new LogicException($"Cannot repeatedly trigger the {nameof(Init)}()");
            }

            Process = StartProcess.Init;
            Raise(new BeforeInitEventArgs(this));
            Process = StartProcess.Initing;

            foreach (var provider in loadedProviders)
            {
                InitProvider(provider);
            }

            inited = true;
            Process = StartProcess.Inited;
            Raise(new AfterInitEventArgs(this));

            Process = StartProcess.Running;
            Raise(new StartCompletedEventArgs(this));
        }


        public void Terminate()
        {
            Process = StartProcess.Terminated;
            Raise(new BeforeTerminateEventArgs(this));
            Process = StartProcess.Terminating;
            Flush();
            // TODO: 全局访问置空

            Process = StartProcess.Terminated;
            Raise(new AfterTerminateEventArgs(this));
        }

        #endregion

        #region Register

        public virtual void Register(IServiceProvider provider, bool force = false)
        {
            Guard.Requires<ArgumentNullException>(provider != null);
            if (IsRegistered(provider))
            {
                if (!force)
                {
                    throw new LogicException($"Provider [{provider.GetType()}] is already register.");
                }

                loadedProviders.Remove(provider);
            }

            if (Process == StartProcess.Initing)
            {
                throw new LogicException($"Unable to add service provider during {nameof(StartProcess.Initing)}");
            }

            if (Process > StartProcess.Running)
            {
                throw new LogicException($"Unable to {nameof(Terminate)} in-process registration service provider.");
            }

            if (provider is ServiceProvider baseProvider)
            {
                baseProvider.SetApplication(this);
            }

            var skipped = Raise(new RegisterProviderEventArgs(provider, this)).IsSkip;

            if (skipped)
            {
                return;
            }

            try
            {
                registering = true;
                provider.Register();
            }
            finally
            {
                registering = false;
            }

            loadedProviders.Add(provider);

            if (inited)
            {
                InitProvider(provider);
            }
        }

        public bool IsRegistered(IServiceProvider provider)
        {
            Guard.Requires<ArgumentNullException>(provider != null);
            return loadedProviders.Contains(provider);
        }

        #endregion

        protected virtual void InitProvider(IServiceProvider provider)
        {
            Raise(new InitProviderEventArgs(provider, this));
            provider.Init();
        }

        protected override void GuardConstruct(string method)
        {
            if (registering)
            {
                throw new LogicException(
                    $"It is not allowed to make services or dependency injection in the {nameof(Register)} process,method:{method}");
            }

            base.GuardConstruct(method);
        }

        #region Event

        public void SetDispatcher(IEventDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.Instance<IEventDispatcher>(dispatcher);
        }

        public IEventDispatcher GetDispatcher()
        {
            return dispatcher;
        }

        private T Raise<T>(T args) where T : EventParam
        {
            if (!dispatchMapping.TryGetValue(args.GetType(), out string eventName))
            {
                throw new AssertException($"Assertion error: Undefined event {args}");
            }

            if (dispatcher == null)
            {
                return args;
            }

            dispatcher.Raise(eventName, this, args);
            return args;
        }

        #endregion
    }
}
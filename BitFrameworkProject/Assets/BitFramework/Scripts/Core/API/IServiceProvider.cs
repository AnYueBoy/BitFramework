namespace BitFramework.Core
{
    public interface IServiceProvider
    {
        /// <summary>
        /// 初始化服务提供者
        /// </summary>
        void Init();

        /// <summary>
        /// 注册服务
        /// </summary>
        void Register();
    }
}
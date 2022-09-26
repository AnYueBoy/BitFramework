using BitFramework.Core;
using BitFramework.Runtime;
using BitFramework.Util;

public class Main : Framework
{
    protected override void OnStartCompleted(IApplication application, StartCompletedEventArgs args)
    {
        // 框架完成
    }

    protected override IBootstrap[] GetBootstraps()
    {
        return Arr.Merge(base.GetBootstraps(), Bootstraps.GetBootstraps(this));
    }
}
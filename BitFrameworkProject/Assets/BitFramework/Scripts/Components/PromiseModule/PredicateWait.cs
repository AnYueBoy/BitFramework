using System;

namespace BitFramework.PromiseModule
{
    internal class PredicateWait
    {
        public IPendingPromise pendingPromise;
        public Func<float, bool> predicate;
        public float createTime;

        public float alreadyWaitTime;
    }
}
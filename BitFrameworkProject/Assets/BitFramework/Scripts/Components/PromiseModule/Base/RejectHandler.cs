using System;
using SException = System.Exception;

namespace BitFramework.PromiseModule
{
    public class RejectHandler
    {
        public Action<SException> callback;
        public IRejectable rejectable;
    }
}
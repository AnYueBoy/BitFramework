using SException = System.Exception;

namespace BitFramework.PromiseModule
{
    public interface IRejectable
    {
        void Reject(SException exception);
    }
}
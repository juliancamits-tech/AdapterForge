
namespace AdapterForge.Abstractions
{
    public abstract class AdapterForgeOperation<TRequest, TResponse>
        where TRequest : class
        where TResponse : class
    {
        protected abstract void Configure(OperationBuilder builder);
    }
}

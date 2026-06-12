namespace YAAF.Abstractions
{
    public abstract class AdapterForgeOperation<TRequest, TResponse>
    {
        protected abstract void Configure(OperationBuilder builder);
    }
}

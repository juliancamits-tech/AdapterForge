#pragma warning disable IDE0060 // Remove unused parameter
namespace YAAF.Abstractions
{

    public sealed class OperationBuilder
    {
        public OperationBuilder Group(string group)
            => this;

        public OperationBuilder Name(string name)
            => this;


        public OperationBuilder Description(string description)

            => this;

        public OperationBuilder Http(HttpMethod httpMethod)
            => this;

        public OperationBuilder Mcp()
            => this;

        //public OperationBuilder Grpc()=> this;
    }
}

#pragma warning restore IDE0060 // Remove unused parameter
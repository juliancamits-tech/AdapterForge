#pragma warning disable IDE0060 // Remove unused parameter
namespace AdapterForge.Abstractions
{

    public sealed class OperationBuilder
    {
        /// <summary>
        /// Define the Group name for related with other endpoints, think this like the clasic "ClientController.cs"
        /// </summary>
        public OperationBuilder Group(string group)
            => this;

        /// <summary>
        /// Name of the endpoint
        /// </summary>
        public OperationBuilder Name(string name)
            => this;

        /// <summary>
        /// Description used for documentation
        /// </summary>
        public OperationBuilder Description(string description)
            => this;

        /// <summary>
        /// Configuration for minimal API
        /// </summary>
        /// <param name="httpMethod">Verb of the method</param>
        /// <param name="authorization">null for don't use RequireAuthorization, string.Empty for any user or pass the Policy name</param>
        /// <param name="summary">Summary for OpenAPI </param>
        public OperationBuilder Http(HttpMethod httpMethod, string? authorization = null, string summary = "")
            => this;

        /// <summary>
        /// Configuration for MCP server
        /// </summary>
        /// <param name="isReadOnly">The endpoint just read information</param>
        /// <param name="isDestructive">The endpoint hard delete information</param>
        /// <param name="isIdempotent">Multiple calls with same parameters generate the same output</param>
        /// <param name="openWorld">The endpoint use thrid party service (web scraping, another api, etc)</param>
        /// <returns></returns>
        public OperationBuilder Mcp(bool isReadOnly = false, bool isDestructive = true, bool isIdempotent = false, bool openWorld = false)
            => this;

        //public OperationBuilder Grpc()=> this;
    }
}

#pragma warning restore IDE0060 // Remove unused parameter
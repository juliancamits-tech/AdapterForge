using Bogus;
using YAAF.Abstractions;

namespace TestApi
{
    public class GetClients : AdapterForgeOperation<GetClientsRequest, ClientsDto>
    {
        protected override void Configure(OperationBuilder builder)
        {
            builder
           .Group("Client")
           .Name("GetClients")
           .Description("Obtiene a todos los clientes")
           .Http(YAAF.Abstractions.HttpMethod.GET)
           .Mcp()
           ;
        }

        public ClientsDto Execute(GetClientsRequest request)
        {
            var dto = new ClientsDto();
            var f = new Faker();
            for (int x = 0; x < Random.Shared.Next(1, 10); x++)
                dto.Names.Add(f.Name.FullName());

            return dto;
        }

    }
    public class GetClientsRequest
    {
    }

    public class ClientsDto
    {
        public List<string> Names { get; set; } = [];
    }
}

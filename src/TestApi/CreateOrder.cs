using Bogus;
using Microsoft.Extensions.Caching.Memory;
using YAAF.Abstractions;

namespace TestApi
{
    public class CreateOrder : AdapterForgeOperation<CreateOrderRequest, CreateOrderDto>
    {
        protected override void Configure(OperationBuilder builder)
        {
            builder
                .Group("Order")
                .Name("CreateOrder")
                .Description("Crea una nueva orden recibiendo una lista de productos")
                .Http(YAAF.Abstractions.HttpMethod.POST)
                .Mcp()
                ;
        }

        public CreateOrderDto Execute(CreateOrderRequest request, IConfiguration configuration)
        {
            return new() { OrderId = Random.Shared.Next(0,100)};
        }
    }


    //This is here because i am lazy

    public class CreateOrderRequest
    {
        public List<string>? Products { get; set; }
    }

    public class CreateOrderDto
    {
        public int OrderId { get; set; }
    }
}

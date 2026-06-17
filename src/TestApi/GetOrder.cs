using AdapterForge.Abstractions;
using Bogus;
using Microsoft.Extensions.Caching.Memory;

namespace TestApi
{
    public sealed class GetOrder : AdapterForgeOperation<GetOrderRequest, OrderDto>
    {
        protected override void Configure(OperationBuilder builder)
        {
            builder
                .Group("Order")
                .Name("GetOrder")
                .Description("Obtiene una orden por su identificador")
                .Http(AdapterForge.Abstractions.HttpMethod.GET, summary: "Obtener order x identificador")
                .Mcp()
                ;
        }

        public OrderDto Execute(GetOrderRequest request, IMemoryCache memoryCache)
        {
            var order = new OrderDto
            {
                Id = request.OrderId
            };
            var number = Random.Shared.Next(0, 5);
            var f = new Faker();
            for (int x = 0; x < number; x++)
                order.Items.Add(f.Commerce.Product());

            return order;
        }
    }

    //This is here because i am lazy

    public class GetOrderRequest
    {
        public int OrderId { get; set; }
    }

    public class OrderDto
    {
        public int Id { get; set; }

        public List<string> Items { get; set; } = [];
    }
}

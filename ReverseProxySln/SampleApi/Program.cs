using SampleReverseProxy.Core.Classes;

var builder = WebApplication.CreateBuilder(args);

// Add required services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SimpleRestApi v1"));

// In-memory storage for products
var products = new List<ProductViewModel>();

app.MapGet("/", (HttpContext context) => $"This request is received on: MachineName: {Environment.MachineName}, ProcessId: {Environment.ProcessId}," +
$" RemoteIpAddress: {context.Connection.RemoteIpAddress}, LocalIpAddress: {context.Connection.LocalIpAddress}");

app.MapGet("/health", () => "This service is healhty.");

app.MapGet("/cache", (HttpContext context) => $"This endpoint demonstrates caching. The time now is {DateTime.Now.ToString("o")}" + $"This request is received on: MachineName: {Environment.MachineName}, ProcessId: {Environment.ProcessId}," +
$" RemoteIpAddress: {context.Connection.RemoteIpAddress}, LocalIpAddress: {context.Connection.LocalIpAddress}");

app.MapGet("/product", () => products);

app.MapPost("/product", (ProductViewModel product) =>
{
    product.Id = products.Count > 0 ? products.Max(p => p.Id) + 1 : 1;
    products.Add(product);
    return Results.Created($"/product/{product.Id}", product);
});

app.MapPut("/product/{id}", (int id, ProductViewModel updatedProduct) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);
    if (product == null)
    {
        return Results.NotFound();
    }

    product.Name = updatedProduct.Name;
    product.Price = updatedProduct.Price;
    return Results.NoContent();
});

app.MapDelete("/product/{id}", (int id) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);
    if (product == null)
    {
        return Results.NotFound();
    }

    products.Remove(product);
    return Results.NoContent();
});

app.Run();
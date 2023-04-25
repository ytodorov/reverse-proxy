using ReverseProxy.Core.Middlewares;
using ReverseProxy.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLoadBalancer();

var app = builder.Build();

app.UseMiddleware<LoadBalancerMiddleware>();

app.Run();
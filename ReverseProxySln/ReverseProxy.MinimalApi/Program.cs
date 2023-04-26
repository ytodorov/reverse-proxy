using ReverseProxy.Core.Classes;
using ReverseProxy.Core.Interfaces;
using ReverseProxy.Core.Middlewares;
using ReverseProxy.Core.Extensions;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLoadBalancer();

var app = builder.Build();

app.UseMiddleware<LoadBalancerMiddleware>();

app.Run();

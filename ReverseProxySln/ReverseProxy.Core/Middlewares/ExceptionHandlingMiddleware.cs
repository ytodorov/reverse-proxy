using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReverseProxy.Core.Middlewares
{
    /// <summary>
    /// This class is not used.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseExceptionHandler(options =>
            {
                options.Run(async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json";

                    var error = context.Features.Get<IExceptionHandlerFeature>();
                    if (error != null)
                    {
                        var ex = error.Error;
                        var result = System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message });
                        await context.Response.WriteAsync(result);
                    }
                });
            });
        }
    }
}

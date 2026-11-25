using EticaretMicroservice.Shared.Middlewares;
using Microsoft.AspNetCore.Builder;

namespace EticaretMicroservice.Shared.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseCustomExceptionHandler(this IApplicationBuilder app)
        {
            return app.UseMiddleware<CustomExceptionHandlerMiddleware>();
        }
    }
}

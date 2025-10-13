using System.Reflection;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Swagger;

namespace WebApi.MinimalApi.Samples;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerGeneration(this IServiceCollection services)
    {
        return services.AddSwaggerGen(options =>
        {
            // Создаем документ с описанием API
            options.SwaggerDoc("web-api", new OpenApiInfo
            {
                Title = "Web API",
                Version = "0.1",
            });

            // Конфигурируем Swashbuckle, чтобы использовались Xml Documentation Comments
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            options.IncludeXmlComments(xmlPath);

            // Конфигурируем Swashbuckle, чтобы работали атрибуты
            options.EnableAnnotations();
        });
    }

    public static void UseSwaggerWithUI(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/web-api/swagger.json", "Web API");
            c.RoutePrefix = string.Empty;
        });
    }

    public static string GetSwaggerDocument(this IHost host, string documentName)
    {
        var sw = (ISwaggerProvider)host.Services.GetService(typeof(ISwaggerProvider));
        var doc = sw.GetSwagger(documentName);

        return JsonConvert.SerializeObject(
            doc,
            Formatting.Indented,
            new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
            });
    }
}
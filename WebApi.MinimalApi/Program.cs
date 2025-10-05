using System.Reflection;
using Microsoft.AspNetCore.Mvc.Formatters;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
        options.SuppressMapClientErrors = true;
    });
builder.Services.AddControllers(options =>
{
    // Этот OutputFormatter позволяет возвращать данные в XML, если требуется.
    options.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());
    // Эта настройка позволяет отвечать кодом 406 Not Acceptable на запросы неизвестных форматов.
    options.ReturnHttpNotAcceptable = true;
    // Эта настройка приводит к игнорированию заголовка Accept, когда он содержит */*
    // Здесь она нужна, чтобы в этом случае ответ возвращался в формате JSON
    options.RespectBrowserAcceptHeader = true;
});

builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>()
    .AddAutoMapper(
        cfg =>
        {
            cfg.CreateMap<UserEntity, UserDto>().ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));
        }, Array.Empty<Assembly>());
var app = builder.Build();

app.MapControllers();

app.Run();
using System.Text.Json;
using System.Text.Json.Serialization;
using Api;
using Application;
using Host.Middleware;
using Host.Transformers;
using Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<DocumentTransformer>();
    options.AddSchemaTransformer<NullableSchemaTransformer>();
});

builder
    .AddApi()
    .AddApplication()
    .AddInfrastructure();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();
app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();
app.UseMiddleware<GlobalExceptionMiddleware>();

app
    .MapApi()
    .MapApplication()
    .MapInfrastructure();

app.Run();

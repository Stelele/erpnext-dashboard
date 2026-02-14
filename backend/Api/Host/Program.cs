using Api;
using Application;
using Host.Middleware;
using Host.Transformers;
using Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<DocumentTransformer>();
});

builder
    .AddApi()
    .AddApplication()
    .AddInfrastructure();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();
app.UseMiddleware<GlobalExceptionMiddleware>();

app
    .MapApi()
    .MapApplication()
    .MapInfrastructure();

app.Run();

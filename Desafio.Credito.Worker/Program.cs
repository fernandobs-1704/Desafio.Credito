using Desafio.Credito.Domain.Interfaces;
using Desafio.Credito.Infrastructure;
using Desafio.Credito.Infrastructure.Data;
using Desafio.Credito.Infrastructure.Repositories;
using Desafio.Credito.Worker;
using Desafio.Credito.Worker.Clients;
using Desafio.Credito.Worker.Configurations;
using Desafio.Credito.Worker.Consumers;
using Desafio.Credito.Worker.Services;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

// CONFIG
builder.Services.Configure<AppSettings>(
    builder.Configuration.GetSection("AppSettings"));

// DB
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// REPOSITORY
builder.Services.AddScoped<IEvolucaoContratoRepository, EvolucaoContratoRepository>();

// HTTP CLIENT (API)
builder.Services.AddHttpClient<IPriceApiClient, PriceApiClient>(client =>
{
    var url = builder.Configuration["AppSettings:BaseUrl"];
    client.BaseAddress = new Uri(url);
});

// SERVICE BUS
builder.Services.AddSingleton<ServiceBusConsumer>();

// EVENT HUB
builder.Services.AddSingleton<ILogEventService, LogEventService>();

// WORKER
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
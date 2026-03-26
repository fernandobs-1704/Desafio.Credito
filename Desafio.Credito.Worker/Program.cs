using Desafio.Credito.Worker;
using Desafio.Credito.Worker.Configurations;
using Desafio.Credito.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<AppSettings>(
    builder.Configuration.GetSection("AppSettings"));

builder.Services.AddHttpClient<PriceApiClient>((serviceProvider, client) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["AppSettings:BaseUrl"];

    client.BaseAddress = new Uri(baseUrl!);
});

builder.Services.AddHostedService<Worker>();

builder.Services.Configure<ServiceBusSettings>(
    builder.Configuration.GetSection("ServiceBus"));
builder.Services.AddSingleton<ServiceBusConsumer>();

var host = builder.Build();
host.Run();
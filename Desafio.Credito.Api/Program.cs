using Desafio.Credito.Domain.Interfaces;
using Desafio.Credito.Domain.Services;
using Desafio.Credito.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using Desafio.Credito.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<ICalculadoraPriceService, CalculadoraPriceService>();
builder.Services.AddScoped<IEvolucaoContratoRepository, EvolucaoContratoRepository>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.Run();
using Desafio.Credito.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Desafio.Credito.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<EvolucaoContrato> EvolucoesContrato { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<EvolucaoContrato>(entity =>
        {
            entity.ToTable("EvolucaoContrato");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Dia).IsRequired();

            entity.Property(x => x.Prestacao)
                .HasColumnType("decimal(18,2)");

            entity.Property(x => x.JurosPeriodo)
                .HasColumnType("decimal(18,2)");

            entity.Property(x => x.Amortizacao)
                .HasColumnType("decimal(18,2)");

            entity.Property(x => x.SaldoAposPagar)
                .HasColumnType("decimal(18,2)");

            entity.Property(x => x.DataProcessamento)
                .IsRequired();
        });
    }
}
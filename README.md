# 💰 Desafio Técnico – Cálculo de Crédito (Tabela Price)
<p align="center">

![.NET](https://img.shields.io/badge/.NET-8-blue?logo=dotnet)
![C#](https://img.shields.io/badge/C%23-Programming-green?logo=csharp)
![SQL Server](https://img.shields.io/badge/SQL%20Server-Database-red?logo=microsoftsqlserver)
![EF Core](https://img.shields.io/badge/EF%20Core-ORM-purple)
![Azure](https://img.shields.io/badge/Azure-Cloud-blue?logo=microsoftazure)
![Service Bus](https://img.shields.io/badge/Service%20Bus-Messaging-orange)
![Event Hub](https://img.shields.io/badge/Event%20Hub-Streaming-yellow)
![Architecture](https://img.shields.io/badge/Clean%20Architecture-Design-lightgrey)
![Status](https://img.shields.io/badge/status-complete-success)

</p>

## 📌 Visão Geral

Esta aplicação foi desenvolvida em **.NET 8** com o objetivo de processar contratos de crédito utilizando o sistema de amortização **Tabela Price**, com **evolução diária do contrato**.

A solução segue uma arquitetura em camadas e processamento assíncrono baseado em mensageria.

---

## 🧱 Arquitetura da Solução

A solução está organizada nos seguintes projetos:

- Desafio.Credito.Api → API de cálculo
- Desafio.Credito.Domain → Regras de negócio
- Desafio.Credito.Infrastructure → Persistência (SQL Server)
- Desafio.Credito.Shared → DTOs
- Desafio.Credito.Tests → Testes unitários
- Desafio.Credito.Worker → Processamento assíncrono

---

## ⚙️ Tecnologias Utilizadas

- .NET 8
- Entity Framework Core
- SQL Server
- Azure Service Bus
- Azure Event Hub
- xUnit
- Moq
- FluentAssertion
---

## 🔄 Fluxo de Processamento

Fila → Worker → API → Banco → Event Hub

---

## 📊 Regra de Negócio

- Sistema: Price (parcelas fixas)
- Capitalização: diária
- Pagamento: a cada 30 dias
- Evolução: 1 registro por dia

Exemplo:

{
  "valorEmprestimo": 10000,
  "taxaJurosMensal": 1.8,
  "prazoMeses": 30
}

Resultado esperado:
30 meses × 30 dias = 900 registros

---

## 🚀 Execução

### 1. Restaurar pacotes
dotnet restore

### 2. Criar banco
dotnet ef database update --project Desafio.Credito.Infrastructure --startup-project Desafio.Credito.Worker

### 3. Executar aplicação

Basta executar a solução (F5 no Visual Studio).

A aplicação está configurada para iniciar:
- API
- Worker

automaticamente em conjunto.

---

## 📨 Teste

Enviar mensagem JSON para a fila:

{
  "valorEmprestimo": 10000,
  "taxaJurosMensal": 1.8,
  "prazoMeses": 30
}

---

## 🔍 Validação

### Banco
SELECT COUNT(*) FROM EvolucaoContrato;

Esperado:
900 registros

---

## ✅ Testes Unitários

A solução possui um projeto dedicado de testes unitários:

- Desafio.Credito.Tests

Os testes foram criados para validar os principais comportamentos da aplicação nas camadas mais relevantes da solução:

### Cobertura atual dos testes

- **Domínio**
  - validação da lógica de cálculo da Tabela Price;
  - geração correta da quantidade de registros por prazo;
  - consistência da evolução diária;
  - amortização apenas a cada 30 dias;
  - saldo final zerado ao término do contrato;
  - comportamento da prestação fixa, considerando eventual ajuste residual na última parcela.

- **API**
  - validação de payload inválido;
  - retorno `BadRequest` para dados inconsistentes;
  - retorno `OK` para payload válido;
  - garantia de chamada correta da camada de serviço.

- **Worker**
  - processamento de mensagem válida;
  - tratamento de mensagem inválida;
  - comportamento em falha de API;
  - comportamento em falha de persistência;
  - validação do registro do payload original nos logs de processamento.

### Como executar os testes

```bash
dotnet test

---

## 🧠 Destaques Técnicos

- Separação clara entre API e Worker
- Processamento assíncrono
- Logs estruturados (Event Hub)
- Arquitetura escalável

---


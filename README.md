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

## 🖥️ Pré-requisitos

Para execução da aplicação em ambiente local, é necessário possuir:

- .NET SDK 8 instalado
- SQL Server (local ou remoto)
- Visual Studio 2022 (ou superior) ou VS Code
- Acesso aos serviços Azure utilizados (já configurados no projeto)

### Verificação do .NET

dotnet --version

---

## 🚀 Execução

### 1. Restaurar pacotes
dotnet restore

### 2. Criar banco
dotnet ef database update --project Desafio.Credito.Infrastructure --startup-project Desafio.Credito.Worker

### 3. Executar aplicação

Configurar a solução para "Vários projetos de inicialização" através do menu "propriedades" e escolher opção "iniciar" na Api e Worker. 
Executar a solução (F5 no Visual Studio).

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

### 📊 Exemplo de evolução do contrato (amostra)

Abaixo uma amostra real da evolução do contrato ao longo do tempo:

#### 🔹 Início do contrato

| Dia | Prestação | Juros Período | Amortização | Saldo Após Pagar |
|-----|----------|---------------|-------------|------------------|
| 1   | 434.31   | 5.95          | 0.00        | 10005.95         |
| 2   | 434.31   | 11.90         | 0.00        | 10011.90         |
| 3   | 434.31   | 17.86         | 0.00        | 10017.86         |

---

#### 🔹 Primeiro ciclo de pagamento (30 dias)

| Dia | Prestação | Juros Período | Amortização | Saldo Após Pagar |
|-----|----------|---------------|-------------|------------------|
| 29  | 434.31   | 173.95        | 0.00        | 10173.95         |
| 30  | 434.31   | 180.00        | 254.31      | 9745.69          |
| 31  | 434.31   | 5.80          | 0.00        | 9751.49          |

---

#### 🔹 Final do contrato

| Dia | Prestação | Juros Período | Amortização | Saldo Após Pagar |
|-----|----------|---------------|-------------|------------------|
| 898 | 434.31   | 7.20          | 0.00        | 434.02           |
| 899 | 434.31   | 7.46          | 0.00        | 434.28           |
| 900 | 434.54   | 7.72          | 426.82      | 0.00             |

---

### 📌 Observações

- Os juros são **acumulados diariamente**
- A amortização ocorre **apenas a cada 30 dias**
- A prestação permanece **praticamente fixa**, com ajuste final para zerar o saldo
- O saldo cresce diariamente até o momento do pagamento mensal

---
  
## 🧠 Destaques Técnicos

- Separação clara entre API e Worker
- Processamento assíncrono
- Logs estruturados (Event Hub)
- Arquitetura escalável
- Cobertura de testes para domínio, API e Worker

---


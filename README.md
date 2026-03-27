# 💰 Desafio Técnico – Cálculo de Crédito (Tabela Price)

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
- Desafio.Credito.Worker → Processamento assíncrono

---

## ⚙️ Tecnologias Utilizadas

- .NET 8
- Entity Framework Core
- SQL Server
- Azure Service Bus
- Azure Event Hub

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

## 🧠 Destaques Técnicos

- Separação clara entre API e Worker
- Processamento assíncrono
- Logs estruturados (Event Hub)
- Arquitetura escalável

---

## 👨‍💻 Autor

Desenvolvido para avaliação técnica – Coordenação de TI

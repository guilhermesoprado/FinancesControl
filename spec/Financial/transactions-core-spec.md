# Spec - Transactions Core

## 1. Objetivo da Spec

Este documento detalha o desenvolvimento do terceiro modulo da Fase 2:

- `Transactions Core`

O objetivo deste modulo e materializar o nucleo transacional basico do projeto com `Transaction` como entidade central.

Nesta fase, o modulo deve permitir apenas:

- criar `income`
- criar `expense`
- criar `transfer`
- listar transacoes por periodo com filtros basicos

## 2. Escopo Fechado do Modulo

### 2.1 Entra no escopo

- entidade central `Transaction`
- tipos controlados:
  - `income`
  - `expense`
  - `transfer`
- status controlado no dominio para separar o conceito de realizado do previsto
- endpoints explicitos por intencao:
  - `POST /api/v1/transactions/income`
  - `POST /api/v1/transactions/expense`
  - `POST /api/v1/transactions/transfer`
- endpoint de listagem:
  - `GET /api/v1/transactions`
- atualizacao de saldo visivel em `FinancialAccount` ao registrar a transacao
- tela inicial de listagem
- fluxo de nova receita
- fluxo de nova despesa
- fluxo de nova transferencia
- filtros basicos por periodo, tipo e conta

### 2.2 Nao entra no escopo

- editar transacao
- cancelar transacao
- excluir transacao
- cartao de credito
- fatura
- pagamento de fatura
- parcelamento
- recorrencia
- dashboard analitico
- IA
- conciliacao contabil

## 3. Decisoes Ja Fechadas

Estas decisoes passam a valer como regra para a implementacao:

- `Transaction` continua como entidade central do nucleo financeiro
- `income` e `expense` nao viram entidades separadas
- `transfer` continua no mesmo agregado, mas com regra propria
- transferencia move dinheiro entre contas do proprio usuario
- transferencia afeta saldo de origem e destino, mas nao contamina leitura de receita x despesa
- `Financial Accounts` continua sendo a estrutura de origem ou destino do dinheiro
- `Transaction Categories` continua sendo classificacao semantica para `income` e `expense`
- `transfer` nao deve depender de categoria nesta fase
- datas de negocio devem usar `date`
- a listagem inicial precisa ser simples e operacional antes de qualquer sofisticacao analitica

## 4. Papel do Modulo no Projeto

Este modulo resolve o problema central da Fase 2.

Sem `Transaction`, o sistema ja possui autenticacao, contas e categorias, mas ainda nao registra o movimento real do dinheiro.

Em termos de projeto, este modulo existe para:

- materializar a movimentacao financeira do usuario
- conectar contas a entradas, saidas e transferencias
- alimentar o saldo visivel das contas dentro da Fase 2
- preparar a base para filtros, extrato e modulos futuros

## 5. Modelo de Dados Esperado

A entidade `Transaction` deve representar um lancamento financeiro central.

Campos esperados nesta fase:

- `Id`
- `UserId`
- `Type`
- `Status`
- `Amount`
- `OccurredOn`
- `Description` opcional
- `FinancialAccountId` para `income` e `expense`
- `TransactionCategoryId` para `income` e `expense`
- `SourceFinancialAccountId` para `transfer`
- `DestinationFinancialAccountId` para `transfer`
- `CreatedAtUtc`
- `UpdatedAtUtc`

## 6. Invariantes do Modulo

Estas regras nao podem ser quebradas:

- toda transacao pertence a um unico usuario
- `Amount` deve ser maior que zero
- `OccurredOn` e obrigatoria
- `income` exige conta e categoria do tipo `income`
- `expense` exige conta e categoria do tipo `expense`
- `transfer` exige conta de origem e conta de destino
- conta de origem e conta de destino nao podem ser iguais
- `transfer` nao usa categoria nesta fase
- ownership deve ser respeitado em contas e categorias
- a listagem deve retornar apenas transacoes do usuario autenticado

## 7. Recorte Exato de Entrada no Modulo

O ponto profissional de entrada em `Transactions Core` nesta janela e:

1. fechar a spec do modulo
2. criar o backend minimo e coerente do nucleo
3. validar que as mutacoes de saldo funcionam no nivel de aplicacao
4. so depois subir o frontend inicial do modulo

Este recorte existe para evitar dois erros:

- construir uma tela antes de existir contrato estavel
- inflar o dominio com preocupacoes de fases futuras

## 8. Ordem Profissional de Desenvolvimento

A implementacao deste modulo deve seguir esta ordem:

1. spec e contrato do modulo
2. enums e entidade de dominio
3. ajuste controlado de `FinancialAccount` para receber mutacoes de saldo
4. contratos de aplicacao e servico
5. repositorios e mapeamento EF Core
6. migration
7. endpoints da API
8. validacao backend
9. tipos e services de frontend
10. tela inicial com filtros e fluxos de criacao
11. validacao integrada

Motivo:

- `Transactions Core` e o primeiro modulo que realmente altera o dinheiro visivel
- por isso a consistencia de dominio precisa vir antes da interface
- a UX so deve nascer depois que a regra basica estiver fechada

## 9. Microtarefas Backend

## 9.1 Criar enum `TransactionType`

Saida esperada:

- `income`
- `expense`
- `transfer`

## 9.2 Criar enum `TransactionStatus`

Saida esperada:

- um estado inicial de transacao realizada
- estrutura pronta para separar realizado e previsto sem abrir recorrencia agora

## 9.3 Criar entidade `Transaction`

O que deve resolver:

- materializar o lancamento central do sistema
- garantir combinacao de campos coerente por tipo

## 9.4 Ajustar `FinancialAccount` para suportar mutacao de saldo

O que deve resolver:

- aplicar entrada, saida e transferencia sobre o saldo visivel
- manter a Fase 2 operacional sem abrir conciliacao completa

## 9.5 Criar contrato de persistencia `ITransactionRepository`

Operacoes minimas esperadas:

- adicionar transacao
- listar por usuario e periodo

## 9.6 Expandir contratos de repositorio de contas e categorias quando necessario

O que deve resolver:

- validar ownership
- carregar conta ou categoria exigida pela transacao

## 9.7 Criar servico de aplicacao de transacoes

Casos minimos:

- `RegisterIncomeTransaction`
- `RegisterExpenseTransaction`
- `RegisterTransferTransaction`
- `GetTransactionsByPeriod`

## 9.8 Criar configuracao EF Core e `DbSet<Transaction>`

O que deve resolver:

- salvar e consultar transacoes com filtros basicos

## 9.9 Criar migration do modulo

O que deve resolver:

- materializar a tabela `transactions`

## 9.10 Criar DTOs de request e response da API

Requests minimos:

- criacao de `income`
- criacao de `expense`
- criacao de `transfer`

Response minimo:

- dados suficientes para a listagem e para atualizacao local da tela

## 9.11 Criar controller `TransactionsController`

Rotas minimas:

- `POST /api/v1/transactions/income`
- `POST /api/v1/transactions/expense`
- `POST /api/v1/transactions/transfer`
- `GET /api/v1/transactions`

## 9.12 Validar backend

Cenarios minimos:

- criar receita e somar saldo da conta
- criar despesa e reduzir saldo da conta
- criar transferencia e mover saldo entre duas contas
- listar por periodo
- filtrar por tipo
- filtrar por conta

## 10. Microtarefas Frontend

## 10.1 Criar tipos de frontend do modulo

## 10.2 Criar service de `Transactions`

Operacoes minimas:

- `createIncomeTransaction`
- `createExpenseTransaction`
- `createTransferTransaction`
- `getTransactions`

## 10.3 Criar a tela `Transactions`

Blocos minimos:

- cabecalho
- resumo curto
- filtros
- lista
- estado vazio
- estado de erro
- acoes para nova receita, nova despesa e nova transferencia

## 10.4 Consumir `Financial Accounts` e `Transaction Categories`

O que deve resolver:

- preencher selects de conta e categoria sem duplicar verdade de negocio

## 10.5 Implementar formulario de receita

Campos minimos:

- conta
- categoria `income`
- valor
- data
- descricao opcional

## 10.6 Implementar formulario de despesa

Campos minimos:

- conta
- categoria `expense`
- valor
- data
- descricao opcional

## 10.7 Implementar formulario de transferencia

Campos minimos:

- conta de origem
- conta de destino
- valor
- data
- descricao opcional

## 10.8 Implementar listagem com filtros basicos

Filtros minimos:

- `from`
- `to`
- `type`
- `financialAccountId`

## 11. Criterios de Pronto

### Backend pronto quando

- criacao de `income`, `expense` e `transfer` funciona
- ownership de conta e categoria esta protegido
- saldo visivel das contas e atualizado
- listagem por periodo funciona

### Frontend pronto quando

- tela consome backend real
- filtros basicos funcionam
- formularios criam transacoes reais
- a lista atualiza sem recarga manual

### Modulo pronto para a Fase 2 quando

- o usuario autenticado consegue registrar receita
- o usuario autenticado consegue registrar despesa
- o usuario autenticado consegue registrar transferencia entre contas proprias
- o usuario autenticado consegue listar transacoes por periodo com filtros basicos

## 12. O Que Nao Deve Ser Feito Neste Modulo

- misturar `Transactions Core` com cartao
- abrir regras de fatura
- abrir parcelamento
- abrir recorrencia
- transformar a tela inicial em dashboard
- tentar resolver conciliacao completa de saldo
- reabrir shell autenticada sem evidencia real

## 13. Resultado Esperado

Ao final deste modulo, o sistema deve permitir que um usuario autenticado:

- registre receitas
- registre despesas
- registre transferencias entre contas proprias
- veja essas movimentacoes em uma lista por periodo
- filtre o extrato inicial por recorte basico

Quando isso estiver funcionando com backend real e validacao minima integrada, `Transactions Core` podera ser tratado como implementado dentro da Fase 2.

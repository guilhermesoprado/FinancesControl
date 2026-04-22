# Spec - Roadmap Completo do Projeto

## 1. Objetivo da Spec

Este documento formaliza o fluxo completo do projeto do inicio ao fim.

Ele existe para responder, sem ambiguidade:

- quais fases existem no projeto
- em que ordem elas devem acontecer
- o que cada fase resolve
- qual e o estado atual de cada fase
- qual e o proximo passo imediato
- como um agente orquestrador deve decidir o fluxo do trabalho

Esta spec e o documento mestre do roadmap.

## 2. Regra de Leitura da Documentacao

Qualquer pessoa ou agente entrando no projeto deve ler a documentacao nesta ordem:

1. `README.md`
2. `spec/Roadmap/project-roadmap-spec.md`
3. `spec/Roadmap/phase-0-foundation-spec.md`
4. `spec/Roadmap/phase-2-operational-finance-spec.md`
5. `spec/Roadmap/phase-3-credit-and-invoice-spec.md`
6. `spec/Roadmap/phase-4-financial-planning-spec.md`
7. `spec/Roadmap/phase-5-control-and-governance-spec.md`
8. `spec/Roadmap/phase-6-design-evolution-spec.md`
9. `spec/Roadmap/phase-7-intelligence-and-finalization-spec.md`
10. `spec/Financial/phase-2-orchestration-roadmap.md`
11. `spec/Financial/financial-accounts-spec.md`
12. `spec/Financial/transaction-categories-spec.md`
13. `spec/Financial/transactions-core-spec.md`
14. `spec/Financial/financial-overview-spec.md`

Motivo:

- o `README.md` explica o repositorio e o estado publico atual
- esta spec explica o fluxo integral do produto
- as specs por fase explicam o recorte de cada etapa do roadmap
- as specs de modulo explicam a execucao detalhada da Fase 2

## 3. Regra de Numeracao das Fases

A numeracao publica deste projeto segue a convencao historica ja usada nos materiais existentes.

Por isso, esta documentacao formaliza:

- `Fase 0`
- `Fase 2`
- `Fase 3`
- `Fase 4`
- `Fase 5`
- `Fase 6`
- `Fase 7`

### 3.1 O que significa a ausencia da Fase 1

A ausencia da `Fase 1` nesta camada publica nao significa erro de documentacao.

Ela significa apenas que:

- o repositorio publico atual parte de uma fundacao previa
- a numeracao operacional que ja existia no projeto comeca a camada financeira principal em `Fase 2`
- esta spec preserva a numeracao ja usada para nao quebrar a linguagem do projeto

## 4. Linha Mestra do Produto

O fluxo limpo do projeto e:

1. construir a fundacao do produto
2. construir a base operacional financeira
3. expandir para credito e fatura
4. expandir para planejamento financeiro
5. reforcar controle, confiabilidade e governanca
6. evoluir design e experiencia sem quebrar o que ja funciona
7. fechar o produto com inteligencia, leitura gerencial e prontidao final

## 5. Sequencia Oficial das Fases

A ordem oficial do projeto e:

1. `Fase 0 - Foundation`
2. `Fase 2 - Operational Finance`
3. `Fase 3 - Credit and Invoice`
4. `Fase 4 - Financial Planning`
5. `Fase 5 - Control and Governance`
6. `Fase 6 - Design Evolution`
7. `Fase 7 - Intelligence and Finalization`

Essa e a sequencia que um agente orquestrador deve seguir.

## 6. Estado Atual do Projeto

### 6.1 Fase 0

Status:

- concluida para o escopo atual do repositorio

### 6.2 Fase 2

Status:

- concluida para o escopo atual

Modulos fechados dentro da Fase 2:

- `Financial Accounts`
- `Transaction Categories`
- `Transactions Core`
- `Financial Overview`

### 6.3 Fase 3

Status:

- concluida para o escopo atual
- validada no nucleo avancado de credito

### 6.4 Fase 4

Status:

- formalizada
- aberta oficialmente
- proxima fase ativa do projeto

### 6.5 Fase 5

Status:

- nao iniciada

### 6.6 Fase 6

Status:

- nao iniciada

### 6.7 Fase 7

Status:

- nao iniciada

## 7. Papel de Cada Fase

### 7.1 Fase 0

Papel:

- colocar o produto de pe
- garantir autenticacao, shell, backend, frontend e infraestrutura local

### 7.2 Fase 2

Papel:

- criar a primeira operacao financeira real do sistema
- estruturar contas, categorias, transacoes e primeira camada de leitura

### 7.3 Fase 3

Papel:

- fechar o arco operacional de credito, fatura, parcelamento, ajustes e encargos simples

### 7.4 Fase 4

Papel:

- levar o sistema do registro do passado para o planejamento do futuro

### 7.5 Fase 5

Papel:

- aumentar confiabilidade, rastreabilidade e seguranca operacional

### 7.6 Fase 6

Papel:

- evoluir design, navegacao e experiencia sem quebrar o comportamento ja validado

### 7.7 Fase 7

Papel:

- levar o produto ao estado de leitura gerencial, inteligencia e fechamento do ciclo do projeto

## 8. Grafo de Dependencias Entre Fases

O grafo principal e:

- `Fase 0` -> `Fase 2`
- `Fase 2` -> `Fase 3`
- `Fase 2` -> `Fase 4`
- `Fase 3` -> `Fase 5`
- `Fase 4` -> `Fase 5`
- `Fase 5` -> `Fase 6`
- `Fase 6` -> `Fase 7`

Regras derivadas:

- `Fase 2` nao pode ser pulada
- `Fase 3` nao deve abrir antes de o nucleo operacional da `Fase 2` estar estavel
- `Fase 4` nao deve abrir antes de existir uma base transacional confiavel
- `Fase 6` nao deve ser usada para reinventar o produto do zero
- `Fase 7` so deve acontecer depois do produto estar funcional e visualmente consolidado

## 9. Como um Agente Orquestrador Deve Operar

O agente orquestrador deve seguir estas regras.

### 9.1 Primeiro diagnosticar o estado atual

Cada fase deve ser classificada como:

- nao iniciada
- em implementacao
- implementada mas nao validada
- validada
- concluida para o escopo atual

### 9.2 Nunca abrir a fase seguinte antes de fechar a atual

Antes de promover o projeto para a fase seguinte, o orquestrador deve garantir:

- contrato de backend estavel
- contrato de frontend estavel
- validacao integrada
- checks automatizados quando fizer sentido
- documentacao atualizada

### 9.3 Preservar o arco original do produto

Quando houver conflito entre:

- fechar a sequencia natural do produto
- abrir cedo um dominio mais complexo

o orquestrador deve preferir fechar a sequencia natural.

### 9.4 Proteger a fase de design contra regressao funcional

Quando chegar a `Fase 6`, o orquestrador deve operar com a regra:

- melhorar visual, navegacao e consistencia
- sem quebrar contratos, fluxos e comportamentos ja validados

## 10. O Que Deve Ser Feito Agora

Dado o estado atual do projeto, a proxima acao correta e:

1. manter a `Fase 3` fechada como base estavel do dominio de credito
2. abrir a execucao da `Fase 4`
3. iniciar pelo modulo `Scheduled Entries / Planned Transactions`
4. preservar `Fase 2` e `Fase 3` como referencias congeladas dos arcos ja validados

## 11. Criterio de Encerramento do Projeto

O projeto pode ser tratado como encerrado quando:

- todas as fases previstas estiverem completas para seu escopo
- o produto possuir operacao financeira real e confiavel
- o produto possuir leitura clara e utilizavel do dinheiro
- o produto possuir experiencia visual consistente e madura
- o produto possuir documentacao suficiente para manutencao por novos agentes e pessoas

## 12. Resumo Final do Comeco ao Fim

A leitura correta do projeto e:

- `Fase 0` constroi a base
- `Fase 2` cria a operacao financeira e sua primeira camada de leitura
- `Fase 3` abre, consolida e valida o dominio de credito e fatura
- `Fase 4` abre planejamento financeiro a partir da base operacional ja estabilizada
- `Fase 5` reforca controle e governanca
- `Fase 6` melhora design e experiencia sem quebrar o que ja funciona
- `Fase 7` fecha o produto com inteligencia, leitura gerencial e maturidade final

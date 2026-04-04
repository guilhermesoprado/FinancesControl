# 18 - Mapa de Progresso e Governanca da Execucao

## 1. Objetivo do Documento

Este documento foi criado para resolver uma lacuna operacional importante do projeto:

- sabermos exatamente o que ja foi feito
- diferenciarmos estrutura criada de funcionalidade realmente entregue
- identificarmos o que ainda falta fazer
- mantermos um mecanismo confiavel de acompanhamento conforme a execucao avancar

Em projeto longo, esse controle nao e detalhe administrativo.
Ele protege a ordem do roadmap e reduz perda de contexto entre janelas, agentes e etapas.

## 2. Regra de Ouro Deste Mapa

Nada deve ser marcado como concluido apenas porque:

- a pasta existe
- a classe foi criada
- o projeto compila
- a intencao esta documentada

Algo so pode avancar de status quando houver evidencia verificavel.

Exemplos de evidencia:

- endpoint funcional
- migration aplicada
- fluxo exercitado no Swagger
- tela consumindo backend real
- validacao integrada basica

## 3. Modelo Oficial de Status

Para evitar ambiguidade, este projeto passa a usar os seguintes status:

### 3.1 `documentado`

Significa:

- o item esta descrito nos artefatos do projeto
- escopo e papel estao claros

### 3.2 `estruturado`

Significa:

- a base tecnica foi criada no repositorio
- existem projetos, pastas, arquivos ou scaffolds iniciais

### 3.3 `implementado`

Significa:

- o comportamento principal foi codificado
- o contrato funcional existe no codigo

### 3.4 `validado`

Significa:

- o comportamento foi exercitado com evidencia real
- a funcionalidade provou funcionar no contexto esperado

### 3.5 `concluido`

Significa:

- o item atingiu o criterio de pronto definido para sua fase ou modulo
- foi aceito como fechado dentro do escopo combinado

### 3.6 `bloqueado`

Significa:

- existe dependencia, ambiguidade ou falha impedindo o avanco seguro

## 4. Estrutura Oficial de Acompanhamento

O progresso deve ser registrado em quatro niveis:

- `Fase`
- `Modulo`
- `Tarefa`
- `Subtarefa`

Cada nivel precisa ter:

- nome
- status
- dependencias
- evidencia
- observacoes

## 5. Estado Atual Oficial do Projeto

Este estado foi consolidado cruzando:

- documentos oficiais do projeto
- estrutura real do repositorio
- codigo efetivamente encontrado

## 6. Fase 0 - Fundacao Tecnica do Repositorio

Status oficial:

- `concluido`

Justificativa:

- `backend` criado
- `frontend` criado
- solucao `.sln` criada
- projetos backend criados
- app Next.js criado
- estrutura inicial de pastas criada

Observacao importante:

- no frontend, a fundacao estrutural existe
- isso nao significa que o shell autenticado ou as telas ja estejam implementados

## 7. Fase 1 - Fundacao do Backend e Autenticacao

Status oficial:

- `concluido`

Justificativa:

- autenticacao backend implementada
- migration inicial criada
- PostgreSQL em container funcionando
- Swagger com `Authorize` funcionando
- fluxo real de `register`, `login` e `me` validado

### 7.1 Backend de autenticacao

Status:

- `concluido`

Evidencias:

- entidade `User`
- enum `UserStatus`
- `AuthService`
- `IUserRepository`
- `IPasswordHasher`
- `ITokenService`
- `IDateTimeProvider`
- `FinanceManagerDbContext`
- `UserConfiguration`
- `UserRepository`
- `JwtTokenService`
- `Pbkdf2PasswordHasher`
- `AuthController`
- requests e responses de auth
- migration `InitialAuthentication`

### 7.2 Frontend de autenticacao

Status:

- `validado`

Justificativa:

- `login` implementado com backend real
- `register` implementado com backend real
- sessao persistida em `localStorage` e cookie
- hidratacao por `GET /auth/me`
- protecao de rotas por `proxy.ts`
- redirecionamento da raiz e pos-login validado funcionalmente no navegador

## 8. Fase 2 - Nucleo Transacional Basico

Status oficial:
- `concluido`

Justificativa:

- escopo, ordem e criterios de pronto foram consolidados
- `Financial Accounts` foi implementado e validado ponta a ponta
- `Transaction Categories` foi implementado e validado funcionalmente
- a shell autenticada foi estabilizada antes do inicio de `Transactions Core`
- os tres modulos obrigatorios da Fase 2 foram implementados e validados com evidencia real

### 8.0 Decisao operacional vigente da fase

Fica oficialmente registrado que, durante o estado atual do projeto, o redirecionamento principal apos autenticacao deve seguir esta regra:

- usuario nao autenticado na raiz vai para `login`
- usuario autenticado na raiz vai para `financial-accounts`
- login com sucesso redireciona para `financial-accounts`

Justificativa:

- `Financial Accounts` e o primeiro modulo operacional da Fase 2
- o dashboard continua importante no mapa do produto, mas ainda nao e a tela principal funcional do estado atual
- a navegacao de entrada deve apontar para a primeira area realmente utilizavel da fase vigente

### 8.1 Financial Accounts

Backend:

- `validado`

Frontend:

- `validado`

Evidencias backend:

- entidade `FinancialAccount`
- enum `FinancialAccountType`
- contrato `IFinancialAccountRepository`
- `IFinancialAccountService` e `FinancialAccountService`
- configuracao EF Core
- `DbSet<FinancialAccount>` no `FinanceManagerDbContext`
- repositorio concreto
- endpoints `POST /api/v1/financial-accounts` e `GET /api/v1/financial-accounts`
- migration `AddFinancialAccounts`
- validacao funcional real com autenticacao e criacao/listagem dos 3 tipos

Evidencias frontend:

- service frontend do modulo criado
- tela de contas implementada com loading, vazio, erro e lista
- fluxo `Nova conta` implementado com modal e submissao real
- integracao com autenticacao real validada no navegador
- navegacao autenticada validada apos estabilizacao da shell compartilhada

Observacoes:

- modulo estabilizado dentro da shell autenticada compartilhada
- pronto para servir como primeiro destino operacional da Fase 2

### 8.2 Transaction Categories

Backend:

- `validado`

Frontend:

- `validado`

Evidencias backend:

- entidade `TransactionCategory`
- enum `TransactionCategoryType`
- contrato `ITransactionCategoryRepository`
- caso de uso de criacao e listagem implementado
- migration do modulo aplicada
- endpoints `POST /api/v1/transaction-categories` e `GET /api/v1/transaction-categories` validados
- bloqueio de duplicidade basica por `Name + Type` no mesmo usuario

Evidencias frontend:

- service frontend do modulo criado
- tela de categorias implementada com loading, vazio, erro e lista
- fluxo `Nova categoria` implementado com modal e submissao real
- integracao com autenticacao real validada no navegador
- navegacao autenticada validada apos estabilizacao da shell compartilhada

Observacoes:

- modulo estabilizado para continuar servindo de base ao futuro consumo por `Transactions Core`
- nao avancar para reuso em transacoes antes do inicio formal desse modulo

### 8.2.1 Shell autenticada compartilhada

Status:

- `validado`

Dependencias:

- autenticacao frontend funcional
- paginas autenticadas de `Financial Accounts` e `Transaction Categories` implementadas
- extracao da shell para `app/(authenticated)/layout.tsx`

Evidencias:

- layout autenticado compartilhado criado
- sidebar lateral esquerda centralizada em componente unico
- links ativos funcionando corretamente em `Contas` e `Categorias`
- validacao visual automatizada com navegador local em desktop e largura pequena
- modais e headers preservados visualmente nas duas telas

Observacoes:

- a shell deixou de ficar duplicada por pagina
- a base autenticada voltou a ser confiavel antes da entrada em `Transactions Core`

### 8.3 Transactions Core

Backend:

- `validado`

Frontend:

- `validado`

Evidencias backend:

- spec `spec/Financial/transactions-core-spec.md` criada
- entidade `Transaction`
- enums `TransactionType` e `TransactionStatus`
- ajuste de `FinancialAccount` para mutacao de saldo
- contrato `ITransactionRepository`
- servico `ITransactionService` e `TransactionService`
- configuracao EF Core do modulo
- `DbSet<Transaction>` no `FinanceManagerDbContext`
- repositorio concreto
- endpoints `POST /api/v1/transactions/income`, `POST /api/v1/transactions/expense`, `POST /api/v1/transactions/transfer` e `GET /api/v1/transactions`
- migration `AddTransactionsCore` gerada
- build da solucao aprovado apos a implementacao
- migration aplicada no PostgreSQL local
- validacao runtime real de `income`, `expense`, `transfer` e `GET /api/v1/transactions`

Evidencias frontend:

- tipos do modulo criados
- service frontend de transacoes criado
- tela `Transactions` implementada com loading, vazio, erro e listagem
- formularios de receita, despesa e transferencia implementados
- filtros basicos por periodo, tipo e conta implementados
- rota autenticada `/transactions` criada
- proxy atualizado para proteger a rota
- shell autenticada atualizada para expor a navegacao do modulo
- `npm run build` aprovado apos a implementacao
- consumo real do backend validado por runtime da API e persistencia local

Observacoes:

- o modulo saiu de `documentado` para `validado`
- os quatro fluxos reais do modulo foram exercitados com sucesso em runtime
- os saldos finais das contas confirmaram o comportamento esperado de receita, despesa e transferencia

## 9. Fases Posteriores

As fases abaixo continuam:

- `documentado`

Sem implementacao oficial iniciada:

- Fase 3 - Cartoes, faturas e pagamentos
- Fase 4 - Parcelamento, recorrencia e previsao
- Fase 5 - Dashboard e consolidacao frontend
- Fase 6 - Refino operacional, QA e preparacao para expansao

## 10. Mapa Resumido do Que Ja Foi Feito

### 10.1 Ja concluido

- fundacao do repositorio
- solucao backend
- projetos backend
- app frontend base
- estrutura inicial de pastas
- autenticacao backend
- frontend de autenticacao com `register`, `login`, sessao e protecao de rotas
- migration inicial de auth
- integracao com PostgreSQL
- Swagger com Bearer
- validacao funcional real de `register`, `login` e `me`
- `Financial Accounts` backend e frontend validados
- `Transaction Categories` backend e frontend validados
- `Transactions Core` backend e frontend validados
- shell autenticada compartilhada estabilizada e validada

### 10.2 Ja estruturado, mas nao funcionalmente entregue

- arquitetura inicial do frontend
- diretorios planejados do frontend
- fases posteriores do roadmap

### 10.3 Ainda nao implementado

- Fase 3 em diante

### 10.4 Observacao operacional imediata

- o primeiro backup remoto publico ja foi publicado no GitHub
- a prioridade operacional agora e consolidar o fechamento formal da Fase 2 e preparar a entrada na Fase 3 sem antecipar escopo


## 11. Agente de Acompanhamento de Progresso

Fica oficialmente recomendado um agente proprio para acompanhamento de estado.

Nome de papel sugerido:

- `Agente de Acompanhamento de Estado`

Missao:

- atualizar o mapa de progresso do projeto
- marcar tarefas, subtarefas, modulos e fases conforme evidencia real
- registrar bloqueios e dependencias
- evitar que o estado do projeto dependa de memoria informal

## 12. O que Esse Agente Deve Fazer

- ler o plano vigente da fase atual
- acompanhar o que foi concluido no codigo e na validacao
- atualizar o status dos itens
- registrar observacoes curtas sobre evidencias
- sinalizar bloqueios
- manter rastreabilidade entre fase, modulo, tarefa e subtarefa

## 13. O que Esse Agente Nao Deve Fazer

- redefinir escopo por conta propria
- marcar algo como concluido sem evidencia
- confundir pasta criada com funcionalidade pronta
- alterar o roadmap
- substituir o agente principal de integracao tecnica

## 14. Quando Ele Pode Marcar Algo como Concluido

### 14.1 Subtarefa

Pode marcar como `concluido` quando:

- a subtarefa tiver evidencias concretas no codigo ou na validacao

### 14.2 Tarefa

Pode marcar como `concluido` quando:

- todas as subtarefas obrigatorias estiverem concluidas
- e a tarefa tiver atendido seu resultado verificavel

### 14.3 Modulo

Pode marcar como `concluido` quando:

- o criterio de pronto do modulo tiver sido atingido
- e houver validacao basica integrada

### 14.4 Fase

Pode marcar como `concluido` quando:

- todos os modulos obrigatorios da fase tiverem atingido seus criterios de pronto

## 15. Quando Ele Nao Pode Marcar Algo como Concluido

Nao pode marcar como `concluido` quando:

- so existe documentacao
- so existe estrutura vazia
- o codigo existe, mas nao foi integrado
- o endpoint existe, mas nao foi exercitado
- a tela existe, mas nao consome o backend real quando isso faz parte do escopo
- ainda ha ambiguidade relevante sobre o criterio de pronto

## 16. Formato Recomendado de Registro

Cada item acompanhado deve conter, no minimo:

- identificador
- fase
- modulo
- tarefa
- subtarefa opcional
- status
- dependencias
- evidencia
- observacoes

Exemplo textual:

```text
Fase: 2
Modulo: Financial Accounts
Tarefa: CreateFinancialAccount backend
Subtarefa: endpoint POST /api/v1/financial-accounts
Status: implementado
Dependencias: entidade + caso de uso + repositorio
Evidencia: controller criado e rota registrada
Observacoes: ainda falta validacao via Swagger
```

## 17. Regra Operacional para as Proximas Etapas

Daqui em diante, toda etapa importante do projeto deve ser tratada assim:

1. planejar
2. implementar
3. validar
4. atualizar o mapa de progresso

Isso evita o erro comum de termos codigo novo sem estado oficial atualizado.

## 18. Conclusao

Este documento fecha duas coisas importantes ao mesmo tempo:

- o estado real do projeto ate aqui
- a governanca de como esse estado deve ser mantido atualizado

Com isso, o projeto passa a ter:

- memoria operacional
- criterio de status
- fronteira clara entre estrutura e entrega real
- base pronta para um agente de acompanhamento disciplinado

Esse mapa deve ser atualizado sempre que uma tarefa, subtarefa, modulo ou fase atingir evidencia suficiente para mudanca real de status.






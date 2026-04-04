# Spec - Financial Accounts

## 1. Objetivo da Spec

Este documento detalha o desenvolvimento do primeiro modulo da Fase 2:

- `Financial Accounts`

O objetivo deste modulo e criar a base operacional das contas financeiras do usuario autenticado, que depois sustentarao:

- receitas
- despesas
- transferencias
- pagamentos futuros de outros modulos

Nesta fase, o modulo deve permitir apenas:

- criar conta financeira
- listar contas financeiras do usuario autenticado

## 2. Escopo Fechado do Modulo

### 2.1 Entra no escopo

- `FinancialAccount` como estrutura com saldo direto
- tipos exibidos no formulario:
  - `bank_account`
  - `wallet`
  - `investment_account`
- `bank_account` como tipo padrao do formulario
- endpoint de criacao
- endpoint de listagem
- tela de listagem
- fluxo de nova conta
- estados visuais de loading, vazio, erro e sucesso

### 2.2 Nao entra no escopo

- editar conta
- inativar conta por fluxo de UI
- excluir conta
- filtros por tipo na listagem
- conciliacao de saldo
- recalculo de saldo por transacoes
- cartao de credito dentro desta abstracao

## 3. Decisoes Ja Fechadas

Estas decisoes passam a valer como regra para a implementacao:

- a listagem nao tera filtro por tipo nesta primeira entrega
- o formulario exibira os tres tipos: `bank_account`, `wallet` e `investment_account`
- o tipo padrao sera `bank_account`
- o estado vazio da tela mostrara:
  - CTA principal
  - explicacao educativa curta
- `CreditCard` continua fora deste modulo


## 3.1 Decisao de navegacao de entrada

Fica definido para a Fase 2 que o pos-login do sistema deve redirecionar o usuario autenticado para a area de `Financial Accounts`, e nao para `Dashboard`.

Esta decisao existe porque:

- `Financial Accounts` sera o primeiro modulo operacional real da Fase 2
- o dashboard ainda nao e a tela principal funcional do estado atual do projeto
- o primeiro destino apos autenticacao deve levar o usuario para a primeira area realmente utilizavel da fase atual

Regras derivadas:

- usuario nao autenticado acessando a raiz deve ser redirecionado para `login`
- usuario autenticado acessando a raiz deve ser redirecionado para `financial-accounts`
- login com sucesso deve redirecionar para `financial-accounts`

## 4. Papel do Modulo no Projeto

Este modulo resolve um problema estrutural do dominio.

Sem conta financeira, o sistema ainda nao possui um destino ou origem coerente para as operacoes do nucleo transacional.

Em termos de projeto, este modulo existe para:

- amarrar ownership financeiro ao usuario autenticado
- criar a primeira estrutura operacional reutilizavel pela Fase 2
- preparar a base para `Transactions Core`
- iniciar a materializacao da linguagem visual premium do produto na area autenticada

## 5. Modelo de Dados Esperado

A entidade `FinancialAccount` deve representar uma estrutura com saldo direto.

Campos esperados nesta fase:

- `Id`
- `UserId`
- `Name`
- `Type`
- `InitialBalance`
- `CurrentBalanceSnapshot` opcional para evolucao futura
- `IsActive`
- `InstitutionName` opcional
- `Description` opcional
- `CreatedAt`
- `UpdatedAt`

## 6. Invariantes do Modulo

Estas regras nao podem ser quebradas:

- toda conta pertence a um unico usuario
- `Name` e obrigatorio
- `Type` deve ser um dos tipos permitidos
- `InitialBalance` representa o valor inicial da estrutura
- `CreditCard` nao pode ser criado como conta financeira
- a listagem deve retornar apenas contas do usuario autenticado

## 7. Ordem Profissional de Desenvolvimento

A implementacao deste modulo deve seguir esta ordem:

1. dominio
2. application
3. persistence
4. api
5. validacao backend
6. service frontend
7. tela de listagem
8. fluxo de nova conta
9. validacao integrada frontend-backend

Motivo:

- o backend precisa fechar a verdade do modulo antes do frontend final
- a UI nao deve inventar contrato
- a validacao deve acontecer em checkpoints pequenos

## 8. Microtarefas Backend

## 8.1 Definir enum ou estrutura de tipo de conta

### O que faz

Cria a representacao controlada dos tipos permitidos de conta financeira.

### O que resolve

Impede que tipos arbitrarios sejam aceitos pelo sistema e protege o dominio de contas fora do escopo.

### Saida esperada

- tipo representando:
  - `bank_account`
  - `wallet`
  - `investment_account`

### Criterio de pronto

- o dominio aceita apenas os tres tipos definidos

## 8.2 Criar entidade `FinancialAccount`

### O que faz

Representa a conta financeira do usuario dentro do dominio.

### O que resolve

Materializa a primeira estrutura com ownership financeiro real da Fase 2.

### Regras que deve respeitar

- `UserId` obrigatorio
- `Name` obrigatorio
- `Type` obrigatorio
- `InitialBalance` registrado no ato de criacao
- `IsActive` presente para evolucao futura, mas sem fluxo exposto agora

### Criterio de pronto

- entidade criada com campos e construcao coerente com o dominio

## 8.3 Criar contrato de persistencia `IFinancialAccountRepository`

### O que faz

Define as operacoes de persistencia necessarias para este modulo.

### O que resolve

Permite que a camada de Application trabalhe com abstracao de persistencia sem depender de EF Core diretamente.

### Operacoes minimas esperadas

- adicionar conta
- listar contas por `userId`
- verificar suporte basico a futuras consultas do modulo

### Criterio de pronto

- contrato suficiente para suportar criacao e listagem

## 8.4 Criar caso de uso `CreateFinancialAccount`

### O que faz

Recebe a intencao do usuario de criar uma conta e coordena o fluxo de criacao.

### O que resolve

Transforma a intencao da API em operacao de sistema com ownership e validacoes basicas.

### Validacoes obrigatorias

- usuario autenticado deve existir no contexto
- `Name` obrigatorio
- `Type` valido
- saldo inicial com formato monetario valido

### Resultado esperado

- conta criada com `id`
- retorno minimo para API

### Criterio de pronto

- handler ou service funcional de criacao

## 8.5 Criar caso de uso de listagem de contas

### O que faz

Retorna a lista das contas do usuario autenticado.

### O que resolve

Permite que o frontend consulte a fonte oficial das contas disponiveis.

### Regras obrigatorias

- listar apenas contas do usuario autenticado
- nao misturar contas de outros usuarios
- nao aplicar filtro por tipo nesta entrega

### Criterio de pronto

- query ou service de listagem funcional

## 8.6 Criar configuracao EF Core de `FinancialAccount`

### O que faz

Mapeia a entidade para a persistencia relacional.

### O que resolve

Permite salvar e consultar contas no PostgreSQL com coerencia estrutural.

### Itens esperados

- nome da tabela
- colunas
- tipos basicos adequados
- relacionamento com `User`
- constraints basicas

### Criterio de pronto

- configuracao aplicada no `DbContext`

## 8.7 Expor `DbSet<FinancialAccount>` no `FinanceManagerDbContext`

### O que faz

Integra a nova entidade ao contexto de persistencia.

### O que resolve

Permite migrations e operacoes reais do modulo.

### Criterio de pronto

- `DbContext` reconhece a entidade

## 8.8 Implementar repositiorio concreto

### O que faz

Implementa a persistencia real de `FinancialAccount`.

### O que resolve

Conecta Application ao banco sem vazar detalhes tecnicos para as camadas superiores.

### Criterio de pronto

- repositiorio salva e lista contas do usuario autenticado

## 8.9 Criar migration do modulo

### O que faz

Adiciona a tabela de contas financeiras ao banco.

### O que resolve

Materializa a persistencia do modulo no PostgreSQL.

### Criterio de pronto

- migration gerada
- schema consistente com a entidade e configuracao

## 8.10 Criar DTO de request para criacao

### O que faz

Define o contrato de entrada da API para nova conta.

### O que resolve

Isola a API do dominio interno e padroniza a validacao de entrada.

### Campos do request

- `name`
- `type`
- `initialBalance`
- `institutionName` opcional
- `description` opcional

### Criterio de pronto

- DTO alinhado ao caso de uso

## 8.11 Criar DTO de response para criacao e listagem

### O que faz

Define a saida padronizada para o frontend.

### O que resolve

Permite ao frontend consumir dados sem conhecer entidade interna.

### Campos minimos da resposta de criacao

- `id`
- `name`
- `type`
- `initialBalance`
- `isActive`
- `createdAt`

### Campos minimos da listagem

- `id`
- `name`
- `type`
- `institutionName`
- `currentBalanceSnapshot` quando existir
- `isActive`

### Criterio de pronto

- contratos prontos para uso do frontend

## 8.12 Criar endpoint `POST /api/v1/financial-accounts`

### O que faz

Recebe requisicao de criacao de conta.

### O que resolve

Abre a fronteira HTTP oficial do modulo.

### Comportamento esperado

- receber request
- validar formato basico
- encaminhar para Application
- devolver resposta estruturada

### Criterio de pronto

- endpoint operacional e protegido por autenticacao

## 8.13 Criar endpoint `GET /api/v1/financial-accounts`

### O que faz

Retorna a lista das contas do usuario autenticado.

### O que resolve

Abre a leitura oficial do modulo para a interface.

### Comportamento esperado

- usuario autenticado chama endpoint
- sistema retorna apenas suas contas
- resposta vem em formato estavel

### Criterio de pronto

- endpoint operacional e protegido por autenticacao

## 8.14 Validar backend via Swagger

### O que faz

Exercita os endpoints com token real no ambiente local.

### O que resolve

Evita considerar o modulo pronto apenas porque compila.

### Cenarios minimos

- criar conta `bank_account`
- criar conta `wallet`
- criar conta `investment_account`
- listar contas do usuario autenticado
- verificar isolamento por usuario

### Criterio de pronto

- fluxo backend validado em execucao real

## 9. Microtarefas Frontend

## 9.1 Definir contratos de frontend do modulo

### O que faz

Cria os tipos usados pela interface para request e response de contas.

### O que resolve

Evita espalhar contratos improvisados em componentes.

### Criterio de pronto

- tipos do modulo criados e alinhados com a API

## 9.2 Criar service de `Financial Accounts`

### O que faz

Centraliza chamadas HTTP do modulo.

### O que resolve

Evita `fetch` solto dentro dos componentes e torna o consumo da API previsivel.

### Operacoes minimas

- `createFinancialAccount`
- `getFinancialAccounts`

### Criterio de pronto

- service funcional usando o cliente HTTP do frontend

## 9.3 Criar estrutura da pagina `Financial Accounts`

### O que faz

Monta a pagina principal do modulo dentro do shell autenticado.

### O que resolve

Cria a superficie visual onde o usuario vai consultar e iniciar o fluxo de criacao.

### Criterio de pronto

- pagina criada com composicao base e pontos de integracao

## 9.4 Implementar estado de loading da pagina

### O que faz

Mostra ao usuario que as contas estao sendo carregadas.

### O que resolve

Evita opacidade e ansiedade durante a consulta de dados.

### Criterio de pronto

- estado visual claro durante a consulta inicial

## 9.5 Implementar estado vazio da pagina

### O que faz

Mostra orientacao quando nao houver contas.

### O que resolve

Ensina o usuario o papel do modulo desde a primeira entrada.

### Conteudo esperado

- titulo curto de vazio
- explicacao educativa curta
- botao `Nova conta`

### Texto orientativo sugerido

- `Voce ainda nao possui contas cadastradas. Crie sua primeira conta para comecar a registrar movimentacoes.`

### Criterio de pronto

- vazio funcional com CTA claro

## 9.6 Implementar estado de erro da pagina

### O que faz

Exibe falha de carregamento de forma clara.

### O que resolve

Evita telas silenciosas quando a API falhar.

### Criterio de pronto

- mensagem clara e botao de nova tentativa

## 9.7 Implementar lista de contas

### O que faz

Exibe as contas retornadas pela API.

### O que resolve

Entrega a visao principal do modulo.

### Informacoes minimas por item

- nome
- tipo
- instituicao
- status visual de ativa
- saldo inicial ou snapshot quando houver

### Criterio de pronto

- lista conectada a dados reais da API

## 9.8 Criar fluxo de `Nova conta`

### O que faz

Abre a experiencia de cadastro da conta.

### O que resolve

Entrega a principal mutacao do modulo.

### Criterio de pronto

- usuario consegue entrar no fluxo de criacao a partir da pagina

## 9.9 Implementar formulario de nova conta

### O que faz

Recebe dados da conta e envia para a API.

### O que resolve

Materializa a criacao da conta no frontend.

### Campos obrigatorios

- `Nome`
- `Tipo`
- `Saldo inicial`

### Campos opcionais

- `Instituicao`
- `Descricao`

### Regra de UX do campo tipo

- exibir os tres tipos
- selecionar `bank_account` por padrao

### Criterio de pronto

- formulario renderiza corretamente e submete dados validos

## 9.10 Implementar validacao visual do formulario

### O que faz

Mostra erros por campo e impede envio incoerente.

### O que resolve

Reduz retrabalho do usuario e melhora confianca da operacao.

### Criterio de pronto

- campos invalidos recebem feedback claro

## 9.11 Implementar estado de submissao

### O que faz

Bloqueia envio duplo e mostra processamento.

### O que resolve

Evita duplicidade de acoes e melhora a percepcao de controle.

### Criterio de pronto

- botao principal entra em loading durante envio

## 9.12 Implementar tratamento de sucesso

### O que faz

Atualiza a experiencia apos a criacao da conta.

### O que resolve

Conclui o fluxo de maneira clara e confiavel.

### Comportamento esperado

- conta criada com sucesso
- formulario fecha ou retorna para a lista
- lista atualiza com a nova conta sem recarga manual
- feedback curto de sucesso

### Criterio de pronto

- fluxo completo de sucesso funcionando

## 9.13 Implementar tratamento de erro de submissao

### O que faz

Exibe falhas de validacao, negocio ou rede.

### O que resolve

Evita que o usuario perca contexto ou duvide do resultado da operacao.

### Criterio de pronto

- falhas aparecem de forma clara e nao silenciosa

## 10. Dependencias Entre Microtarefas

### 10.1 Dependencias backend

- enum de tipo antes da entidade
- entidade antes do mapeamento EF
- entidade e repositorio antes do caso de uso
- caso de uso antes dos endpoints
- `DbContext` e configuracao antes da migration
- endpoints antes da validacao via Swagger

### 10.2 Dependencias frontend

- contratos antes do service
- service antes da integracao real da pagina
- pagina antes da lista final
- formulario antes do fluxo de sucesso completo
- API estavel antes da integracao final do modulo

## 11. O Que Pode Andar em Paralelo

Pode:

- contratos de frontend e service depois que os DTOs da API estiverem estaveis
- layout visual da pagina de contas enquanto a persistencia backend fecha
- estrutura do formulario enquanto o endpoint de criacao esta sendo finalizado

Nao pode:

- integracao final do frontend antes dos endpoints estarem estaveis
- migration antes de entidade e configuracao estarem corretas
- validacao final do modulo antes de backend e frontend se encontrarem no fluxo real

## 12. Tela `Financial Accounts`

## 12.1 Objetivo da tela

Ser o ponto de consulta das contas financeiras do usuario e o ponto de entrada para criacao de nova conta.

## 12.2 Blocos visuais da tela

- cabecalho da pagina
- subtitulo explicativo curto
- botao primario `Nova conta`
- area de feedback global quando necessario
- area central de lista de contas
- estado vazio quando nao houver contas
- estado de erro quando a consulta falhar

## 12.3 Conteudo do cabecalho

Titulo:

- `Contas financeiras`

Subtitulo sugerido:

- `Gerencie as estruturas que recebem e concentram o seu saldo disponivel.`

## 12.4 Botoes da tela

### Botao `Nova conta`

Funcao:

- abrir o fluxo de criacao de conta

Acao esperada:

- abrir pagina, drawer ou modal de `Nova conta` conforme a implementacao escolhida

Resultado esperado:

- usuario entra no formulario de criacao

### Botao `Tentar novamente`

Funcao:

- repetir a consulta quando a listagem falhar

Acao esperada:

- refazer chamada de listagem

Resultado esperado:

- tela volta para loading e depois exibe lista ou novo erro

## 12.5 Estado de loading

Comportamento:

- lista ainda nao exibida
- placeholders ou skeletons de cards/tabela
- CTA principal continua legivel

## 12.6 Estado vazio

Comportamento:

- mostrar mensagem educativa
- mostrar CTA principal de criacao

Texto sugerido:

- `Voce ainda nao possui contas cadastradas. Crie sua primeira conta para comecar a registrar movimentacoes.`

Botao presente:

- `Nova conta`

Acao do botao:

- abre o fluxo de criacao

## 12.7 Estado de erro

Comportamento:

- mensagem clara de falha
- botao `Tentar novamente`

Texto sugerido:

- `Nao foi possivel carregar suas contas agora.`

## 12.8 Estado com dados

Comportamento:

- exibir lista de contas
- cada item deve destacar:
  - nome da conta
  - tipo
  - instituicao quando houver
  - saldo
  - status de ativa

## 13. Fluxo `Nova conta`

## 13.1 Objetivo do fluxo

Permitir criar uma nova conta financeira com baixo atrito e alta clareza.

## 13.2 Blocos visuais do fluxo

- titulo do fluxo
- explicacao curta
- formulario principal
- barra de acoes
- feedback de erro ou sucesso

## 13.3 Titulo sugerido

- `Nova conta financeira`

## 13.4 Explicacao sugerida

- `Cadastre uma conta bancaria, carteira ou conta de investimento para organizar suas movimentacoes.`

## 13.5 Campos do formulario

### Campo `Nome`

Tipo:

- texto

Obrigatorio:

- sim

Funcao:

- identificar a conta na interface e nos fluxos futuros

### Campo `Tipo`

Tipo:

- select

Obrigatorio:

- sim

Opcoes exibidas:

- `bank_account`
- `wallet`
- `investment_account`

Valor padrao:

- `bank_account`

Funcao:

- classificar a conta dentro da abstracao permitida

### Campo `Saldo inicial`

Tipo:

- monetario

Obrigatorio:

- sim

Funcao:

- registrar o saldo inicial da conta no momento da criacao

### Campo `Instituicao`

Tipo:

- texto

Obrigatorio:

- nao

Funcao:

- informar banco ou instituicao relacionada

### Campo `Descricao`

Tipo:

- textarea ou texto longo

Obrigatorio:

- nao

Funcao:

- registrar contexto adicional da conta

## 13.6 Botoes do fluxo

### Botao `Criar conta`

Funcao:

- enviar o formulario para a API

Comportamento ao clicar:

- validar campos
- se houver erro, destacar os campos
- se estiver valido, iniciar submissao
- bloquear envio duplo

Resultado esperado em sucesso:

- criar conta
- mostrar feedback curto
- retornar para a listagem atualizada

### Botao `Cancelar`

Funcao:

- sair do fluxo sem criar conta

Comportamento ao clicar:

- fechar formulario ou voltar para a lista

Resultado esperado:

- usuario retorna ao estado anterior sem mutacao de dados

## 13.7 Estados do fluxo

### Loading de submissao

Comportamento:

- botao `Criar conta` entra em loading
- botao pode ficar desabilitado
- evitar clique duplo

### Erro de validacao

Comportamento:

- mensagem no campo correspondente
- manter formulario aberto

### Erro de negocio ou transporte

Comportamento:

- mostrar bloco de erro geral
- nao limpar formulario

### Sucesso

Comportamento:

- exibir feedback curto
- atualizar lista
- encerrar fluxo

## 14. Criterios de Pronto por Microbloco

### Dominio pronto quando

- entidade existe
- tipos estao controlados
- invariantes basicas estao protegidas

### Application pronta quando

- criacao e listagem existem
- ownership e validacoes funcionam

### Persistencia pronta quando

- entidade esta mapeada
- migration existe
- repositorio salva e lista corretamente

### API pronta quando

- endpoints de criacao e listagem funcionam com autenticacao
- contratos de request e response estao estaveis

### Frontend de listagem pronto quando

- pagina carrega da API real
- estados de loading, vazio e erro existem
- lista renderiza contas reais

### Fluxo de nova conta pronto quando

- formulario envia para a API real
- sucesso atualiza a lista
- erro e validacao sao visiveis

## 15. Checkpoints de Validacao

## 15.1 Checkpoint backend

Validar:

- criar `bank_account`
- criar `wallet`
- criar `investment_account`
- listar contas do usuario autenticado
- garantir isolamento por ownership

## 15.2 Checkpoint de contrato

Validar:

- request e response alinhados entre API e frontend
- nomes de campos estaveis

## 15.3 Checkpoint frontend

Validar:

- estado vazio aparece corretamente
- formulario inicia com `bank_account`
- criar conta atualiza a lista
- erro de rede ou validacao aparece com clareza

## 15.4 Checkpoint integrado

Fluxo esperado:

1. usuario autenticado entra em `Financial Accounts`
2. sistema lista contas ou mostra vazio
3. usuario clica em `Nova conta`
4. usuario preenche formulario
5. usuario clica em `Criar conta`
6. sistema cria conta
7. usuario retorna para lista atualizada

## 16. O Que Nao Deve Ser Feito Durante Este Modulo

- adicionar edicao "so para aproveitar"
- adicionar inativacao "porque o campo ja existe"
- acoplar cartao de credito ao conceito de conta
- adicionar filtro por tipo nesta primeira entrega
- antecipar saldo derivado de transacoes antes de `Transactions Core`

## 17. Decisao Final de UX Deste Modulo

A experiencia da primeira entrega de `Financial Accounts` deve ser:

- simples
- premium
- didatica
- segura
- coerente com a linguagem escura do produto

A interface nao deve tentar parecer completa demais cedo.
Ela deve provar com clareza o primeiro bloco operacional da Fase 2.

## 18. Resultado Esperado ao Final da Implementacao

Ao final deste modulo, o sistema deve permitir que um usuario autenticado:

- veja a tela de contas
- entenda o papel da tela mesmo no estado vazio
- crie uma conta financeira
- escolha entre `bank_account`, `wallet` e `investment_account`
- veja `bank_account` como opcao padrao
- retorne para a lista com a nova conta exibida

Quando isso estiver funcionando com backend real e validacao minima integrada, o modulo `Financial Accounts` podera ser considerado pronto para a Fase 2.


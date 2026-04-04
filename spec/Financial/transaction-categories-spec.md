# Spec - Transaction Categories

## 1. Objetivo da Spec

Este documento detalha o desenvolvimento do segundo modulo da Fase 2:

- `Transaction Categories`

O objetivo deste modulo e criar a camada de classificacao semantica que sera usada por `Transaction` sem retirar de `Transaction` o papel de entidade central do nucleo financeiro.

Nesta fase, o modulo deve permitir apenas:

- criar categoria transacional
- listar categorias transacionais do usuario autenticado
- disponibilizar categorias de forma pronta para consumo futuro em formularios de receita e despesa

## 2. Escopo Fechado do Modulo

### 2.1 Entra no escopo

- `TransactionCategory` como classificacao semantica de receitas e despesas
- endpoint de criacao
- endpoint de listagem
- tela simples de listagem
- fluxo de nova categoria
- estados visuais de loading, vazio, erro e sucesso
- suporte estrutural para consumo futuro em selects de `Transactions Core`
- `isActive` e `isSystem` no modelo para evolucao futura

### 2.2 Nao entra no escopo

- editar categoria
- inativar categoria por fluxo de UI
- excluir categoria
- sistema de categorias padrao seedadas automaticamente
- filtros por tipo na listagem desta primeira entrega
- customizacao visual avancada de paleta
- galeria de icones
- hierarquia de categorias
- subcategorias
- regras de categoria influenciando saldo ou comportamento financeiro

## 3. Decisoes Ja Fechadas

Estas decisoes passam a valer como regra para a implementacao:

- `Transaction` continua como entidade central; categoria nao assume regra transacional
- a listagem inicial nao tera filtro por tipo
- o modulo tera tela propria simples para ficar validavel e reutilizavel
- o formulario exibira os tipos `income` e `expense`
- `expense` sera o tipo padrao do formulario por ser o caso mais frequente no fluxo inicial do produto
- o estado vazio mostrara:
  - CTA principal
  - explicacao educativa curta
- `Color` e `Icon` permanecem opcionais no modelo, mas a primeira entrega nao deve virar um modulo de personalizacao visual

## 3.1 Assuncao operacional desta spec

Para manter a Fase 2 profissional e enxuta, esta spec assume que:

- categorias serao criadas pelo usuario nesta fase
- nao havera categorias de sistema seedadas automaticamente agora
- o formulario inicial exigira `Name` e `Type`
- `Color` e `Icon` poderao existir no contrato do backend, mas o frontend inicial pode tratar esses campos como opcionais simples, sem sofisticacao visual

Motivo:

- isso preserva o dominio previsto nos documentos
- evita superconstruir um modulo de apoio antes de `Transactions Core`
- prepara um contrato bom o suficiente para consumo futuro sem inflar a UX cedo demais

## 4. Papel do Modulo no Projeto

Este modulo resolve um problema de linguagem e organizacao do dinheiro.

Sem categorias, o sistema ate consegue registrar movimentacoes, mas perde uma parte importante da leitura semantica que o usuario espera ao analisar receitas e despesas.

Em termos de projeto, este modulo existe para:

- dar nome e contexto analitico as futuras `Transaction`
- preparar os formularios de receita e despesa da Fase 2
- organizar leitura futura de extrato, filtros e relatorios
- manter a classificacao como apoio, sem invadir o nucleo da regra financeira

## 5. Modelo de Dados Esperado

A entidade `TransactionCategory` deve representar uma classificacao reutilizavel pelo usuario.

Campos esperados nesta fase:

- `Id`
- `UserId` opcional para compatibilidade futura com categorias de sistema
- `Name`
- `Type`
- `Color` opcional
- `Icon` opcional
- `IsSystem`
- `IsActive`
- `CreatedAt`
- `UpdatedAt`

## 6. Invariantes do Modulo

Estas regras nao podem ser quebradas:

- categoria do usuario pertence a um unico usuario quando `IsSystem = false`
- `Name` e obrigatorio
- `Type` deve ser controlado
- `Type` deve representar apenas `income` ou `expense` nesta fase
- categoria nao altera calculo de saldo, nao define conta e nao define status de transacao
- listagem do usuario nao pode misturar categorias privadas de outros usuarios
- `IsSystem` pode existir no modelo, mas a primeira entrega nao deve depender de seed obrigatoria

## 7. Ordem Profissional de Desenvolvimento

A implementacao deste modulo deve seguir esta ordem:

1. dominio
2. application
3. persistence
4. api
5. validacao backend
6. service frontend
7. tela de listagem
8. fluxo de nova categoria
9. validacao integrada frontend-backend
10. checkpoint de prontidao para consumo por `Transactions Core`

Motivo:

- categorias sao modulo de apoio, entao o contrato precisa nascer claro e estavel
- o backend continua sendo a fonte de verdade do modulo
- o frontend precisa nascer pensando no reuso futuro em formularios de transacao

## 8. Microtarefas Backend

## 8.1 Definir enum ou estrutura de tipo de categoria

### O que faz

Cria a representacao controlada dos tipos permitidos de categoria transacional.

### O que resolve

Impede classificacoes arbitrarias e prepara integracao clara com `income` e `expense` em `Transactions Core`.

### Saida esperada

- tipo representando:
  - `income`
  - `expense`

### Criterio de pronto

- o dominio aceita apenas os tipos definidos para a fase

## 8.2 Criar entidade `TransactionCategory`

### O que faz

Representa a classificacao reutilizavel que podera ser aplicada a varias `Transaction`.

### O que resolve

Materializa o modulo de classificacao sem roubar de `Transaction` a regra central do nucleo financeiro.

### Regras que deve respeitar

- `Name` obrigatorio
- `Type` controlado
- `UserId` presente quando a categoria for do usuario
- `IsSystem` presente para evolucao futura
- `IsActive` presente para evolucao futura, mas sem fluxo exposto agora

### Criterio de pronto

- entidade criada com campos e construcao coerente com o dominio

## 8.3 Criar contrato de persistencia `ITransactionCategoryRepository`

### O que faz

Define as operacoes de persistencia necessarias para criacao e leitura do modulo.

### O que resolve

Permite que a Application trate categorias com abstracao de persistencia, sem acoplar regra de negocio ao EF Core.

### Operacoes minimas esperadas

- adicionar categoria
- listar categorias por `userId`
- verificar duplicidade minima por nome e tipo dentro do ownership do usuario
- preparar leitura futura de categorias do usuario e categorias de sistema

### Criterio de pronto

- contrato suficiente para suportar criacao, listagem e validacoes basicas

## 8.4 Criar caso de uso `CreateTransactionCategory`

### O que faz

Recebe a intencao do usuario de criar uma categoria e coordena o fluxo de criacao.

### O que resolve

Transforma a necessidade de classificacao do usuario em operacao de sistema com ownership e consistencia semantica.

### Validacoes obrigatorias

- usuario autenticado deve existir no contexto
- `Name` obrigatorio
- `Type` valido
- impedir duplicidade obvia de `Name + Type` para o mesmo usuario nesta fase

### Resultado esperado

- categoria criada com `id`
- retorno minimo para API

### Criterio de pronto

- handler ou service funcional de criacao

## 8.5 Criar caso de uso de listagem de categorias

### O que faz

Retorna a lista das categorias disponiveis para o usuario autenticado.

### O que resolve

Permite que o frontend consulte a fonte oficial de categorias antes da chegada de `Transactions Core`.

### Regras obrigatorias

- listar categorias do usuario autenticado
- preparar compatibilidade com categorias de sistema, sem depender delas agora
- nao aplicar filtro por tipo nesta entrega
- resposta deve ser simples o suficiente para uso futuro em select

### Criterio de pronto

- query ou service de listagem funcional

## 8.6 Criar configuracao EF Core de `TransactionCategory`

### O que faz

Mapeia a entidade para persistencia relacional.

### O que resolve

Permite salvar e consultar categorias no PostgreSQL com estrutura consistente.

### Itens esperados

- nome da tabela
- colunas
- tipos adequados para `Name`, `Type`, `Color`, `Icon`
- relacionamento opcional com `User`
- constraints basicas
- indice coerente para leitura por ownership e atividade

### Criterio de pronto

- configuracao aplicada no `DbContext`

## 8.7 Expor `DbSet<TransactionCategory>` no `FinanceManagerDbContext`

### O que faz

Integra a nova entidade ao contexto de persistencia.

### O que resolve

Permite migrations e operacoes reais do modulo.

### Criterio de pronto

- `DbContext` reconhece a entidade

## 8.8 Implementar repositorio concreto

### O que faz

Implementa a persistencia real de `TransactionCategory`.

### O que resolve

Conecta Application ao banco com ownership claro e sem vazar detalhe tecnico para camadas superiores.

### Criterio de pronto

- repositorio salva e lista categorias do usuario autenticado

## 8.9 Criar migration do modulo

### O que faz

Adiciona a tabela de categorias transacionais ao banco.

### O que resolve

Materializa a persistencia do modulo no PostgreSQL.

### Criterio de pronto

- migration gerada
- schema consistente com entidade e configuracao

## 8.10 Criar DTO de request para criacao

### O que faz

Define o contrato de entrada da API para nova categoria.

### O que resolve

Isola a API do dominio interno e padroniza a validacao de entrada.

### Campos do request

- `name`
- `type`
- `color` opcional
- `icon` opcional

### Criterio de pronto

- DTO alinhado ao caso de uso

## 8.11 Criar DTO de response para criacao e listagem

### O que faz

Define a saida padronizada para o frontend e para o consumo futuro em formularios de transacao.

### O que resolve

Permite ao frontend consumir dados sem conhecer a entidade interna.

### Campos minimos da resposta de criacao

- `id`
- `name`
- `type`
- `color`
- `icon`
- `isSystem`
- `isActive`
- `createdAt`

### Campos minimos da listagem

- `id`
- `name`
- `type`
- `color`
- `icon`
- `isSystem`
- `isActive`

### Criterio de pronto

- contratos prontos para uso do frontend e futuro reuso em selects

## 8.12 Criar endpoint `POST /api/v1/transaction-categories`

### O que faz

Recebe requisicao de criacao de categoria.

### O que resolve

Abre a fronteira HTTP oficial do modulo.

### Comportamento esperado

- receber request
- validar formato basico
- encaminhar para Application
- devolver resposta estruturada

### Criterio de pronto

- endpoint operacional e protegido por autenticacao

## 8.13 Criar endpoint `GET /api/v1/transaction-categories`

### O que faz

Retorna a lista das categorias disponiveis para o usuario autenticado.

### O que resolve

Abre a leitura oficial do modulo para a interface e para o futuro fluxo de transacoes.

### Comportamento esperado

- usuario autenticado chama endpoint
- sistema retorna suas categorias
- resposta vem em formato estavel e simples de consumir

### Criterio de pronto

- endpoint operacional e protegido por autenticacao

## 8.14 Validar backend via Swagger

### O que faz

Exercita os endpoints com token real no ambiente local.

### O que resolve

Evita considerar o modulo pronto apenas porque compila.

### Cenarios minimos

- criar categoria `expense`
- criar categoria `income`
- listar categorias do usuario autenticado
- verificar isolamento por usuario
- verificar bloqueio de duplicidade basica quando previsto

### Criterio de pronto

- fluxo backend validado em execucao real

## 9. Microtarefas Frontend

## 9.1 Definir contratos de frontend do modulo

### O que faz

Cria os tipos usados pela interface para request e response de categorias.

### O que resolve

Evita espalhar contratos improvisados em componentes e prepara reuso futuro em selects de transacao.

### Criterio de pronto

- tipos do modulo criados e alinhados com a API

## 9.2 Criar service de `Transaction Categories`

### O que faz

Centraliza chamadas HTTP do modulo.

### O que resolve

Evita `fetch` solto dentro dos componentes e torna o consumo da API previsivel.

### Operacoes minimas

- `createTransactionCategory`
- `getTransactionCategories`

### Criterio de pronto

- service funcional usando o cliente HTTP do frontend

## 9.3 Criar estrutura da pagina `Transaction Categories`

### O que faz

Monta a pagina principal do modulo dentro do shell autenticado.

### O que resolve

Cria a superficie visual onde o usuario consulta categorias e inicia o fluxo de criacao.

### Criterio de pronto

- pagina criada com composicao base e pontos de integracao

## 9.4 Implementar estado de loading da pagina

### O que faz

Mostra ao usuario que as categorias estao sendo carregadas.

### O que resolve

Evita opacidade durante a consulta inicial de dados.

### Criterio de pronto

- estado visual claro durante a consulta inicial

## 9.5 Implementar estado vazio da pagina

### O que faz

Mostra orientacao quando nao houver categorias.

### O que resolve

Ensina o usuario o papel da classificacao antes do modulo de transacoes ficar pronto.

### Conteudo esperado

- titulo curto de vazio
- explicacao educativa curta
- botao `Nova categoria`

### Texto orientativo sugerido

- `Voce ainda nao possui categorias cadastradas. Crie as categorias que irao organizar suas receitas e despesas.`

### Criterio de pronto

- vazio funcional com CTA claro

## 9.6 Implementar estado de erro da pagina

### O que faz

Exibe falha de carregamento de forma clara.

### O que resolve

Evita telas silenciosas quando a API falhar.

### Criterio de pronto

- mensagem clara e botao de nova tentativa

## 9.7 Implementar lista de categorias

### O que faz

Exibe as categorias retornadas pela API.

### O que resolve

Entrega a visao principal do modulo e prepara o olhar do usuario para a linguagem de classificacao.

### Informacoes minimas por item

- nome
- tipo
- cor quando existir
- icone quando existir
- status visual de ativa
- marcador simples para categoria do sistema quando isso vier a existir

### Criterio de pronto

- lista conectada a dados reais da API

## 9.8 Criar fluxo de `Nova categoria`

### O que faz

Abre a experiencia de cadastro da categoria.

### O que resolve

Entrega a principal mutacao do modulo.

### Criterio de pronto

- usuario consegue entrar no fluxo de criacao a partir da pagina

## 9.9 Implementar formulario de nova categoria

### O que faz

Recebe dados da categoria e envia para a API.

### O que resolve

Materializa a criacao da categoria no frontend.

### Campos obrigatorios

- `Nome`
- `Tipo`

### Campos opcionais

- `Cor`
- `Icone`

### Regra de UX do campo tipo

- exibir `expense` e `income`
- selecionar `expense` por padrao

### Regra de UX para `Cor`

- pode ser `input type=color` ou select simples de cores predefinidas
- nao deve disparar construcao de um editor visual complexo nesta fase

### Regra de UX para `Icone`

- pode ser texto controlado ou omitido do frontend inicial se isso reduzir complexidade sem quebrar o contrato
- nao construir galeria de icones nesta fase

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

Atualiza a experiencia apos a criacao da categoria.

### O que resolve

Conclui o fluxo de forma clara e reutilizavel.

### Comportamento esperado

- categoria criada com sucesso
- formulario fecha ou retorna para a lista
- lista atualiza com a nova categoria sem recarga manual
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

## 9.14 Preparar a saida do modulo para reuso futuro

### O que faz

Garante que os dados carregados e os contratos do modulo possam ser reaproveitados por `Transactions Core`.

### O que resolve

Evita retrabalho quando os formularios de receita e despesa forem implementados.

### Criterio de pronto

- contratos, service e leitura do modulo servem ao uso futuro em selects de transacao

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
- modulo estavel antes do reuso em `Transactions Core`

## 11. O Que Pode Andar em Paralelo

Pode:

- contratos de frontend e service depois que os DTOs da API estiverem estaveis
- layout visual da pagina enquanto a persistencia backend fecha
- estrutura do formulario enquanto o endpoint de criacao esta sendo finalizado
- preparacao do contrato de reuso para transacoes depois que o `GET` estiver estavel

Nao pode:

- integrar `Transactions Core` a categorias antes do modulo estar estavel
- criar seed obrigatoria de categorias do sistema no meio deste modulo
- transformar a tela em modulo de personalizacao visual antes da entrega operacional
- validar o modulo como pronto antes da leitura e criacao funcionarem no frontend real

## 12. Tela `Transaction Categories`

## 12.1 Objetivo da tela

Ser o ponto de consulta das categorias do usuario e o ponto de entrada para criacao de nova categoria.

## 12.2 Blocos visuais da tela

- cabecalho da pagina
- subtitulo explicativo curto
- botao primario `Nova categoria`
- area de feedback global quando necessario
- area central de lista de categorias
- estado vazio quando nao houver categorias
- estado de erro quando a consulta falhar

## 12.3 Conteudo do cabecalho

Titulo:

- `Categorias de transacao`

Subtitulo sugerido:

- `Organize como suas futuras receitas e despesas serao classificadas no sistema.`

## 12.4 Botoes da tela

### Botao `Nova categoria`

Funcao:

- abrir o fluxo de criacao de categoria

Acao esperada:

- abrir pagina, drawer ou modal de `Nova categoria` conforme a implementacao escolhida

Resultado esperado:

- usuario entra no formulario de criacao

### Botao `Recarregar lista`

Funcao:

- repetir a consulta manualmente

Acao esperada:

- refazer chamada de listagem

Resultado esperado:

- tela volta para loading e depois exibe lista ou novo erro

### Botao `Tentar novamente`

Funcao:

- recuperar a tela apos erro de consulta

Acao esperada:

- refazer a chamada de listagem

Resultado esperado:

- usuario volta ao fluxo normal da pagina

## 12.5 Estado de loading

Comportamento:

- lista ainda nao exibida
- placeholders ou skeletons de cards
- CTA principal continua visivel

## 12.6 Estado vazio

Comportamento:

- mostrar mensagem educativa
- mostrar CTA principal de criacao

Texto sugerido:

- `Voce ainda nao possui categorias cadastradas. Crie as categorias que irao organizar suas receitas e despesas.`

Botao presente:

- `Nova categoria`

Acao do botao:

- abre o fluxo de criacao

## 12.7 Estado de erro

Comportamento:

- mensagem clara de falha
- botao `Tentar novamente`

Texto sugerido:

- `Nao foi possivel carregar suas categorias agora.`

## 12.8 Estado com dados

Comportamento:

- exibir lista de categorias
- cada item deve destacar:
  - nome da categoria
  - tipo `income` ou `expense`
  - cor quando existir
  - icone quando existir
  - status de ativa

## 13. Fluxo `Nova categoria`

## 13.1 Objetivo do fluxo

Permitir criar uma nova categoria com baixo atrito e clareza suficiente para o usuario entender que esta definindo uma classificacao reutilizavel.

## 13.2 Blocos visuais do fluxo

- titulo do fluxo
- explicacao curta
- formulario principal
- barra de acoes
- feedback de erro ou sucesso

## 13.3 Titulo sugerido

- `Nova categoria de transacao`

## 13.4 Explicacao sugerida

- `Crie categorias que organizarao suas receitas e despesas nos proximos modulos do sistema.`

## 13.5 Campos do formulario

### Campo `Nome`

Tipo:

- texto

Obrigatorio:

- sim

Funcao:

- identificar a categoria na interface e nos futuros filtros e formularios

### Campo `Tipo`

Tipo:

- select

Obrigatorio:

- sim

Opcoes exibidas:

- `expense`
- `income`

Valor padrao:

- `expense`

Funcao:

- classificar se a categoria sera usada para despesa ou receita

### Campo `Cor`

Tipo:

- seletor simples ou input controlado

Obrigatorio:

- nao

Funcao:

- oferecer apoio visual leve, sem transformar esta entrega em configurador de design

### Campo `Icone`

Tipo:

- texto simples controlado ou opcionalmente ausente da UI inicial

Obrigatorio:

- nao

Funcao:

- preparar leitura visual futura, sem exigir galeria de icones agora

## 13.6 Botoes do fluxo

### Botao `Criar categoria`

Funcao:

- enviar o formulario para a API

Comportamento ao clicar:

- validar campos
- se houver erro, destacar os campos
- se estiver valido, iniciar submissao
- bloquear envio duplo

Resultado esperado em sucesso:

- criar categoria
- mostrar feedback curto
- retornar para a listagem atualizada

### Botao `Cancelar`

Funcao:

- sair do fluxo sem criar categoria

Comportamento ao clicar:

- fechar formulario ou voltar para a lista

Resultado esperado:

- usuario retorna ao estado anterior sem mutacao de dados

## 13.7 Estados do fluxo

### Loading de submissao

Comportamento:

- botao `Criar categoria` entra em loading
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
- duplicidade basica esta tratada quando prevista

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
- lista renderiza categorias reais

### Fluxo de nova categoria pronto quando

- formulario envia para a API real
- sucesso atualiza a lista
- erro e validacao sao visiveis

### Modulo pronto para `Transactions Core` quando

- existe `GET` estavel para alimentar selects
- nome e tipo estao claros no contrato
- ownership e leitura estao consolidados

## 15. Checkpoints de Validacao

## 15.1 Checkpoint backend

Validar:

- criar categoria `expense`
- criar categoria `income`
- listar categorias do usuario autenticado
- garantir isolamento por ownership
- validar duplicidade minima quando prevista

## 15.2 Checkpoint de contrato

Validar:

- request e response alinhados entre API e frontend
- nomes de campos estaveis
- dados prontos para reuso em formularios de transacao

## 15.3 Checkpoint frontend

Validar:

- estado vazio aparece corretamente
- formulario inicia com `expense`
- criar categoria atualiza a lista
- erro de rede ou validacao aparece com clareza

## 15.4 Checkpoint integrado

Fluxo esperado:

1. usuario autenticado entra em `Transaction Categories`
2. sistema lista categorias ou mostra vazio
3. usuario clica em `Nova categoria`
4. usuario preenche formulario
5. usuario clica em `Criar categoria`
6. sistema cria categoria
7. usuario retorna para lista atualizada

## 16. O Que Nao Deve Ser Feito Durante Este Modulo

- adicionar edicao "so para aproveitar"
- adicionar inativacao porque o campo ja existe
- criar subcategorias cedo demais
- acoplar categoria ao calculo de saldo
- transformar a tela em editor visual de cor e icone
- antecipar integracao com `Transactions Core` antes do modulo estabilizar

## 17. Decisao Final de UX Deste Modulo

A experiencia da primeira entrega de `Transaction Categories` deve ser:

- simples
- didatica
- reutilizavel
- coerente com a linguagem premium escura do produto
- claramente preparatoria para o modulo de transacoes

A interface nao deve tentar parecer uma central de taxonomia completa.
Ela deve provar com clareza o segundo bloco operacional da Fase 2 e preparar o terreno para `Transactions Core`.

## 18. Resultado Esperado ao Final da Implementacao

Ao final deste modulo, o sistema deve permitir que um usuario autenticado:

- veja a tela de categorias
- entenda o papel da tela mesmo no estado vazio
- crie uma categoria transacional
- escolha entre `expense` e `income`
- veja `expense` como opcao padrao
- retorne para a lista com a nova categoria exibida
- deixe o modulo pronto para alimentar os formularios de `Transactions Core`

Quando isso estiver funcionando com backend real e validacao minima integrada, o modulo `Transaction Categories` podera ser considerado pronto para a Fase 2.

# Spec - Financial Overview

## 1. Objetivo da Spec

Este documento detalha o desenvolvimento do quarto modulo da Fase 2:

- `Financial Overview`

O objetivo deste modulo e criar a primeira camada clara de leitura financeira do produto a partir das estruturas e transacoes ja existentes.

Nesta fase, o modulo deve permitir apenas:

- visualizar um resumo consolidado do periodo atual
- visualizar saldos por conta
- visualizar destaques recentes de transacoes
- visualizar uma leitura simples de entradas, saidas e transferencias

## 2. Escopo Fechado do Modulo

### 2.1 Entra no escopo

- tela principal de overview financeiro autenticado
- resumo consolidado do periodo atual
- cards de leitura rapida
- saldo total consolidado a partir das contas existentes
- resumo de receitas, despesas e transferencias do periodo
- lista curta de transacoes recentes
- leitura por conta com saldo visivel
- consumo de dados reais dos modulos ja existentes
- estados visuais de loading, vazio, erro e sucesso

### 2.2 Nao entra no escopo

- BI avancado
- graficos complexos obrigatorios
- IA
- previsao financeira
- metas
- comparativos sofisticados entre periodos
- filtros analiticos profundos
- cartao de credito
- fatura
- recorrencia
- construtor de relatorios

## 3. Decisoes Ja Fechadas

Estas decisoes passam a valer como regra para a implementacao:

- o modulo deve ser uma camada de leitura, nao uma nova central operacional
- a fonte de verdade continua sendo os modulos anteriores da Fase 2
- a tela deve priorizar clareza antes de sofisticacao analitica
- o overview deve funcionar com dados reais de contas e transacoes ja existentes
- a primeira entrega pode usar graficos simples ou ate mesmo nenhuma visualizacao grafica, desde que a leitura esteja clara
- o overview nao deve substituir a tela de transacoes; ele deve resumir e apontar para ela

## 4. Papel do Modulo no Projeto

Este modulo fecha o arco principal da Fase 2.

Sem ele, o sistema ja registra dinheiro, mas ainda depende demais da leitura operacional bruta de contas e transacoes.

Em termos de projeto, este modulo existe para:

- transformar operacao em leitura
- consolidar a primeira experiencia de panorama financeiro
- reduzir a necessidade de interpretar o sistema apenas por listas isoladas
- preparar o terreno para fases futuras de credito, planejamento e inteligencia

## 5. Modelo de Leitura Esperado

O modulo deve consolidar informacoes que ja existem no sistema.

Fontes principais desta fase:

- `Financial Accounts`
- `Transactions Core`

Leituras minimas esperadas:

- saldo total consolidado
- quantidade de contas ativas
- total de receitas no periodo
- total de despesas no periodo
- total de transferencias no periodo
- lista de transacoes recentes
- saldos visiveis por conta

## 6. Invariantes do Modulo

Estas regras nao podem ser quebradas:

- o overview deve mostrar apenas dados do usuario autenticado
- o overview nao cria nova verdade de dominio
- o overview nao recalcula regras financeiras fora das fontes oficiais
- a leitura deve ser coerente com os saldos e transacoes ja existentes
- a ausencia de dados deve gerar estado vazio claro, nao tela quebrada

## 7. Ordem Profissional de Desenvolvimento

A implementacao deste modulo deve seguir esta ordem:

1. fechar a spec do overview
2. definir o contrato de leitura consolidada
3. implementar backend de agregacao simples quando necessario
4. validar backend
5. criar tipos e services de frontend
6. criar tela principal do overview
7. conectar cards, lista recente e leitura por conta
8. validar estados de loading, vazio e erro
9. validar integracao completa com dados reais

Motivo:

- o overview depende de contratos claros dos modulos anteriores
- a consolidacao da leitura precisa nascer estavel antes da interface final
- a UI nao deve improvisar regra analitica fora do backend quando isso gerar ambiguidade

## 8. Microtarefas Backend

## 8.1 Definir estrategia de leitura consolidada

### O que faz

Define se o overview sera alimentado por um endpoint dedicado ou por composicao controlada de endpoints existentes.

### O que resolve

Evita que o frontend monte uma pseudo-analise fraca e inconsistente por conta propria.

### Criterio de pronto

- existe uma estrategia unica e clara para alimentar o overview

## 8.2 Criar contrato de resposta do overview

### O que faz

Define a saida oficial da leitura consolidada do modulo.

### O que resolve

Impede que a tela dependa de juncao improvisada de contratos heterogeneos.

### Conteudo minimo esperado

- saldo total consolidado
- total de receitas no periodo
- total de despesas no periodo
- total de transferencias no periodo
- contas com saldo
- transacoes recentes

### Criterio de pronto

- contrato estavel e simples de consumir

## 8.3 Criar caso de uso de leitura do overview

### O que faz

Consolida os dados necessarios para a tela principal do modulo.

### O que resolve

Cria uma superficie unica de leitura, sem espalhar regra de agregacao pela UI.

### Regras obrigatorias

- usar apenas dados do usuario autenticado
- respeitar ownership em contas e transacoes
- manter agregacoes simples e explicaveis

### Criterio de pronto

- caso de uso funcional e coerente com a fase

## 8.4 Criar endpoint do overview

### O que faz

Expoe a leitura consolidada do modulo pela API.

### O que resolve

Entrega ao frontend uma porta oficial para o panorama financeiro.

### Criterio de pronto

- endpoint operacional e protegido por autenticacao

## 8.5 Validar backend do modulo

### O que faz

Exercita a resposta consolidada com dados reais.

### O que resolve

Evita considerar o overview pronto apenas porque a tela renderiza.

### Cenarios minimos

- usuario com contas e transacoes reais recebe leitura consolidada correta
- usuario sem transacoes recebe estado vazio coerente
- dados de outros usuarios nao aparecem

### Criterio de pronto

- leitura consolidada validada com dados reais

## 9. Microtarefas Frontend

## 9.1 Criar tipos de frontend do modulo

### O que faz

Cria os contratos usados pela tela do overview.

### O que resolve

Evita espalhar formas improvisadas de dados na UI.

### Criterio de pronto

- tipos criados e alinhados com a API

## 9.2 Criar service de `Financial Overview`

### O que faz

Centraliza o consumo HTTP do modulo.

### O que resolve

Evita que a tela faca chamadas soltas e sem padrao.

### Operacao minima

- `getFinancialOverview`

### Criterio de pronto

- service funcional usando o cliente HTTP do frontend

## 9.3 Criar estrutura da pagina `Financial Overview`

### O que faz

Monta a pagina principal de leitura financeira dentro do shell autenticado.

### O que resolve

Cria a superficie central do resumo financeiro da fase.

### Criterio de pronto

- pagina criada com composicao base e pontos de integracao

## 9.4 Implementar estado de loading

### O que faz

Mostra ao usuario que o overview esta sendo carregado.

### O que resolve

Evita opacidade no primeiro acesso a tela de leitura consolidada.

### Criterio de pronto

- estado visual claro durante a consulta inicial

## 9.5 Implementar estado vazio

### O que faz

Mostra orientacao quando ainda nao houver dados suficientes para um panorama util.

### O que resolve

Evita que o usuario encontre uma tela fria e sem explicacao.

### Conteudo esperado

- titulo curto de vazio
- explicacao educativa curta
- CTA para criar conta ou registrar transacao quando fizer sentido

### Criterio de pronto

- vazio funcional com orientacao clara

## 9.6 Implementar estado de erro

### O que faz

Exibe falha de carregamento de forma clara.

### O que resolve

Evita que o overview falhe silenciosamente.

### Criterio de pronto

- mensagem clara e acao de nova tentativa

## 9.7 Implementar cards de resumo

### O que faz

Exibe os principais numeros do periodo de forma legivel.

### O que resolve

Entrega valor imediato sem obrigar o usuario a abrir listas detalhadas.

### Informacoes minimas

- saldo consolidado
- receitas
- despesas
- transferencias

### Criterio de pronto

- cards conectados a dados reais

## 9.8 Implementar leitura por conta

### O que faz

Mostra as contas do usuario com saldo visivel.

### O que resolve

Permite entender rapidamente onde o dinheiro esta alocado.

### Criterio de pronto

- contas renderizadas com dados reais e leitura clara

## 9.9 Implementar transacoes recentes

### O que faz

Mostra uma lista curta das movimentacoes mais recentes.

### O que resolve

Conecta o resumo ao comportamento real do extrato.

### Criterio de pronto

- lista curta renderizada com dados reais

## 9.10 Implementar navegacao para areas operacionais

### O que faz

Permite sair do overview para contas ou transacoes quando necessario.

### O que resolve

Impede que o overview vire uma tela isolada sem continuidade de fluxo.

### Criterio de pronto

- acoes claras para aprofundar leitura ou operar o sistema

## 10. Dependencias Entre Microtarefas

### 10.1 Dependencias backend

- estrategia de leitura antes do contrato final
- contrato antes do endpoint final
- caso de uso antes da validacao backend

### 10.2 Dependencias frontend

- tipos antes do service
- service antes da integracao real da pagina
- pagina antes dos cards finais
- contrato estavel antes da leitura consolidada final

## 11. O Que Pode Andar em Paralelo

Pode:

- estrutura visual da pagina enquanto o contrato backend fecha
- definicao dos tipos de frontend depois que o contrato principal estiver claro
- refinamento dos estados visuais enquanto a agregacao backend estabiliza

Nao pode:

- consolidar leitura final no frontend antes da estrategia de backend estar clara
- abrir analise avancada antes da primeira versao simples funcionar
- considerar o modulo pronto antes de a leitura com dados reais estar validada

## 12. Tela `Financial Overview`

## 12.1 Objetivo da tela

Ser a porta principal de leitura financeira da Fase 2.

## 12.2 Blocos visuais da tela

- cabecalho
- subtitulo explicativo curto
- cards de resumo
- area de contas com saldo
- area de transacoes recentes
- estado vazio quando necessario
- estado de erro quando necessario

## 12.3 Conteudo do cabecalho

Titulo sugerido:

- `Visao financeira`

Subtitulo sugerido:

- `Acompanhe o panorama atual das suas contas e movimentacoes.`

## 12.4 Botoes e acoes principais

### Acao `Ver transacoes`

Funcao:

- abrir a tela operacional de extrato

### Acao `Ver contas`

Funcao:

- abrir a tela de contas financeiras

### Acao `Tentar novamente`

Funcao:

- recarregar o overview apos falha

## 12.5 Estado de loading

Comportamento:

- cards e blocos aparecem como placeholders
- estrutura principal continua reconhecivel

## 12.6 Estado vazio

Comportamento:

- exibir mensagem educativa
- orientar o usuario para iniciar contas ou transacoes

Texto sugerido:

- `Seu panorama financeiro ainda nao possui dados suficientes. Cadastre contas e registre movimentacoes para acompanhar seu resumo aqui.`

## 12.7 Estado de erro

Comportamento:

- mostrar mensagem clara de falha
- mostrar acao de nova tentativa

Texto sugerido:

- `Nao foi possivel carregar sua visao financeira agora.`

## 12.8 Estado com dados

Comportamento:

- exibir cards principais
- exibir contas com saldo
- exibir transacoes recentes
- manter leitura clara e pouco poluida

## 13. Criterios de Pronto por Microbloco

### Backend pronto quando

- existe leitura consolidada oficial
- ownership esta protegido
- resposta esta estavel

### Frontend pronto quando

- tela consome backend real
- estados de loading, vazio e erro existem
- cards e blocos principais renderizam dados reais

### Modulo pronto para fechar a Fase 2 quando

- usuario entende seu estado financeiro atual sem depender apenas da lista bruta de transacoes
- a leitura do overview e coerente com contas e transacoes existentes
- o modulo entrega resumo sem inflar escopo analitico

## 14. Checkpoints de Validacao

## 14.1 Checkpoint backend

Validar:

- overview consolidado com dados reais
- overview vazio sem quebra de contrato
- ownership protegido

## 14.2 Checkpoint de contrato

Validar:

- campos de resumo estaveis
- contas e transacoes recentes alinhadas com a API

## 14.3 Checkpoint frontend

Validar:

- cards carregam corretamente
- estado vazio orienta o usuario
- erro aparece com clareza
- navegacao para modulos operacionais funciona

## 14.4 Checkpoint integrado

Fluxo esperado:

1. usuario autenticado entra na visao financeira
2. sistema carrega resumo consolidado
3. usuario entende rapidamente saldo, receitas, despesas e transacoes recentes
4. usuario aprofunda a leitura entrando em contas ou transacoes

## 15. O Que Nao Deve Ser Feito Durante Este Modulo

- transformar o overview em central de BI
- abrir previsao financeira
- abrir IA de recomendacao
- reabrir cartao e fatura dentro da Fase 2
- sobrecarregar a tela com filtros e visualizacoes cedo demais

## 16. Decisao Final de UX Deste Modulo

A experiencia da primeira entrega de `Financial Overview` deve ser:

- clara
- premium
- leve
- confiavel
- coerente com os modulos operacionais ja implementados

A tela deve parecer um painel de leitura, nao um cockpit inflado.

## 17. Resultado Esperado ao Final da Implementacao

Ao final deste modulo, o sistema deve permitir que um usuario autenticado:

- veja seu saldo consolidado
- veja um resumo simples do periodo atual
- veja contas com saldo
- veja transacoes recentes
- navegue rapidamente para os modulos operacionais quando quiser aprofundar

Quando isso estiver funcionando com backend real e validacao minima integrada, `Financial Overview` podera ser tratado como a peca final da Fase 2.

# Spec - Fase 3 Credit and Invoice

## 1. Objetivo da Fase

A `Fase 3` existe para abrir e consolidar o dominio de credito com regras proprias.

Ela deve separar claramente:

- conta financeira com saldo direto
- cartao de credito com ciclo de fatura
- compra de cartao como evento proprio
- pagamento de fatura como operacao financeira distinta

## 2. Estado da Fase

A fase esta:

- concluida para o escopo atual
- validada no nucleo avancado de credito
- congelada como base para as fases seguintes

## 3. Escopo Fechado da Fase

### 3.1 Entra no escopo

- cadastro de cartoes de credito
- fatura como estrutura propria
- lancamentos vinculados ao cartao
- fechamento basico e fechamento avancado de ciclo
- pagamento de fatura
- pagamento parcial de fatura
- valor minimo sugerido para pagamento
- valor de pagamento editavel pelo usuario para refletir a realidade
- parcelamento de compra com distribuicao por ciclos futuros
- leitura inicial e leitura operacional da situacao do cartao
- escolha de destino do lancamento quando a data cair em fatura ja fechada
- ajustes de fatura
- multa e juros de atraso
- juros de rotativo em cima de saldo remanescente vencido
- navegacao operacional entre cartoes e faturas

### 3.2 Nao entra no escopo

- simuladores complexos
- renegociacao
- limites compartilhados entre cartoes
- cartoes adicionais
- integracao bancaria real
- importacao automatica de extrato
- scoring de credito
- cashback
- programa de pontos

## 4. Regras de Produto Ja Fechadas

Estas regras passam a valer como regra oficial da fase.

### 4.1 Parcelamento de compra

- compra parcelada deve ser distribuida por ciclo
- cada parcela entra na fatura correspondente ao seu mes futuro
- a quantidade maxima padrao e `12` parcelas
- o valor deve ser distribuido de forma que a soma final das parcelas reflita exatamente o valor total da compra

### 4.2 Fechamento avancado

- a fatura pode ser fechada manualmente
- uma fatura fechada nao deve receber novos lancamentos por padrao
- se o usuario registrar uma compra cuja data cairia em uma fatura ja fechada, o sistema deve perguntar e permitir duas opcoes:
  - incluir o lancamento naquela fatura fechada
  - jogar o lancamento para a proxima fatura
- o sistema nao deve decidir isso silenciosamente quando houver conflito com a realidade operacional do usuario

### 4.3 Pagamento parcial

- pagamento parcial passa a existir oficialmente
- o valor minimo sugerido deve seguir o padrao simples aprovado para a fase: `15%` da fatura
- esse valor deve ser editavel no momento do pagamento para refletir a realidade do banco ou do app do usuario
- o sistema deve aceitar pagamento total ou parcial, desde que o valor seja positivo e nao exceda o saldo remanescente da fatura no momento do pagamento

### 4.4 Ajustes de fatura

- ajustes simples entram no escopo da fase
- os tipos minimos da fase devem cobrir:
  - credito ou desconto
  - tarifa
  - juros
  - multa
  - aumento manual
  - reducao manual
- ajustes devem alterar o valor da fatura de forma controlada, sem edicao direta solta do total

### 4.5 Regras bancarias simplificadas da fase

Padrao aprovado para esta fase:

- pagamento minimo sugerido: `15%`
- multa por atraso: `2%`
- juros de atraso: `1% ao mes`
- juros de rotativo: `12% ao mes`
- parcelamento maximo de compra: `12`

Estas regras existem como aproximacao operacional do mercado para esta fase e podem evoluir no futuro.

## 5. Dependencias da Fase

A fase depende de:

- `Fase 2` fechada no seu arco principal
- base transacional estavel
- contratos de contas e categorias confiaveis
- camada inicial de leitura financeira ja validada

## 6. Papel da Fase no Projeto

Esta fase expande o sistema da movimentacao simples para um dominio financeiro mais realista e mais proximo do uso cotidiano completo.

Ela passa a representar:

- compras em cartao por ciclo
- fechamento de fatura
- pagamento total ou parcial
- saldo remanescente de credito
- encargos por atraso e rotativo
- correcao manual da realidade via ajuste controlado

## 7. Encerramento Formal da Fase

A `Fase 3` passa a ser tratada como encerrada porque o projeto ja entrega, no escopo atual:

- cartoes de credito com cadastro e leitura
- faturas com abertura automatica e manual
- compras reais de cartao
- parcelamento por ciclos futuros
- fechamento avancado de fatura
- pagamento total e parcial com valor editavel
- encargos simples de atraso e rotativo
- ajustes controlados de fatura
- leitura consolidada e operacional do cartao
- navegacao operacional entre cartoes e faturas

## 8. Criterio de Pronto da Fase

A fase estara pronta quando o usuario conseguir:

- cadastrar cartoes
- registrar compras no cartao
- registrar compras parceladas
- fechar faturas
- visualizar faturas e extrato do cartao
- pagar faturas total ou parcialmente
- corrigir valor de pagamento quando a realidade do banco estiver diferente do sugerido
- aplicar ajustes simples de fatura
- lidar com compra cuja data caiu em fatura ja fechada escolhendo o destino correto
- navegar operacionalmente entre cartoes e faturas

## 9. Resultado Esperado

Ao final da fase, o produto deve suportar tanto saldo direto quanto ciclo de credito com fatura, parcela, fechamento, saldo remanescente e encargos simples de forma separada e consistente.

## 10. Macroetapas da Implementacao

A conclusao da fase deve seguir esta ordem:

1. estabilizar contrato de fatura para saldo remanescente, pagamento parcial e fechamento
2. introduzir parcelamento por ciclo futuro
3. introduzir conflito de compra em fatura ja fechada com decisao explicita do usuario
4. introduzir ajustes controlados de fatura
5. introduzir encargos simples de atraso e rotativo
6. consolidar leitura operacional entre cartoes e faturas
7. validar comportamento integrado ponta a ponta

## 11. Quebra em Tarefas Executaveis

### 11.1 Dominio de fatura

- expandir `Invoice` para representar valor total, valor pago, saldo remanescente, fechamento, minimo sugerido e encargos simples
- permitir transicao coerente entre `open`, `closed`, `partially_paid` e `paid`
- impedir pagamento maior que o saldo remanescente

### 11.2 Pagamento parcial

- aceitar valor de pagamento informado pelo usuario
- sugerir minimo, mas nao impor o valor exato sugerido
- refletir o valor pago na conta financeira real
- manter saldo remanescente da fatura quando houver pagamento parcial

### 11.3 Parcelamento

- permitir informar quantidade de parcelas na compra do cartao
- distribuir parcelas por faturas futuras
- manter rastreabilidade minima de numero da parcela e total de parcelas

### 11.4 Fechamento avancado

- criar operacao de fechar fatura
- impedir lancamento silencioso em fatura fechada
- suportar a escolha do usuario quando a compra cairia em fatura fechada

### 11.5 Ajustes de fatura

- criar operacao de ajuste controlado
- suportar credito, desconto, tarifa, juros, multa, aumento manual e reducao manual
- refletir ajuste no total e no saldo remanescente

### 11.6 Encargos simples

- aplicar multa e juros de atraso sobre saldo remanescente vencido
- aplicar juros de rotativo de forma simplificada sobre saldo remanescente vencido
- impedir aplicacao silenciosa descontrolada de encargos duplicados

### 11.7 Frontend minimo da fase expandida

- formulario de compra com parcelamento
- formulario de compra com escolha de destino quando o ciclo ja estiver fechado
- pagamento de fatura com valor editavel
- acao de fechar fatura
- acao de ajustar fatura
- leitura de saldo remanescente, minimo sugerido e encargos

## 12. Criterio de Validacao Integrada

O fechamento completo da fase exige ao menos estes cenarios:

1. compra simples gera fatura correta
2. compra parcelada distribui parcelas futuras corretamente
3. fechamento manual impede comportamento silencioso errado
4. compra em ciclo fechado obriga escolha explicita do usuario
5. pagamento parcial reduz saldo sem liquidar a fatura
6. pagamento total liquida a fatura
7. valor editado no pagamento e respeitado
8. ajuste altera a fatura corretamente
9. atraso gera encargos simples de forma coerente
10. cartao e fatura continuam navegaveis nos dois sentidos

## 13. Regra de Transicao para a Fase 4

A partir deste ponto, o orquestrador do projeto deve assumir:

- o dominio de credito da `Fase 3` como base estavel
- nenhuma expansao nova de credito como prioridade antes da abertura de planejamento
- a `Fase 4` como proxima fase ativa do produto
- o modulo `Scheduled Entries / Planned Transactions` como primeira abertura recomendada da `Fase 4`

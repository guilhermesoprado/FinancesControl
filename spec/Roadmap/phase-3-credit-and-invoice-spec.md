# Spec - Fase 3 Credit and Invoice

## 1. Objetivo da Fase

A `Fase 3` existe para abrir o dominio de credito com regras proprias.

Ela deve separar claramente:

- conta financeira com saldo direto
- cartao de credito com ciclo de fatura

## 2. Estado da Fase

A fase esta:

- formalizada
- pronta para abertura
- definida como a proxima fase ativa do projeto

## 3. Escopo Fechado da Fase

### 3.1 Entra no escopo

- cadastro de cartoes de credito
- fatura como estrutura propria
- lancamentos vinculados ao cartao
- fechamento basico de ciclo
- pagamento de fatura
- leitura inicial da situacao do cartao

### 3.2 Nao entra no escopo

- parcelamento avancado
- simuladores complexos
- regras bancarias sofisticadas
- renegociacao

## 4. Dependencias da Fase

A fase depende de:

- `Fase 2` fechada no seu arco principal
- base transacional estavel
- contratos de contas e categorias confiaveis
- camada inicial de leitura financeira ja validada

## 5. Papel da Fase no Projeto

Esta fase expande o sistema da movimentacao simples para um dominio financeiro mais realista e mais proximo do uso cotidiano completo.

## 6. Primeiro Movimento Recomendado de Abertura

A abertura formal da fase deve seguir esta ordem:

1. definir o primeiro modulo de cartao
2. definir o contrato minimo de fatura
3. definir como despesas de cartao se relacionam com as estruturas ja existentes
4. evitar abrir cedo parcelamento e complexidade bancaria desnecessaria

## 7. Criterio de Pronto

A fase estara pronta quando o usuario conseguir:

- cadastrar cartoes
- registrar despesas de cartao
- visualizar faturas
- pagar faturas de forma coerente com o restante do sistema

## 8. Resultado Esperado

Ao final da fase, o produto deve suportar tanto saldo direto quanto ciclo de credito de forma separada e consistente.

# Spec - Fase 4 Financial Planning

## 1. Objetivo da Fase

A `Fase 4` existe para levar o produto do registro do passado para o planejamento do futuro.

## 2. Estado da Fase

A fase esta:

- formalizada
- aberta oficialmente
- concluida para o escopo atual
- com `Scheduled Entries / Planned Transactions` implementado e validado como primeiro modulo oficial
- com visualizacao de recorrencias por ocorrencia e competencia ja formalizada no primeiro modulo
- com calendario financeiro simples consolidado sobre a mesma base operacional por ocorrencia
- com previsao operacional basica ja ampliada sobre a mesma fundacao de ocorrencia, status, acao e calendario
- com leitura mensal para decisao ja refinada por meses criticos, distribuicao de carga e prioridades operacionais
- fechada neste nivel estavel para a fase atual do projeto

## 3. Escopo Fechado da Fase

### 3.1 Entra no escopo

- lancamentos previstos
- recorrencia controlada
- compromissos financeiros futuros
- calendario financeiro simples
- previsao operacional basica

### 3.2 Nao entra no escopo

- modelos preditivos complexos
- IA financeira
- automacao excessiva sem validacao

## 4. Dependencias da Fase

A fase depende de:

- `Fase 2` estavel
- preferencialmente `Fase 3` fechada quando planejamento envolver credito

## 5. Papel da Fase no Projeto

Esta fase resolve a passagem de um produto que apenas registra para um produto que tambem ajuda a planejar.

## 6. Primeiro Modulo Oficial da Fase

O primeiro modulo oficial que abriu a fase e:

- `Scheduled Entries / Planned Transactions`

Esse modulo inaugurou a fase porque cria a primeira camada formal de planejamento sem exigir automacao excessiva.

Ele deve permitir, no minimo:

- cadastrar lancamentos futuros
- marcar se o lancamento e unico ou recorrente
- distinguir previsto de realizado
- associar conta, categoria, valor e data prevista
- manter visivel a recorrencia por competencia, sem esconder automaticamente a ocorrencia ja tratada quando a proxima for aberta

## 7. Criterio de Pronto

A fase estara pronta quando o usuario conseguir:

- registrar compromissos futuros
- entender o que ainda vai acontecer
- distinguir realizado de previsto
- consultar o que ja foi tratado e o que ainda vai acontecer dentro da mesma recorrencia

Status atual deste criterio:

- atendido para o escopo atual da `Fase 4`

## 8. Resultado Esperado

Ao final da fase, o sistema passa a oferecer visibilidade do futuro financeiro imediato do usuario.

Essa visibilidade deve preservar a leitura operacional da linha do tempo do planejamento:

- uma competencia tratada continua consultavel com status proprio
- a proxima competencia da mesma recorrencia continua visivel como compromisso futuro

## 9. Regra de Evolucao da Fase

A partir deste documento, a `Fase 4` fica registrada como concluida para o escopo atual do projeto.

O orquestrador deve seguir esta ordem:

1. consolidar `Scheduled Entries / Planned Transactions` como base oficial do planejamento
2. estabilizar a semantica operacional da recorrencia controlada sobre a leitura por ocorrencia
3. consolidar compromissos futuros e calendario financeiro simples
4. ampliar previsao operacional basica



# Spec - Fase 0 Foundation

## 1. Objetivo da Fase

A `Fase 0` existe para colocar o produto de pe.

Ela prepara a base tecnica e funcional minima para que as fases financeiras possam acontecer com seguranca.

## 2. Papel da Fase no Projeto

Sem esta fase, o projeto nao possui:

- identidade do usuario
- acesso protegido
- shell autenticada
- integracao backend-frontend
- ambiente local minimamente confiavel

Esta fase resolve o problema da base do produto.

## 3. Escopo Fechado da Fase

### 3.1 Entra no escopo

- autenticacao com registro e login
- sessao autenticada
- endpoint de usuario atual
- shell autenticada
- infraestrutura local com banco
- integracao real entre frontend e backend

### 3.2 Nao entra no escopo

- operacao financeira real
- contas financeiras
- categorias transacionais
- transacoes
- dashboard
- cartao e fatura

## 4. Resultado Esperado

Ao final da fase, o sistema deve permitir:

- registrar usuario
- autenticar usuario
- proteger rotas autenticadas
- carregar o frontend autenticado com backend real

## 5. Criterio de Pronto

A fase esta pronta quando:

- autenticacao funciona ponta a ponta
- shell autenticada existe
- infraestrutura local sobe com confianca
- o sistema esta apto a iniciar a camada financeira da `Fase 2`

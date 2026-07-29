# Instruções do repositório

## Idioma

- Escreva todos os comentários de code review em português do Brasil.
- Não escreva explicações em inglês, exceto nomes técnicos, identificadores, APIs, comandos e trechos de código.
- Use linguagem técnica, objetiva e respeitosa.
- Explique claramente o problema, o impacto e uma possível correção.

## Code Review Rules

### Formato dos comentários

Para cada problema encontrado:

1. Informe a gravidade: P0, P1, P2 ou P3.
2. Explique o que está incorreto.
3. Explique o possível impacto.
4. Indique o arquivo e o trecho afetado.
5. Sugira uma correção concreta.
6. Evite comentários puramente estéticos que possam ser tratados por formatadores ou linters.

### Critérios gerais

- Procure bugs, regressões e comportamentos inesperados.
- Verifique tratamento de erros e exceções.
- Verifique possíveis problemas de segurança.
- Verifique vazamento de dados sensíveis em logs.
- Verifique alterações incompatíveis em APIs públicas.
- Verifique problemas de concorrência e condições de corrida.
- Verifique consultas ineficientes ao banco de dados.
- Verifique uso incorreto de recursos descartáveis.
- Verifique se os testes cobrem os principais cenários modificados.

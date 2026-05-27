# Copilot Instructions

## Diretrizes de projeto
- No README do projeto HealthCheck, o usuário prefere: sem licença (uso não permitido), documentação de visão geral sem detalhamento profundo de modelos/SQL/páginas, e apresentação visual com emojis para impressionar recrutadores.
- Documentar a implementação de autenticação com comentários para melhor compreensão.

## Regras e Padrões para Commits

Ao gerar mensagens de commit ou responder sobre controle de versão, siga estritamente estas diretrizes:

*   **Padrão:** Utilize sempre o **Conventional Commits** (ex: `feat:`, `fix:`, `refactor:`, `chore:`).
*   **Idioma e Tempo Verbal:** Escreva as mensagens em português e no modo imperativo. 
    *   *Correto:* `feat: adiciona serviço de emissão de notas`
    *   *Incorreto:* `adicionado serviço` ou `adicionando serviço`
*   **Tamanho:** O título do commit deve ter no máximo 50 caracteres. Se houver mais contexto, adicione uma linha em branco e escreva um corpo detalhado.
*   **Contexto Arquitetural:** Se o commit envolver mudanças estruturais ou de arquitetura, especifique o escopo ou a camada no título. 
    *   *Exemplo:* `refactor(Application): isola regras de domínio usando princípios DDD`
*   **Referências:** Se houver um número de issue ou task relacionada, inclua no final do corpo da mensagem (ex: `Closes #123`).
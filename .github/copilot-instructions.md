# Copilot Instructions

## Diretrizes de projeto
- No README do projeto HealthCheck, o usuário prefere: sem licença (uso não permitido), documentação de visão geral sem detalhamento profundo de modelos/SQL/páginas, e apresentação visual com emojis para impressionar recrutadores.
- Documentar a implementação de autenticação com comentários para melhor compreensão.

## Rotina de commits

Quando eu solicitar a rotina de commits enviando a mensagem "Executar rotina de commits" , siga as seguintes diretrizes:

- Verifique as mudanças feitas no projeto com o comando `git status` para identificar os arquivos modificados.
- Então, analise as mudanças de acordo com o contexto, adicione o(s) arquivo(s) modificado(s) ao stage usando `git add <arquivo>` ou `git add .` para 
adicionar todos os arquivos modificados, de acordo com o contexto das mudanças e a importância de cada alteração, e escreva uma mensagem de commit para 
essas alterações seguindo o padrão de **Regras e Padrões para Commits**.
- Por fim, execute o comando `git commit -m "sua mensagem de commit"` para criar o commit com a mensagem escrita.
- Me pergunte se desejo enviar o commit para o repositório remoto (com a branch ativa) e se eu responder "Sim", execute o comando `git push` para enviar o commit para o repositório remoto.

**OBS** - Os commits devem ser por arquivo ou por grupo de arquivos relacionados, e não devem ser feitos commits muito grandes que incluam muitas mudanças não relacionadas.
Isso ajuda a manter um histórico de commits claro e fácil de entender.

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
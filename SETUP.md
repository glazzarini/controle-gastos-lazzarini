# INSTRUÇÕES DE CONFIGURAÇÃO — Google Sheets

## Planilha: Controle_Cartao_Lazzarini

---

## ABA 1 — "lancamentos" (nome exato, minúsculo)

Crie uma aba chamada exatamente: lancamentos

Adicione esses cabeçalhos na linha 1:
| A       | B          | C          | D     | E               | F    | G           |
|---------|------------|------------|-------|-----------------|------|-------------|
| data    | descricao  | categoria  | valor | estabelecimento | tipo | observacao  |

Exemplo de linha:
| 01/06/2026 | Almoço | Alimentacao fora | 45.90 | Outback | A vista | |

---

## ABA 2 — "config" (nome exato, minúsculo)

Crie uma aba chamada exatamente: config

Adicione esses dados exatamente assim:
| A                    | B     |
|----------------------|-------|
| renda_guilherme      | 20000 |
| renda_esposa         | 14000 |
| meta_fatura          | 12000 |
| total_fixos          | 14559 |

---

## IMPORTANTE

- Os nomes das abas devem ser exatamente: lancamentos e config (tudo minúsculo, sem acento)
- A aba de lançamentos começa com cabeçalho na linha 1, dados a partir da linha 2
- Os valores numéricos na aba config devem ser números sem formatação (ex: 20000, não R$ 20.000)

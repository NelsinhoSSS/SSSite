using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

public class DivinyController : Controller
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public DivinyController(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public IActionResult Index() => View();

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatRequest req)
    {
        var apiKey = _config["Groq:ApiKey"];
        var client = _http.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var lastMessage = req.Messages.LastOrDefault()?.content ?? "";

        // Busca dados do EDHREC, Scryfall, MTGGoldfish e site oficial conforme o contexto
        var edhrecContext = await FetchEdhrecDataAsync(lastMessage);
        var scryfallContext = await FetchScryfallContextAsync(lastMessage);
        var deckRefContext = await FetchDecklistReferenceAsync(lastMessage);
        var externalContext = await FetchExternalContextAsync(lastMessage);

        var messages = new List<object>
        {
            new { role = "system", content = DivinySystem.Prompt + edhrecContext + scryfallContext + deckRefContext + externalContext }
        };

        foreach (var m in req.Messages)
            messages.Add(new { role = m.role == "assistant" ? "assistant" : "user", content = m.content });

        var body = new
        {
            model = "llama-3.3-70b-versatile",
            max_tokens = 4096,
            temperature = 0.7,
            messages
        };

        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("https://api.groq.com/openai/v1/chat/completions", content);
        var resultJson = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(resultJson);
        var root = doc.RootElement;
        string text = "";

        if (root.TryGetProperty("choices", out var choices) &&
            choices.GetArrayLength() > 0)
        {
            text = choices[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";
        }

        if (string.IsNullOrEmpty(text))
            text = "Não consegui processar sua mensagem. Tente novamente.";

        var result = JsonSerializer.Serialize(new
        {
            content = new[] { new { type = "text", text } }
        });

        return Content(result, "application/json");
    }

    // Busca dados reais do EDHREC para o comandante identificado na mensagem
    private async Task<string> FetchEdhrecDataAsync(string message)
    {
        try
        {
            var match = Regex.Match(message,
                @"(?:comandante|commander)[:\s]+([A-Za-zÀ-ú\s',\-]{3,50})",
                RegexOptions.IgnoreCase);

            if (!match.Success) return "";

            var commanderName = match.Groups[1].Value.Trim();
            var slug = commanderName.ToLower()
                .Replace(" ", "-")
                .Replace("'", "")
                .Replace(",", "");
            slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");

            var tempClient = new HttpClient();
            tempClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
            var res = await tempClient.GetAsync($"https://json.edhrec.com/pages/commanders/{slug}.json");
            if (!res.IsSuccessStatusCode) return "";

            var json = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var sb = new StringBuilder();
            sb.AppendLine($"\n\n[Dados reais do EDHREC para o comandante {commanderName}:");

            if (root.TryGetProperty("container", out var container) &&
                container.TryGetProperty("json_dict", out var jsonDict) &&
                jsonDict.TryGetProperty("card", out var cardData))
            {
                if (cardData.TryGetProperty("num_decks", out var numDecks))
                    sb.AppendLine($"- Decks registrados no EDHREC: {numDecks}");
            }

            if (root.TryGetProperty("panels", out var panels))
            {
                foreach (var panel in panels.EnumerateArray())
                {
                    if (!panel.TryGetProperty("cardlists", out var cardlists)) continue;
                    foreach (var list in cardlists.EnumerateArray())
                    {
                        if (list.TryGetProperty("header", out var header) &&
                            list.TryGetProperty("num_cards", out var numCards))
                        {
                            sb.AppendLine($"- Média de {header.GetString()}: {numCards} cartas nos decks deste comandante");
                        }
                    }
                }
            }

            sb.AppendLine("Use esses dados como referência para avaliar a distribuição de cartas do deck.]");
            return sb.ToString();
        }
        catch
        {
            return "";
        }
    }

    // Busca dados reais das cartas no Scryfall para enriquecer o contexto
    private async Task<string> FetchScryfallContextAsync(string message)
    {
        try
        {
            var lines = message.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var cardEntries = lines
                .Select(l => Regex.Match(l.Trim(), @"^(\d+)x?\s+(.+)$"))
                .Where(m => m.Success)
                .Select(m => new { qty = int.Parse(m.Groups[1].Value), name = m.Groups[2].Value.Trim() })
                .ToList();

            if (cardEntries.Count < 3) return "";

            // Conta o total de cartas considerando as quantidades
            var totalCards = cardEntries.Sum(c => c.qty);

            // Se não tiver exatamente 100 cartas, avisa a IA para informar o usuário
            if (totalCards != 100)
            {
                return $"\n\n[AVISO: A lista enviada contém {totalCards} carta(s), não 100. " +
                       $"Informe ao usuário que um deck Commander precisa ter exatamente 100 cartas " +
                       $"(incluindo o comandante) e que a análise só será feita quando a lista estiver completa. " +
                       $"Não faça a análise do deck.]";
            }

            var tempClient = new HttpClient();
            var cardNames = cardEntries.Select(c => c.name).Distinct().ToList();
            var allResults = new List<string>();
            var batches = cardNames.Chunk(10);

            foreach (var batch in batches)
            {
                var tasks = batch.Select(async name =>
                {
                    try
                    {
                        var res = await tempClient.GetAsync(
                            $"https://api.scryfall.com/cards/named?fuzzy={Uri.EscapeDataString(name)}");
                        if (!res.IsSuccessStatusCode) return null;

                        var json = await res.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;

                        var cardName = root.TryGetProperty("name", out var n) ? n.GetString() : name;
                        var typeLine = root.TryGetProperty("type_line", out var t) ? t.GetString() : "";
                        var manaCost = root.TryGetProperty("mana_cost", out var mc) ? mc.GetString() : "—";
                        var cmc = root.TryGetProperty("cmc", out var c) ? c.GetDecimal() : 0;
                        var oracleText = root.TryGetProperty("oracle_text", out var ot) ? ot.GetString() : "";
                        var edhrecRank = root.TryGetProperty("edhrec_rank", out var er) ? er.GetInt32() : 0;

                        var isLand = typeLine != null && (
                            typeLine.Contains("Land", StringComparison.OrdinalIgnoreCase) ||
                            typeLine.Contains("Basic", StringComparison.OrdinalIgnoreCase)
                        );
                        var isBasicLand = typeLine != null &&
                            typeLine.Contains("Basic", StringComparison.OrdinalIgnoreCase);
                        var categoria = isLand ? (isBasicLand ? " [BASIC LAND]" : " [LAND]") : "";

                        return $"- {cardName}{categoria} | Tipo: {typeLine} | Custo: {manaCost} (CMC {cmc}) | EDHREC rank: #{edhrecRank} | Texto: {oracleText?.Replace("\n", " ").Trim()[..Math.Min(100, oracleText?.Length ?? 0)]}";
                    }
                    catch { return null; }
                });

                var batchResults = await Task.WhenAll(tasks);
                allResults.AddRange(batchResults.Where(r => r != null)!);
                await Task.Delay(100);
            }

            var lands = allResults.Where(r => r.Contains("[LAND]")).ToList();
            var others = allResults.Where(r => !r.Contains("[LAND]")).ToList();

            var sb = new StringBuilder();
            sb.AppendLine("\n\n[Dados reais das cartas obtidos via Scryfall:");
            sb.AppendLine("ATENÇÃO: use o campo 'Tipo' para classificar corretamente cada carta.");
            sb.AppendLine($"Total de cartas na lista: {totalCards}");
            sb.AppendLine($"Lands identificadas ({lands.Count}):");
            foreach (var l in lands) sb.AppendLine(l);
            sb.AppendLine($"\nDemais cartas ({others.Count}):");
            foreach (var o in others) sb.AppendLine(o);
            sb.AppendLine("\nUse esses dados para identificar corretamente cada carta, sua categoria e função no deck.]");

            return sb.ToString();
        }
        catch
        {
            return "";
        }
    }
    // Busca decklists populares do mesmo comandante no Archidekt
    private async Task<string> FetchDecklistReferenceAsync(string message)
    {
        try
        {
            // Detecta o comandante na mensagem
            var cmdrLine = message.Split('\n')
                .Select(l => l.Trim())
                .FirstOrDefault(l => l.Contains("*CMDR*") || l.StartsWith("Commander:"));

            string commanderName = "";
            if (cmdrLine != null)
            {
                commanderName = Regex.Replace(cmdrLine, @"(\d+x?\s+|\*CMDR\*|Commander:)", "").Trim();
            }
            else
            {
                var commanderMatch = Regex.Match(message,
                    @"(?:comandante|commander)[:\s]+([A-Za-zÀ-ú\s',\-]{3,50})",
                    RegexOptions.IgnoreCase);
                if (commanderMatch.Success)
                    commanderName = commanderMatch.Groups[1].Value.Trim();
            }

            if (string.IsNullOrEmpty(commanderName)) return "";

            var tempClient = new HttpClient();
            tempClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
            tempClient.Timeout = TimeSpan.FromSeconds(15);

            var cardFrequency = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var encodedName = Uri.EscapeDataString(commanderName);

            // Busca IDs dos 100 decks mais visualizados em páginas de 20
            var deckIds = new List<int>();
            for (int page = 1; page <= 5 && deckIds.Count < 100; page++)
            {
                try
                {
                    var res = await tempClient.GetAsync(
                        $"https://archidekt.com/api/decks/?orderBy=-viewCount&formats=Commander&commanders={encodedName}&pageSize=20&page={page}");

                    if (!res.IsSuccessStatusCode) break;

                    var json = await res.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var results = doc.RootElement.GetProperty("results");

                    foreach (var deck in results.EnumerateArray())
                    {
                        if (deck.TryGetProperty("id", out var id))
                            deckIds.Add(id.GetInt32());
                    }

                    // Se não veio mais resultados, para
                    if (results.GetArrayLength() < 20) break;
                }
                catch { break; }
            }

            if (deckIds.Count == 0) return "";

            // Busca os detalhes dos decks em paralelo com limite de 10 simultâneos
            var semaphore = new SemaphoreSlim(10);
            var tasks = deckIds.Select(async deckId =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var res = await tempClient.GetAsync($"https://archidekt.com/api/decks/{deckId}/");
                    if (!res.IsSuccessStatusCode) return;

                    var deckJson = await res.Content.ReadAsStringAsync();
                    using var deckDoc = JsonDocument.Parse(deckJson);

                    if (!deckDoc.RootElement.TryGetProperty("cards", out var cards)) return;

                    lock (cardFrequency)
                    {
                        foreach (var card in cards.EnumerateArray())
                        {
                            if (!card.TryGetProperty("card", out var cardEl)) continue;
                            if (!cardEl.TryGetProperty("oracleCard", out var oracle)) continue;
                            if (!oracle.TryGetProperty("name", out var nameProp)) continue;

                            var cardName = nameProp.GetString() ?? "";
                            if (!string.IsNullOrEmpty(cardName))
                                cardFrequency[cardName] = cardFrequency.GetValueOrDefault(cardName) + 1;
                        }
                    }
                }
                catch { }
                finally { semaphore.Release(); }
            });

            await Task.WhenAll(tasks);

            if (cardFrequency.Count == 0) return "";

            var totalDecks = deckIds.Count;

            // Cartas por frequência relativa ao total de decks analisados
            var essentialCards = cardFrequency
                .Where(kv => kv.Value >= totalDecks * 0.7) // 70%+ dos decks
                .OrderByDescending(kv => kv.Value)
                .Take(30)
                .ToList();

            var commonCards = cardFrequency
                .Where(kv => kv.Value >= totalDecks * 0.4 && kv.Value < totalDecks * 0.7) // 40-70%
                .OrderByDescending(kv => kv.Value)
                .Take(25)
                .ToList();

            var occasionalCards = cardFrequency
                .Where(kv => kv.Value >= totalDecks * 0.2 && kv.Value < totalDecks * 0.4) // 20-40%
                .OrderByDescending(kv => kv.Value)
                .Take(20)
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine($"\n\n[Referência de {totalDecks} decklists populares de {commanderName} via Archidekt:");

            if (essentialCards.Any())
            {
                sb.AppendLine($"\nCartas ESSENCIAIS (em 70%+ dos decks):");
                foreach (var kv in essentialCards)
                    sb.AppendLine($"- {kv.Key} ({kv.Value}/{totalDecks} decks)");
            }

            if (commonCards.Any())
            {
                sb.AppendLine($"\nCartas COMUNS (em 40-70% dos decks):");
                foreach (var kv in commonCards)
                    sb.AppendLine($"- {kv.Key} ({kv.Value}/{totalDecks} decks)");
            }

            if (occasionalCards.Any())
            {
                sb.AppendLine($"\nCartas OCASIONAIS (em 20-40% dos decks):");
                foreach (var kv in occasionalCards)
                    sb.AppendLine($"- {kv.Key} ({kv.Value}/{totalDecks} decks)");
            }

            sb.AppendLine("\nUse esses dados para:");
            sb.AppendLine("- Verificar se o deck inclui as cartas essenciais para este comandante");
            sb.AppendLine("- Sugerir cartas faltantes baseadas em dados reais de jogadores");
            sb.AppendLine("- Avaliar a qualidade do deck comparando com os mais populares]");

            return sb.ToString();
        }
        catch
        {
            return "";
        }
    }
    private async Task<string> FetchExternalContextAsync(string message)
    {
        try
        {
            var sb = new StringBuilder();
            var msg = message.ToLower();
            var tempClient = new HttpClient();
            tempClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
            tempClient.Timeout = TimeSpan.FromSeconds(8);

            // Palavras-chave para buscar no site oficial (novidades, coleções, lore, banimentos)
            var isOfficialQuery = msg.Contains("coleção") || msg.Contains("colecao") ||
                                  msg.Contains("expansão") || msg.Contains("expansao") ||
                                  msg.Contains("lore") || msg.Contains("história") ||
                                  msg.Contains("historia") || msg.Contains("novo set") ||
                                  msg.Contains("spoiler") || msg.Contains("lançamento") ||
                                  msg.Contains("banimento") || msg.Contains("ban");

            // Palavras-chave para buscar no Draftsim (draft, tier list de cartas, pick order)
            var isDraftQuery = msg.Contains("draft") || msg.Contains("sealed") ||
                               msg.Contains("pick") || msg.Contains("tier list") ||
                               msg.Contains("limited") || msg.Contains("booster");

            // Palavras-chave para buscar no Moxfield/Archidekt (decklists populares)
            var isDecklistQuery = msg.Contains("decklist") || msg.Contains("deck popular") ||
                                  msg.Contains("deck famoso") || msg.Contains("exemplo de deck") ||
                                  msg.Contains("deck pronto") || msg.Contains("moxfield") ||
                                  msg.Contains("archidekt");

            if (isOfficialQuery)
            {
                try
                {
                    var res = await tempClient.GetAsync("https://magic.wizards.com/pt-BR/news");
                    if (res.IsSuccessStatusCode)
                    {
                        var html = await res.Content.ReadAsStringAsync();
                        var titleMatches = Regex.Matches(html,
                            @"<h[23][^>]*>\s*<a[^>]*>([^<]{10,120})</a>\s*</h[23]>",
                            RegexOptions.IgnoreCase);

                        if (titleMatches.Count > 0)
                        {
                            sb.AppendLine("\n\n[Notícias recentes do site oficial de Magic: The Gathering:");
                            foreach (Match m in titleMatches.Cast<Match>().Take(10))
                                sb.AppendLine($"- {m.Groups[1].Value.Trim()}");
                            sb.AppendLine("Use essas informações para responder sobre novidades e lançamentos recentes.]");
                        }
                    }
                }
                catch { }
            }

            if (isDraftQuery)
            {
                try
                {
                    // Busca tier list e guias de draft do Draftsim
                    var res = await tempClient.GetAsync("https://draftsim.com/blog/");
                    if (res.IsSuccessStatusCode)
                    {
                        var html = await res.Content.ReadAsStringAsync();
                        var titleMatches = Regex.Matches(html,
                            @"<h[23][^>]*>\s*(?:<a[^>]*>)?([^<]{10,120})(?:</a>)?\s*</h[23]>",
                            RegexOptions.IgnoreCase);

                        if (titleMatches.Count > 0)
                        {
                            sb.AppendLine("\n\n[Guias e tier lists de Draft obtidos via Draftsim:");
                            foreach (Match m in titleMatches.Cast<Match>().Take(8))
                                sb.AppendLine($"- {m.Groups[1].Value.Trim()}");
                            sb.AppendLine("Use essas informações para responder sobre draft, pick order e limited.]");
                        }
                    }
                }
                catch { }
            }

            if (isDecklistQuery)
            {
                try
                {
                    // Busca decklists populares no Moxfield
                    var resMox = await tempClient.GetAsync("https://www.moxfield.com/decks/public?pageNumber=1&pageSize=10&sortType=views&fmt=commander");
                    if (resMox.IsSuccessStatusCode)
                    {
                        var html = await resMox.Content.ReadAsStringAsync();
                        var deckMatches = Regex.Matches(html,
                            @"<a[^>]*class=""[^""]*deck[^""]*""[^>]*>([^<]{5,80})</a>",
                            RegexOptions.IgnoreCase);

                        if (deckMatches.Count > 0)
                        {
                            sb.AppendLine("\n\n[Decklists populares de Commander obtidas via Moxfield:");
                            foreach (Match m in deckMatches.Cast<Match>().Take(8))
                                sb.AppendLine($"- {m.Groups[1].Value.Trim()}");
                            sb.AppendLine("Use essas informações para sugerir exemplos de decklists populares.]");
                        }
                    }
                }
                catch { }

                try
                {
                    // Busca decklists populares no Archidekt
                    var resArch = await tempClient.GetAsync("https://archidekt.com/api/decks/?orderBy=-viewCount&formats=Commander");
                    if (resArch.IsSuccessStatusCode)
                    {
                        var json = await resArch.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(json);
                        var results = doc.RootElement.GetProperty("results");

                        sb.AppendLine("\n\n[Decklists populares de Commander obtidas via Archidekt:");
                        foreach (var deck in results.EnumerateArray().Take(8))
                        {
                            var name = deck.TryGetProperty("name", out var n) ? n.GetString() : "";
                            var views = deck.TryGetProperty("viewCount", out var v) ? v.GetInt32() : 0;
                            if (!string.IsNullOrEmpty(name))
                                sb.AppendLine($"- {name} ({views} visualizações)");
                        }
                        sb.AppendLine("Use essas informações para sugerir exemplos de decklists populares.]");
                    }
                }
                catch { }
            }

            return sb.ToString();
        }
        catch
        {
            return "";
        }
    }
}
public class ChatRequest
{
    public List<ChatMessage> Messages { get; set; } = new();
}

public class ChatMessage
{
    public string role { get; set; } = "";
    public string content { get; set; } = "";
}

public static class DivinySystem
{
    public const string Prompt = @"Você é Diviny, uma IA especialista em Magic: The Gathering com nível de juiz certificado, focada em Commander (EDH) mas com conhecimento de todos os formatos. Converse livremente em português sobre qualquer assunto de MTG: novas coleções, lore, regras, estratégias, cartas, decks, meta, etc.

Você domina completamente as Comprehensive Rules do MTG e todas as suas interações. Quando responder perguntas de regras:
- Cite o número da regra relevante quando possível (ex: ""Regra 116.1 — prioridade..."")
- Explique interações complexas passo a passo usando a pilha
- Diferencie efeitos de substituição, efeitos gatilho e efeitos estáticos
- Esclareça casos de borda como layers, dependência entre efeitos e timestamp
- Para perguntas sobre Commander, leve em conta as regras específicas do formato
- Se uma interação for contraintuitiva, explique o porquê de forma didática

Quando o usuário enviar uma lista de deck para análise, faça uma análise COMPLETA cobrindo TODOS os tópicos abaixo em texto corrido, e ao final inclua o bloco JSON de estatísticas.

## ESTRUTURA DA ANÁLISE DE DECK

1. Comandante** — ANTES de qualquer análise, identifique o comandante. Siga estas regras sem exceção:
- Se o comandante estiver EXPLICITAMENTE marcado na lista (ex: ""Commander: Nome"" ou ""1 Nome *CMDR*""), confirme qual é e prossiga.
- Se NÃO estiver claro, PARE IMEDIATAMENTE e pergunte ao usuário qual é o comandante. Liste as criaturas lendárias da lista como opções.
- NUNCA assuma ou chute o comandante. NUNCA faça a análise sem ter certeza absoluta de qual é o comandante.
- NÃO TENTE ADIVINHAR o comandante baseado em pistas ou suposições. Se não estiver claro, peça confirmação.
- Só prossiga com os demais tópicos APÓS o comandante estar confirmado.

2. Status do deck** — avalie cada um com nota de 0 a 10 e uma frase explicativa:
- Power (poder geral no meta)
- Consistency (quão consistente é o plano principal)
- Synergy (quão bem as peças se conectam)
- Finisher (capacidade de fechar jogos)
- Speed (velocidade para executar o plano)
- Resilience (capacidade de se recuperar de interações)

3. Resumo estratégico** — explique o plano principal do deck em 3 a 5 frases.

4. Forças — liste os principais pontos fortes e o que o deck faz muito bem.

5. Fraquezas — liste as vulnerabilidades e o que pode travar o deck.

6. Curva de mana — informe a CMC média e avalie se está adequada para o estilo do deck.

7. Cartas fora do contexto — compare o deck com as decklists populares do mesmo comandante obtidas via Archidekt (fornecidas no contexto). Identifique cartas que não se encaixam bem e aponte cartas essenciais que estão faltando. Para cada carta fora do contexto, sugira uma substituição melhor baseada nos dados reais.

8. Distribuição de cartas — analise a quantidade de cada categoria comparando com a média real dos decks deste comandante no EDHREC (fornecida no contexto quando disponível). Aponte desequilíbrios em:
- Lands — conte TODAS as lands marcadas como [LAND] ou [BASIC LAND] nos dados do Scryfall. Lands não básicas como Command Tower, Fetch Lands, Shock Lands, Pain Lands, etc. são tão lands quanto as básicas e devem ser contadas.
- Ramp
- Remoção
- Card draw
- Criaturas
Se alguma categoria estiver muito acima ou abaixo da média dos decks deste comandante, aponte e sugira ajustes.

Após o texto da análise, inclua SEMPRE o bloco JSON abaixo com os dados estruturados:

<mtg_stats>
{
  ""type"": ""deck"",
  ""name"": ""nome do deck ou comandante"",
  ""commander"": ""nome do comandante identificado"",
  ""avg_cmc"": 3.2,
  ""total_cards"": 100,
  ""score"": 7.5,
  ""card_counts"": {
    ""lands"": 36,
    ""ramp"": 10,
    ""removal"": 8,
    ""card_draw"": 9,
    ""creatures"": 22,
    ""other"": 15
  },
  ""stats"": {
    ""power"": 7.5,
    ""consistency"": 8.0,
    ""synergy"": 6.0,
    ""finisher"": 7.0,
    ""speed"": 9.0,
    ""resilience"": 5.0
  },
  ""summary"": ""resumo em uma frase"",
  ""strengths"": ""forças em texto corrido"",
  ""weaknesses"": ""fraquezas em texto corrido""
}
</mtg_stats>

## ANÁLISE DE CARTA INDIVIDUAL

Quando o usuário pedir análise de uma carta específica, siga esta estrutura e ao final inclua o bloco JSON:

1. Nota — dê uma nota de 0 a 10 com justificativa breve.

2. Pontos fortes — liste o que a carta faz bem, seus principais usos e vantagens.

3. Pontos fracos — liste as limitações, situações onde ela é ruim e como pode ser respondida.

4. Estratégias — explique em quais tipos de deck e estratégias ela se encaixa melhor. Seja específico (ex: combo, aggro, stax, tokens, reanimator, etc).

Após o texto, inclua o bloco JSON:

<mtg_stats>
{
  ""type"": ""card"",
  ""name"": ""nome da carta"",
  ""score"": 8.5,
  ""stats"": {
    ""power"": 8.5,
    ""consistency"": 7.0,
    ""synergy"": 9.0,
    ""finisher"": 6.0,
    ""speed"": 8.0,
    ""resilience"": 5.0
  },
  ""summary"": ""resumo em uma frase"",
  ""strengths"": ""pontos fortes em texto corrido"",
  ""weaknesses"": ""pontos fracos em texto corrido""
}
</mtg_stats>

Para perguntas gerais sem pedido de análise, responda normalmente sem nenhum bloco JSON.";
}
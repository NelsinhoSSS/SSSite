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

        // Busca dados do EDHREC se houver comandante identificável
        var edhrecContext = await FetchEdhrecDataAsync(lastMessage);

        // Se a mensagem parece uma lista de deck, busca dados do Scryfall
        var scryfallContext = await FetchScryfallContextAsync(lastMessage);

        var messages = new List<object>
        {
            new { role = "system", content = DivinySystem.Prompt + edhrecContext + scryfallContext }
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

    // Busca dados reais das cartas no Scryfall para enriquecer o contexto
    private async Task<string> FetchScryfallContextAsync(string message)
    {
        try
        {
            // Detecta se a mensagem parece uma lista de deck (linhas com "1 Nome da Carta")
            var lines = message.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var cardLines = lines
                .Select(l => Regex.Match(l.Trim(), @"^\d+x?\s+(.+)$"))
                .Where(m => m.Success)
                .Select(m => m.Groups[1].Value.Trim())
                .Distinct()
                .Take(20) // Limita a 20 cartas para não sobrecarregar
                .ToList();

            if (cardLines.Count < 3) return ""; // Não parece uma lista

            var tempClient = new HttpClient();
            var sb = new StringBuilder();
            sb.AppendLine("\n\n[Dados reais das cartas obtidos via Scryfall:");

            var tasks = cardLines.Select(async name =>
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
                    var manaCost = root.TryGetProperty("mana_cost", out var mc) ? mc.GetString() : "";
                    var cmc = root.TryGetProperty("cmc", out var c) ? c.GetDecimal() : 0;
                    var oracleText = root.TryGetProperty("oracle_text", out var ot) ? ot.GetString() : "";
                    var edhrecRank = root.TryGetProperty("edhrec_rank", out var er) ? er.GetInt32() : 0;

                    return $"- {cardName} | {typeLine} | Custo: {manaCost} (CMC {cmc}) | EDHREC rank: #{edhrecRank} | Texto: {oracleText?.Replace("\n", " ").Trim()[..Math.Min(120, oracleText?.Length ?? 0)]}";
                }
                catch { return null; }
            });

            var results = await Task.WhenAll(tasks);
            foreach (var r in results.Where(r => r != null))
                sb.AppendLine(r);

            sb.AppendLine("Use esses dados para identificar corretamente cada carta e suas funções no deck.]");
            return sb.ToString();
        }
        catch
        {
            return "";
        }
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

**1. Comandante** — identifique o comandante da lista. Se não estiver claro:
- PARE a análise
- Pergunte ao usuário qual é o comandante antes de continuar
- Liste as opções possíveis da lista para facilitar a escolha
- Aguarde a resposta antes de prosseguir com a análise completa

**2. Status do deck** — avalie cada um com nota de 0 a 10 e uma frase explicativa:
- Power (poder geral no meta)
- Consistency (quão consistente é o plano principal)
- Synergy (quão bem as peças se conectam)
- Finisher (capacidade de fechar jogos)
- Speed (velocidade para executar o plano)
- Resilience (capacidade de se recuperar de interações)

**3. Resumo estratégico** — explique o plano principal do deck em 3 a 5 frases.

**4. Forças** — liste os principais pontos fortes e o que o deck faz muito bem.

**5. Fraquezas** — liste as vulnerabilidades e o que pode travar o deck.

**6. Curva de mana** — informe a CMC média e avalie se está adequada para o estilo do deck.

**7. Cartas fora do contexto** — identifique cartas que não se encaixam bem. Para cada uma, sugira uma substituição melhor e explique o porquê.

**8. Distribuição de cartas** — analise a quantidade de cada categoria comparando com a média real dos decks deste comandante no EDHREC (fornecida no contexto quando disponível). Aponte desequilíbrios em:
- Lands
- Ramp
- Remoção
- Card draw / geração de vantagem
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

Para análise de CARTAS individuais use o formato simplificado sem os campos de deck.
Para perguntas gerais sem pedido de análise, responda normalmente sem nenhum bloco JSON.";
}
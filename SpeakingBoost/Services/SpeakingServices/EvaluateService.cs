using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Diagnostics;

namespace SpeakingBoost.Services.SpeakingServices
{
    public class EvaluateService
    {
        private readonly HttpClient _client;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly ILogger<EvaluateService> _logger;

        public EvaluateService(HttpClient client, IConfiguration config, ILogger<EvaluateService> logger)
        {
            _client = client;
            _logger = logger;

            _apiKey = config["OpenAI:ApiKey"]
                ?? throw new InvalidOperationException("OpenAI Text ApiKey missing");

            // ? default sang gpt-5-mini (b?n v?n có th? override trong appsettings)
            _model = config["OpenAI:Model"] ?? "gpt-5-mini";

            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<string> EvaluateAsync(
            string transcript,
            string question,
            int part,
            int wordCount,
            double durationSec)
        {
            transcript ??= "";
            question ??= "";

            var prompt = "";


///////////////////Part 1 Prompt///////////////////////////////////
            if (part==1)
                prompt = $@"
TASK
Evaluate the candidate’s SPOKEN English for IELTS Speaking PART 1 (Interview).
Score ONLY:
1) Fluency and Coherence
2) Lexical Resource
3) Grammatical Range and Accuracy
Do NOT evaluate pronunciation.

TRANSCRIPT:
{transcript}

QUESTION:
{question}

PART 1 FOCUS
- FC: respond directly to questions, short but complete answers, natural turn-taking, basic linking (because/so/also), minimal awkward pauses.
- LR: topic vocabulary, natural common phrases/collocations.
- GRA: simple accuracy, tense appropriate to the question, complete clauses (avoid fragments), agreement; major breakdowns matter most.

SCORING RULES (STRICT)
- Score spoken ability only (not writing).
- Band scores in 0.5 or 1.0 steps only.
- CRITICAL: Distinguish between STT (Speech-to-Text) errors and User errors.
  > If the text is nonsensical (e.g., ""I went to the bitch"" instead of ""beach""), mark it as an STT error and IGNORE it in scoring.
  > If it is a grammatical/lexical error (e.g., ""I goed to the beach""), score it.

ISSUE RULES (VERY STRICT)
- Issues must be short PHRASES only.
- ""wrong"" MUST be an EXACT substring from the transcript (character-for-character). If you cannot find the exact substring, do not list the issue.
- ""right"" must preserve original meaning but be clearer/more natural.
- ""description"" explain the difference between the right and wrong(1 or 2 sentence).
- Max 5 issues per category (focus on the most impactful ones).

SUGGESTION ANSWER RULES
- Create a model answer that corrects the user's mistakes.
- LENGTH: about 4 sentences.
- CONTENT: a better version based ONLY on transcript content that uses more high-level topic-related vocabulary and idiomatic expressions.
- STYLE: Conversational, direct, natural spoken English.
- HIGHLIGHTING: Wrap ONLY the upgraded high-level vocabulary or idioms in this format: [English Word|Vietnamese Meaning BASED ON CONTEXT].

OUTPUT JSON FORMAT (fill with real values)
- Return ONLY valid JSON. No markdown, no extra text.
{{
  ""fluency_coherence"": {{
    ""score"": 0,
    ""issues"": [
      {{
        ""wrong"": """",
        ""right"": """",
        ""description"": """"
      }}
    ],
    ""issue_count"": 0
  }},

  ""lexical_resource"": {{
    ""score"": 0,
    ""issues"": [
      {{
        ""wrong"": """",
        ""right"": """",
        ""description"": """"
      }}
    ],
    ""issue_count"": 0
  }},

  ""grammar"": {{
    ""score"": 0,
    ""issues"": [
      {{
        ""wrong"": """",
        ""right"": """",
        ""description"": """"
      }}
    ],
    ""issue_count"": 0
  }},

  ""suggestion_answer"": """"
}}
";


            ///////////////////Part 2 Prompt///////////////////////////////////
            else if (part==2)
                prompt = $@"
TASK
Evaluate the candidate’s SPOKEN English for IELTS Speaking PART 2 (Long Turn).
Score ONLY:
1) Fluency and Coherence
2) Lexical Resource
3) Grammatical Range and Accuracy
Do NOT evaluate pronunciation.

TRANSCRIPT:
{transcript}

QUESTION / CUE:
{question}


SCORING RULES (STRICT)
- Score spoken ability only (not writing).
- Band scores in 0.5 or 1.0 steps only.
- CRITICAL: Distinguish between STT (Speech-to-Text) errors and User errors.
  > If the text is nonsensical (e.g., ""I went to the bitch"" instead of ""beach""), mark it as an STT error and IGNORE it in scoring.
  > If it is a grammatical/lexical error (e.g., ""I goed to the beach""), score it.



PART 2 FOCUS
- FC: clear structure (opening ? main points ? ending), sequencing, cohesive devices, sustained flow.
- LR: topic-specific vocabulary, precise word choice, natural collocations; penalize excessive repetition.
- GRA: variety (simple + complex), clause control, tense consistency, fewer breakdowns.

ISSUE RULES (VERY STRICT)
- Issues must be short PHRASES only.
- ""wrong"" MUST be an EXACT substring from the transcript (character-for-character). If you cannot find the exact substring, do not list the issue.
- ""right"" must preserve original meaning but be clearer/more natural.
- ""description"" explain the difference between the right and wrong(1 or 2 sentence).
- Max 7 issues per category. Prefer fewer, high-impact issues.


SUGGESTION ANSWER RULES (PART 2 SPECIFIC)
- Create a complete model speech (Long Turn) based on the user's story.
- LENGTH: Approx. 220–260 words (Targeting ~2 minutes of speaking).
- STRUCTURE: Organize the answer logically with clear linking words:
  1. Introduction (Directly state what you are going to talk about).
  2. Body Paragraphs (Details: Who, When, Where, What happened - expand on the user's points).
  3. Conclusion (Explain why it was significant/how you felt).
- CONTENT:
  > Keep the user's core story/facts.
  > If the transcript is too short (< 1 minute), you MUST expand on the user's ideas with logical details to meet the length requirement.
  > Upgrade the vocabulary to Band 7+ (idioms, collocations, phrasal verbs).
- STYLE: Descriptive, engaging, and emotional (Storytelling style). Use narrative tenses correctly.
- HIGHLIGHTING: Wrap ONLY the upgraded high-level vocabulary or idioms in this format: [English Word|Vietnamese Meaning BASED ON CONTEXT].

OUTPUT JSON FORMAT (fill with real values)
- Return ONLY valid JSON. No markdown, no extra text.
{{
  ""fluency_coherence"": {{
    ""score"": 0,
    ""issues"": [
      {{
        ""wrong"": """",
        ""right"": """",
        ""description"": """"
      }}    
    ],
    ""issue_count"": 0
  }},

  ""lexical_resource"": {{
    ""score"": 0,
    ""issues"": [
      {{
        ""wrong"": """",
        ""right"": """",
        ""description"": """"
      }}
    ],
    ""issue_count"": 0
  }},

  ""grammar"": {{
    ""score"": 0,
    ""issues"": [
      {{
        ""wrong"": """",
        ""right"": """",
        ""description"": """"
      }}
    ],
    ""issue_count"": 0
  }},

  ""suggestion_answer"": """"
}}
";

///////////////////Part 3 Prompt///////////////////////////////////
            else if (part ==3)
                prompt = $@"
TASK
Evaluate the candidate’s SPOKEN English for IELTS Speaking PART 3 (Discussion).
Score ONLY:
1) Fluency and Coherence
2) Lexical Resource
3) Grammatical Range and Accuracy
Do NOT evaluate pronunciation.

TRANSCRIPT:
{transcript}

QUESTION / QUESTIONS:
{question}

SCORING RULES (STRICT)
- Score spoken ability only (not writing).
- Band scores in 0.5 or 1.0 steps only.
- CRITICAL: Distinguish between STT (Speech-to-Text) errors and User errors.
  > If the text is nonsensical (e.g., ""I went to the bitch"" instead of ""beach""), mark it as an STT error and IGNORE it in scoring.
  > If it is a grammatical/lexical error (e.g., ""I goed to the beach""), score it.

PART 3 FOCUS
- FC: develop ideas with reasons/examples, clear stance, comparisons, signposting (In my view/For example/Overall).
- LR: more abstract/topic-related vocabulary, precise word choice, paraphrasing, natural collocations; avoid awkward translated phrases.
- GRA: more complex structures (subordinate clauses, conditionals, relative clauses) with control; accuracy under complexity, fewer breakdowns.

ISSUE RULES (VERY STRICT)
- Issues must be short PHRASES only.
- ""wrong"" MUST be an EXACT substring from the transcript (character-for-character). If you cannot find the exact substring, do not list the issue.
- ""right"" must preserve original meaning but be clearer/more natural.
- ""description"" explain the difference between the right and wrong(1 or 2 sentence).
- Max 7 issues per category. Prefer fewer, high-impact issues.


SUGGESTION ANSWER RULES:
- ""suggestion_answer"": Generate a Band 8.0+ model answer based on the user's core ideas.
- CONTENT: Preserve the user's original viewpoint but EXPAND logical reasoning if the transcript is too brief.
- STYLE (Part 3 Focus): Shift from personal anecdotes to general/abstract discussion. Use formal spoken register and advanced cohesive devices.
- STRUCTURE: Follow the P.E.E format (Point -> Explanation -> Example).
- LENGTH: Concise but fully developed (approx. 4-6 sentences, ~100 words).
- HIGHLIGHTING: Wrap ONLY the upgraded high-level vocabulary or idioms in this format: [English Word|Vietnamese Meaning BASED ON CONTEXT].

OUTPUT JSON FORMAT (fill with real values)
- Return ONLY valid JSON. No markdown, no extra text.
{{
  ""fluency_coherence"": {{
    ""score"": 0,
    ""issues"": [
      {{
        ""wrong"": """",
        ""right"": """",
        ""description"": """"
      }}
    ],
    ""issue_count"": 0
  }},

  ""lexical_resource"": {{
    ""score"": 0,
    ""issues"": [
      {{
        ""wrong"": """",
        ""right"": """",
        ""description"": """"
      }}
    ],
    ""issue_count"": 0
  }},

  ""grammar"": {{
    ""score"": 0,
    ""issues"": [
      {{
        ""wrong"": """",
        ""right"": """",
        ""description"": """"
      }}
    ],
    ""issue_count"": 0
  }},

  ""suggestion_answer"": """"
}}
";


            // ? Dùng Dictionary d? “omit field” temperature khi là GPT-5-mini


            var maxToken = part switch
            {
                1 => 4000,
                2 => 6000,
                3 => 5000
            };
                
            




                var body = new Dictionary<string, object?>
                {
                    ["model"] = _model,
                    ["messages"] = new object[]
                            {
                    new { role = "system", content = "You are an IELTS Speaking examiner and your task is to assess a Vietnamese speaker's transcript given the TRANSCRIPT and QUESTION" },
                    new { role = "user", content = prompt }
                            },
                    // ? KHÔNG g?i temperature/top_p/logprobs v?i gpt-5-mini :contentReference[oaicite:2]{index=2}
                    ["max_completion_tokens"] =maxToken ,
                    ["response_format"] = new { type = "json_object" } // JSON mode v?n dùng du?c trên Chat Completions :contentReference[oaicite:3]{index=3}
                };

            var jsonBody = JsonSerializer.Serialize(body);
            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var sw = Stopwatch.StartNew();
            var response = await _client.PostAsync("https://api.openai.com/v1/chat/completions", content);
            var responseText = await response.Content.ReadAsStringAsync();
            sw.Stop();



            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"OpenAI request failed ({(int)response.StatusCode}): {responseText}");

            _logger.LogInformation(
                "Evaluate success | Part={Part} | Model={Model} | PromptChars={PromptChars} | ResponseChars={ResponseChars} | LatencyMs={LatencyMs}",
                part,
                _model,
                prompt.Length,
                responseText.Length,
                sw.ElapsedMilliseconds);

            using var rawDoc = JsonDocument.Parse(responseText);
            var root = rawDoc.RootElement;
            var choices = root.GetProperty("choices");
            var firstChoice = choices[0];
            var finishReason = firstChoice.TryGetProperty("finish_reason", out var fr) ? fr.GetString() : null;

            if (string.Equals(finishReason, "length", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"OpenAI response truncated due to max token limit (part={part}).");
            }

            string resultJson = firstChoice
                .GetProperty("message")
                .GetProperty("content")
                .GetString()
                ?? "{}";

            JsonDocument.Parse(resultJson); // validate JSON
            return resultJson;
        }
    }
}

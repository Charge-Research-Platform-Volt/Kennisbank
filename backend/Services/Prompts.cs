using HandlebarsDotNet;

namespace KnowledgeBank.Services;

// This file contains the prompts used by the AI assistant.
/// <summary>
/// The Prompts class contains predefined prompts for various AI tasks such as question answering, tag generation, and chat title generation.
/// It includes system prompts, templates for generating specific prompts, and JSON schema definitions for output formats.
/// </summary>
/// <remarks>
/// The prompts are designed to guide the AI assistant in providing accurate and relevant responses based on the provided information.
/// The class uses Handlebars templates for dynamic prompt generation, allowing for flexible and reusable prompt structures.
/// The JSON schema definitions ensure that the output formats are consistent and can be easily validated.
/// </remarks>
public class Prompts
{
    #region System Prompts

    public const string SystemContentBasedAi = @"
        You are an AI assistant that answers questions based on the provided information.
        You will be provided with a question and a set of relevant information sources. Answer the question using the information provided.

        CITATION FORMAT:
        - Each source has a pre-assigned ""Source Number"" — use it exactly, do not renumber.
        - Cite inline using the pre-assigned number as a markdown link: [1](link), [2](link), etc.
        - ALWAYS include the full link — NEVER use bare [1] or [2] without (link). Bare numbers are WRONG.
        - The same source always gets the same number and link throughout the response.
        - Do NOT repeat the full title inline — just the number.
        - At the very end, add a ""Sources"" section as a numbered markdown list using the pre-assigned numbers:
        1. [Exact Source Title](link)
        2. [Exact Source Title](link)

        Instructions:
        - Base your answer on the provided information.
        - English is the preferred language for responses.
        - Be concise, accurate, and directly address the question.
        - Be aware that the question may refer to chat history, so consider it when formulating your answer.
        - If the information does not answer the question, state that explicitly and do not include any citations.
        - Links are provided in the format: /library?inspectorId=<guid>&inspectorType=<type> - use them exactly as provided (starting with a slash, no http/https).
        - For math use LaTeX syntax. Use double dollar signs for display math, e.g. $$E=mc^2$$, and single dollar signs for inline math, e.g. $x^2 + y^2 = z^2$.
        ";

    public const string SystemContentBasedAiWithScores = @"
        You are an AI assistant that answers questions based on the provided information from a knowledge base.
        You will receive information sources ranked by relevance, with each source having a relevance level (High/Medium/Low) and a numerical score.

        CITATION FORMAT:
        - Each source has a pre-assigned ""Source Number"" — use it exactly, do not renumber.
        - Cite inline using the pre-assigned number as a markdown link: [1](link), [2](link), etc.
        - ALWAYS include the full link — NEVER use bare [1] or [2] without (link). Bare numbers are WRONG.
        - The same source always gets the same number and link throughout the response.
        - Do NOT repeat the full title inline — just the number.
        - At the very end, add a ""Sources"" section as a numbered markdown list using the pre-assigned numbers:
        1. [Exact Source Title](link)
        2. [Exact Source Title](link)

        CORRECT examples:
        ✓ Framing effects were observed [1](/library?inspectorId=abc&inspectorType=resource) and confirmed in follow-up studies [1](/library?inspectorId=abc&inspectorType=resource)[2](/library?inspectorId=xyz&inspectorType=resource).
        ✓ **Sources**
        1. [Food Recommender Systems](/library?inspectorId=abc&inspectorType=resource)
        2. [Machine Learning in Healthcare](/library?inspectorId=xyz&inspectorType=resource)

        WRONG examples (NEVER use these):
        ✗ [Food Recommender Systems](/library?inspectorId=abc&inspectorType=resource) — do not use full titles inline
        ✗ [Source 1](/library?inspectorId=abc&inspectorType=resource) — do not use ""Source N"" format

        Instructions:
        - Base your answer ONLY on the provided information sources.
        - English is the preferred language for responses.
        - Be concise, accurate, and directly address the question.
        - Consider chat history when formulating your answer - the question may refer to previous messages.
        - Prioritize information from sources with higher relevance scores when formulating your answer.
        - High relevance (0.7+): Very likely to be directly relevant - use as primary sources
        - Medium relevance (0.4-0.7): Likely to be somewhat relevant - use as supporting evidence
        - Low relevance (<0.4): May be tangentially related - use sparingly and with caution
        - If the provided information does not sufficiently answer the question, acknowledge this limitation and explain what information is missing.
        - Links are provided in the format: /library?inspectorId=<guid>&inspectorType=<type> - use them exactly as provided (starting with a slash, no http/https).
        - For math use LaTeX syntax: $$E=mc^2$$ for display math, $x^2$ for inline math.
        - When sources have low relevance scores, mention this uncertainty in your response (e.g., 'Based on potentially related sources...').
        ";

    public const string SystemPromptStandardAi = @"
        You are an AI assistant that helps people find information.
        For math use LaTeX syntax. Use double dollar signs for display math, e.g. $$E=mc^2$$, and single dollar signs for inline math, e.g. $x^2 + y^2 = z^2$.
        ";

    public const string SystemPromptGenerateTitle = @"You are a helpful AI assistant that generates a concise and descriptive title for a chat based on the provided question. Return the title as a JSON object with a single field 'Title'";

    #endregion

    #region Prompts for Question Answering
    private const string GenerateQuestionAnsweringPromptTemplate = @"
        The question:
        {{query}}

        Relevant Information:
        {{#each content}}
        Source Number: {{SourceNumber}}
        Text: {{text}}
        Link: {{link}}
        ---
        {{/each}}
        ";

    /// <summary>
    /// Gets a compiled Handlebars template for generating question-answering prompts.
    /// This template is pre-compiled from the GenerateAnswerPromptTemplate and can be used
    /// to render question-answering prompts with dynamic data.
    /// </summary>
    /// <value>
    /// A compiled Handlebars template that accepts an object as input and returns an object as output.
    /// </value>
    public static HandlebarsTemplate<object, object> QuestionAnsweringTemplate { get; } =
        Handlebars.Compile(GenerateQuestionAnsweringPromptTemplate);

    private const string GenerateQuestionAnsweringWithScoresPromptTemplate = @"
        The question:
        {{query}}

        Relevant Information Sources (ranked by relevance):
        {{#each content}}
        ---
        Source Number: {{SourceNumber}}
        Source Title: {{Title}}
        Relevance Level: {{RelevanceLevel}} (Score: {{RelevanceScore}})
        Text: {{Text}}
        Link: {{Link}}
        {{/each}}
        ---

        CITATION FORMAT REMINDER:
        When citing sources in your answer, you MUST use this exact format:
        [Source Title](Link)

        For example, if the source title is ""{{content.0.Title}}"" and the link is ""{{content.0.Link}}"", cite it as:
        [{{content.0.Title}}]({{content.0.Link}})

        Do NOT use [1], [2], [Source 1], etc. ALWAYS use the actual source title from above.
        ";

    /// <summary>
    /// Gets a compiled Handlebars template for generating question-answering prompts with relevance scores.
    /// This template includes relevance information to help the AI prioritize more relevant sources.
    /// </summary>
    /// <value>
    /// A compiled Handlebars template that accepts an object as input and returns an object as output.
    /// </value>
    public static HandlebarsTemplate<object, object> QuestionAnsweringWithScoresTemplate { get; } =
        Handlebars.Compile(GenerateQuestionAnsweringWithScoresPromptTemplate);

    #endregion

    public static string MetadataExtractionOutputJsonSchema { get; } = """
        {
            "title": "Metadata Extraction",
            "type": "object",
            "properties": {
                "title": { "anyOf": [{"type": "string"}, {"type": "null"}], "description": "The document title" },
                "abstract": { "anyOf": [{"type": "string"}, {"type": "null"}], "description": "The abstract of the paper when scientific, otherwise null" },
                "description": { "anyOf": [{"type": "string"}, {"type": "null"}], "description": "A concise description of the document (50-300 words)" },
                "publicationDate": { "anyOf": [{"type": "string"}, {"type": "null"}], "description": "Publication date in YYYY, YYYY-MM, or YYYY-MM-DD format, or null" },
                "languageCode": { "anyOf": [{"type": "string"}, {"type": "null"}], "description": "ISO 639-1 two-letter language code, or null" },
                "authors": {
                    "type": "array",
                    "description": "Document authors (persons or organisations)",
                    "items": {
                        "type": "object",
                        "properties": {
                            "name": { "type": "string" },
                            "type": { "type": "string", "enum": ["person", "organisation"] }
                        },
                        "required": ["name", "type"],
                        "additionalProperties": false
                    }
                },
                "organisations": {
                    "type": "array",
                    "description": "Organisation names associated with the document",
                    "items": { "type": "string" }
                },
                "relatedPersons": {
                    "type": "array",
                    "description": "Person names mentioned in the document who are not authors",
                    "items": { "type": "string" }
                },
                "publicationCode": { "anyOf": [{"type": "string"}, {"type": "null"}], "description": "DOI, ISBN, arXiv ID, or other identifier, or null" },
                "tags": {
                    "type": "array",
                    "description": "Relevant tags for the document",
                    "items": { "type": "string" }
                }
            },
            "required": ["title", "abstract", "description", "publicationDate", "languageCode", "authors", "organisations", "relatedPersons", "publicationCode", "tags"],
            "additionalProperties": false
        }
    """;

    #region Prompts for Chat Title Generation
    private const string GenerateChatTitlePromptTemplate = @"
    Generate a concise and descriptive title for a chat based on the provided question. The title should capture the essence of the question and be suitable for use as a chat title.
    The title should be in English and should be short, ideally no more than 10 words.

    The question is:
    {{query}}
    ";

    /// <summary>
    /// Gets a compiled Handlebars template for generating chat titles.
    /// This template is pre-compiled from the GenerateChatTitlePromptTemplate and can be used
    /// to render chat title prompts with dynamic data.
    /// </summary>
    /// <value>
    /// A compiled Handlebars template that accepts an object as input and returns an object as output.
    /// </value>
    public static HandlebarsTemplate<object, object> ChatTitleTemplate { get; } =
        Handlebars.Compile(GenerateChatTitlePromptTemplate);

    /// <summary>
    /// Gets the JSON schema definition for the chat title generation output format.
    /// This schema defines the structure for API responses containing generated chat titles.
    /// </summary>
    /// <value>
    /// A JSON schema string that specifies:
    /// - An object with a required "Title" property
    /// - The "Title" property as a string
    /// - Example values showing the expected format: "Generated Chat Title"
    /// </value>
    public static string ChatTitleOutputJsonSchema { get; } = """
        {
            "title": "Title Generation",
            "type": "object",
            "properties": {
                "Title": {
                    "type": "string",
                    "description": "The generated title for the chat.",
                    "example": "Generated Chat Title"
                }
            },
            "required": ["Title"]
        }
    """;
    #endregion
}

public class TitleGeneration
{
    public required string Title { get; set; }
}

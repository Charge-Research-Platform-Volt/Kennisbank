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

    public const string SystemContentBasedAi = @"You are an AI assistant that answers questions based on the provided information.
You will be provided with a question and a set of relevant information sources. Answer the question using the information provided.
For each statement or claim in your answer, include an in-text citation referencing the specific source(s) (using the provided links) that support your response.

Instructions:
- Base your answer on the provided information.
- English is the preferred language for responses.
- Be concise, accurate, and directly address the question.
- Be aware that the question may refer to chat history, so consider it when formulating your answer.
- If the information does not answer the question, state that explicitly and do not include any citations.
- For each fact or claim, include a citation in the format: [Source Number](Source Link). Source Number corresponds to the link, two sources with the same link should have the same number.
- Links will be provided in the format like this: /archive/?id=19e8c737-1f10-45a5-b476-8e0fbd9e7647
- For links do not add http:// or https://, just use the path like this: /archive/?id=19e8c737-1f10-45a5-b476-8e0fbd9e7647
- For math use LaTeX syntax. Use double dollar signs for display math, e.g. $$E=mc^2$$, and single dollar signs for inline math, e.g. $x^2 + y^2 = z^2$.";

    public const string SystemPromptStandardAi = @"You are an AI assistant that helps people find information.
For math use LaTeX syntax. Use double dollar signs for display math, e.g. $$E=mc^2$$, and single dollar signs for inline math, e.g. $x^2 + y^2 = z^2$.";

    public const string SystemPromptGenerateTitle = @"You are a helpful AI assistant that generates a concise and descriptive title for a chat based on the provided question. Return the title as a JSON object with a single field 'Title'";

    public const string SystemPromptGenerateTags = @"You are a helpful AI assistant that extracts tags from a document and returns them as a JSON";

    #endregion


    #region Prompts for Question Answering
    private const string GenerateQuestionAnsweringPromptTemplate =
@"The question:
{{query}}

Relevant Information:
{{#each content}}
Text: {{text}}
Link: {{link}}
--- 
{{/each}}";

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

    #endregion


    #region Prompts for Document Processing and Tag Generation
    private const string GenerateTagsPromptTemplate =
@"You are tasked with generating relevant tags for a large document, which is provided to you in smaller chunks. For each batch, you will receive:
- A list of tags that have already been generated for the document.
- The current group of content chunks.

Instructions:
1. Carefully read the provided content chunks.
2. Generate new, relevant tags that accurately reflect the content of these chunks.
3. Do not repeat or include any tags that have already been generated (see the list below).
4. The tags should be in English.
5. Generate 4 to 6 new tags based on the content provided. 
6. Ensure all tags are concise, specific, and directly related to the content.

Previously Generated Tags:
{{tags}}

Current Document Chunks:
{{#each content}}
{{text}}
---
{{/each}}";


    /// <summary>
    /// Gets a compiled Handlebars template for generating tags-related prompts.
    /// This template is pre-compiled from the GenerateTagsPromptTemplate and can be used
    /// to render tag generation prompts with dynamic data.
    /// </summary>
    /// <value>
    /// A compiled Handlebars template that accepts an object as input and returns an object as output.
    /// </value>
    public static HandlebarsTemplate<object, object> TagsTemplate { get; } =
        Handlebars.Compile(GenerateTagsPromptTemplate);


    /// <summary>
    /// Gets the JSON schema definition for the tags extraction output format.
    /// This schema defines the structure for API responses containing extracted tags from document chunks.
    /// </summary>
    /// <value>
    /// A JSON schema string that specifies:
    /// - An object with a required "Tags" property
    /// - The "Tags" property as an array of strings
    /// - Example values showing the expected format: ["tag1", "tag2", "tag3"]
    /// </value>
    public static string TagsOutputJsonSchema { get; } = """
        {
            "title": "Tags Extraction",
            "type": "object",
            "properties": {
                "Tags": {
                    "type": "array",
                    "items": {
                        "type": "string"
                    },
                    "description": "A list of tags extracted from the document chunks.",
                    "example": ["tag1", "tag2", "tag3"]
                }
            },
            "required": ["Tags"]
        }
    """;
    #endregion


    #region Prompts for Chat Title Generation
    private const string GenerateChatTitlePromptTemplate =
@"Generate a concise and descriptive title for a chat based on the provided question. The title should capture the essence of the question and be suitable for use as a chat title.
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


public class TagsExtraction
{
    public required List<string> Tags { get; set; }
}

public class TitleGeneration
{
    public required string Title { get; set; }
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)



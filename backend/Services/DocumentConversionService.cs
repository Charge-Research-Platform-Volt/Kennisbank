using System.Net.Http.Headers;
using KnowledgeBank.Utils;

namespace KnowledgeBank.Services;

/// <summary>Converst Office documents to PDF through the Gotenburg sidecar (LibreOffice behind an HTTP API).</summary>
public class DocumentConversionService
{
    private readonly HttpClient httpClient;

    public DocumentConversionService(EnvironmentConfig environmentConfig)
    {
        Uri endpoint = new(environmentConfig.GetVariableValue(EnvironmentVariable.GOTENBERG_URL).TrimEnd('/') + "/");
        // Slightly longer than Gotenberg's own --api-timeout of 120s, so its error comes through instead of ours
        httpClient = new HttpClient { BaseAddress = endpoint, Timeout = TimeSpan.FromSeconds(150) };
    }

    /// <summary>
    /// Returns the PDF as a seekable stream. Throws when Gotenberg is unreachable or can't convert the file.
    /// LibreOffice picks the input format from the extension, so <paramref name="fileName"/> must end in .docx.
    /// </summary>
    public async Task<MemoryStream> ConvertToPdfAsync(Stream document, string fileName, CancellationToken cancellationToken = default)
    {
        using MultipartFormDataContent form = new();
        StreamContent file = new(document);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(file, "files", fileName);

        using HttpResponseMessage response = await httpClient.PostAsync("forms/libreoffice/convert", form, cancellationToken);
        response.EnsureSuccessStatusCode();

        MemoryStream pdf = new();
        await response.Content.CopyToAsync(pdf, cancellationToken);
        pdf.Position = 0;
        return pdf;
    }
}
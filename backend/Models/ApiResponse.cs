namespace KnowledgeBank.Models;

/// <summary>
/// This formats the response from the API the same, so that you can always parse it.
/// </summary>
public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public object? Body { get; set; }

    public ApiResponse(bool success, string message)
    {
        this.Success = success;
        this.Message = message;
    }

    public ApiResponse(bool success, string message, object body)
    {
        this.Success = success;
        this.Message = message;
        this.Body = body;
    }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
//
// Author: Abel Dieterich

namespace KnowledgeBank.Responses;

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
    
    public ApiResponse(bool success, string message, object data) 
    {
        this.Success = success;
        this.Message = message;
        this.Body = data;
    }
}
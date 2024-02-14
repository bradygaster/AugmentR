namespace Backend.Models;

public record class Message(
    string Content,
    string FunctionCall,
    string Role,
    string ToolCalls);

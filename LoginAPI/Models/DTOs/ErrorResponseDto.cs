namespace LoginAPI.Models.DTOs;

public class ErrorResponseDto
{
    public string Message { get; set; } = string.Empty;
    public List<string>? Errors { get; set; }
}

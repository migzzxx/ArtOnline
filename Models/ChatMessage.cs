namespace ArtOnline.Models;

public class ChatMessage
{
    public int Id { get; set; }
    public int CommissionId { get; set; }
    public int SenderId { get; set; }
    public string SenderUsername { get; set; } = "";
    public bool IsArtist { get; set; }
    public string Message { get; set; } = "";
    public string? ImagePath { get; set; }
    public DateTime SentAt { get; set; }
}

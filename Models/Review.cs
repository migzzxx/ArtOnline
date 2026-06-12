namespace ArtOnline.Models;

public class Review
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public int CommissionId { get; set; }
    public string CommissionSubject { get; set; } = "";
    public string Message { get; set; } = "";
    public int Rating { get; set; }
    public DateTime SentAt { get; set; }
}

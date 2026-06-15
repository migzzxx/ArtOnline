namespace ArtOnline.Models;

public class Commission
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Email { get; set; } = "";
    public string ContactNumber { get; set; } = "";
    public string SocialAccountLink { get; set; } = "";
    public bool RushCommission { get; set; }
    public string Subject { get; set; } = "";
    public string? CharacterSheetPath { get; set; }
    public string? CharacterReference { get; set; }
    public string CommissionType1 { get; set; } = "";
    public string CommissionType2 { get; set; } = "";
    public string CommissionType3 { get; set; } = "";
    public string ReferencePosePath { get; set; } = "";
    public string? ReferenceBackgroundPath { get; set; }
    public string CanvasSize { get; set; } = "";
    public string? CustomCanvasSize { get; set; }
    public string? OtherNotes { get; set; }
    public string ModeOfPayment { get; set; } = "";
    public string EstimatedBudget { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public DateTime DateSubmitted { get; set; }
    public string TimeSubmitted { get; set; } = "";
    public int? FormTemplateVersion { get; set; }

    public User? User { get; set; }
}

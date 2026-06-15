using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ArtOnline.Data;
using ArtOnline.Models;

namespace ArtOnline.Pages;

[Authorize]
public class CommissionModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public CommissionModel(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public List<Commission> Commissions { get; set; } = new();
    public List<ChatMessage> ChatMessages { get; set; } = new();
    public int SelectedCommissionId { get; set; }
    public Commission? ViewingCommission { get; set; }
    public bool IsEditing { get; set; }
    public bool ShowSuccess { get; set; }
    public bool IsCommissionOpen { get; set; } = true;

    [BindProperty] public string Email { get; set; } = "";
    [BindProperty] public string ContactNumber { get; set; } = "";
    [BindProperty] public string SocialAccountLink { get; set; } = "";
    [BindProperty] public string RushCommission { get; set; } = "No";
    [BindProperty] public string Subject { get; set; } = "";
    [BindProperty] public IFormFile? CharacterSheet { get; set; }
    [BindProperty] public string? CharacterReference { get; set; }
    [BindProperty] public string CommissionType1 { get; set; } = "";
    [BindProperty] public string CommissionType2 { get; set; } = "";
    [BindProperty] public string CommissionType3 { get; set; } = "";
    [BindProperty] public IFormFile? ReferencePose { get; set; }
    [BindProperty] public IFormFile? ReferenceBackground { get; set; }
    [BindProperty] public string CanvasSize { get; set; } = "";
    [BindProperty] public string? CustomCanvasSize { get; set; }
    [BindProperty] public string? OtherNotes { get; set; }
    [BindProperty] public string ModeOfPayment { get; set; } = "";
    [BindProperty] public string EstimatedBudget { get; set; } = "";

    public string ErrorMessage { get; set; } = "";

    public void OnGet(int? commissionId, bool edit = false)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        Commissions = _db.Commissions
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.DateSubmitted)
            .ToList();

        if (TempData["Success"] != null)
            ShowSuccess = true;

        // Check commission status (open/closed)
        var commStatus = _db.CommissionStatuses.FirstOrDefault();
        IsCommissionOpen = commStatus == null || commStatus.IsOpen;

        // Load selected commission for viewing/editing
        if (commissionId.HasValue && Commissions.Any(c => c.Id == commissionId.Value))
        {
            SelectedCommissionId = commissionId.Value;
            ViewingCommission = Commissions.First(c => c.Id == commissionId.Value);
            IsEditing = edit && ViewingCommission.Status == "Pending";
        }

        if (SelectedCommissionId > 0)
        {
            ChatMessages = _db.ChatMessages
                .Where(m => m.CommissionId == SelectedCommissionId)
                .OrderBy(m => m.SentAt)
                .ToList();
        }
    }

    public async Task<IActionResult> OnPostSendMessageAsync(int SelectedCommissionId, string ChatMessageText)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var username = User.FindFirst(ClaimTypes.Name)!.Value;

        if (!string.IsNullOrWhiteSpace(ChatMessageText) && SelectedCommissionId > 0)
        {
            var msg = new ChatMessage
            {
                CommissionId = SelectedCommissionId,
                SenderId = userId,
                SenderUsername = username,
                IsArtist = false,
                Message = ChatMessageText.Trim(),
                SentAt = DateTime.Now
            };

            _db.ChatMessages.Add(msg);
            await _db.SaveChangesAsync();
        }

        return RedirectToPage("/Commission", new { commissionId = SelectedCommissionId });
    }

    public async Task<IActionResult> OnPostUpdateCommissionAsync(int CommissionId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var commission = _db.Commissions.FirstOrDefault(c => c.Id == CommissionId && c.UserId == userId);

        if (commission == null || commission.Status != "Pending")
        {
            return RedirectToPage("/Commission", new { commissionId = CommissionId });
        }

        // Update text fields
        commission.Email = Email;
        commission.ContactNumber = ContactNumber;
        commission.SocialAccountLink = SocialAccountLink;
        commission.RushCommission = RushCommission == "Yes";
        commission.Subject = Subject;
        commission.CharacterReference = CharacterReference;
        commission.CommissionType1 = CommissionType1;
        commission.CommissionType2 = CommissionType2;
        commission.CommissionType3 = CommissionType3;
        commission.CanvasSize = CanvasSize == "Other" ? CustomCanvasSize ?? "" : CanvasSize;
        commission.OtherNotes = OtherNotes;
        commission.ModeOfPayment = ModeOfPayment;
        commission.EstimatedBudget = EstimatedBudget;

        // Handle file uploads — only update if new file provided
        var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "commissions", userId.ToString());
        Directory.CreateDirectory(uploadDir);

        if (CharacterSheet != null)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(CharacterSheet.FileName)}";
            var filePath = Path.Combine(uploadDir, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await CharacterSheet.CopyToAsync(stream);
            commission.CharacterSheetPath = $"/uploads/commissions/{userId}/{fileName}";
        }

        if (ReferencePose != null)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(ReferencePose.FileName)}";
            var filePath = Path.Combine(uploadDir, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await ReferencePose.CopyToAsync(stream);
            commission.ReferencePosePath = $"/uploads/commissions/{userId}/{fileName}";
        }

        if (ReferenceBackground != null)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(ReferenceBackground.FileName)}";
            var filePath = Path.Combine(uploadDir, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await ReferenceBackground.CopyToAsync(stream);
            commission.ReferenceBackgroundPath = $"/uploads/commissions/{userId}/{fileName}";
        }

        await _db.SaveChangesAsync();
        return RedirectToPage("/Commission", new { commissionId = CommissionId });
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // Validate required fields
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(ContactNumber) || string.IsNullOrWhiteSpace(SocialAccountLink))
        {
            ErrorMessage = "All contact information fields are required.";
            Commissions = _db.Commissions.Where(c => c.UserId == userId).OrderByDescending(c => c.DateSubmitted).ToList();
            return Page();
        }

        if (!ContactNumber.All(char.IsDigit))
        {
            ErrorMessage = "Contact number must contain only numbers.";
            Commissions = _db.Commissions.Where(c => c.UserId == userId).OrderByDescending(c => c.DateSubmitted).ToList();
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Subject))
        {
            ErrorMessage = "Subject is required.";
            Commissions = _db.Commissions.Where(c => c.UserId == userId).OrderByDescending(c => c.DateSubmitted).ToList();
            return Page();
        }

        if (CharacterSheet == null && string.IsNullOrWhiteSpace(CharacterReference))
        {
            ErrorMessage = "Please provide either a Character Sheet upload or a Character Reference description.";
            Commissions = _db.Commissions.Where(c => c.UserId == userId).OrderByDescending(c => c.DateSubmitted).ToList();
            return Page();
        }

        if (string.IsNullOrWhiteSpace(CommissionType1) || string.IsNullOrWhiteSpace(CommissionType2) || string.IsNullOrWhiteSpace(CommissionType3))
        {
            ErrorMessage = "All three commission type fields are required.";
            Commissions = _db.Commissions.Where(c => c.UserId == userId).OrderByDescending(c => c.DateSubmitted).ToList();
            return Page();
        }

        if (ReferencePose == null)
        {
            ErrorMessage = "Reference Pose is required.";
            Commissions = _db.Commissions.Where(c => c.UserId == userId).OrderByDescending(c => c.DateSubmitted).ToList();
            return Page();
        }

        if (string.IsNullOrWhiteSpace(CanvasSize))
        {
            ErrorMessage = "Canvas Size is required.";
            Commissions = _db.Commissions.Where(c => c.UserId == userId).OrderByDescending(c => c.DateSubmitted).ToList();
            return Page();
        }

        if (CanvasSize == "Other" && string.IsNullOrWhiteSpace(CustomCanvasSize))
        {
            ErrorMessage = "Please specify your custom canvas size.";
            Commissions = _db.Commissions.Where(c => c.UserId == userId).OrderByDescending(c => c.DateSubmitted).ToList();
            return Page();
        }

        if (string.IsNullOrWhiteSpace(ModeOfPayment))
        {
            ErrorMessage = "Mode of Payment is required.";
            Commissions = _db.Commissions.Where(c => c.UserId == userId).OrderByDescending(c => c.DateSubmitted).ToList();
            return Page();
        }

        if (string.IsNullOrWhiteSpace(EstimatedBudget))
        {
            ErrorMessage = "Estimated Budget is required.";
            Commissions = _db.Commissions.Where(c => c.UserId == userId).OrderByDescending(c => c.DateSubmitted).ToList();
            return Page();
        }

        // Save uploaded files
        var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "commissions", userId.ToString());
        Directory.CreateDirectory(uploadDir);

        string? characterSheetPath = null;
        if (CharacterSheet != null)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(CharacterSheet.FileName)}";
            var filePath = Path.Combine(uploadDir, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await CharacterSheet.CopyToAsync(stream);
            characterSheetPath = $"/uploads/commissions/{userId}/{fileName}";
        }

        var refPoseFileName = $"{Guid.NewGuid()}{Path.GetExtension(ReferencePose.FileName)}";
        var refPosePath = Path.Combine(uploadDir, refPoseFileName);
        using (var stream = new FileStream(refPosePath, FileMode.Create))
        {
            await ReferencePose.CopyToAsync(stream);
        }

        string? refBgPath = null;
        if (ReferenceBackground != null)
        {
            var bgFileName = $"{Guid.NewGuid()}{Path.GetExtension(ReferenceBackground.FileName)}";
            var bgFilePath = Path.Combine(uploadDir, bgFileName);
            using var stream = new FileStream(bgFilePath, FileMode.Create);
            await ReferenceBackground.CopyToAsync(stream);
            refBgPath = $"/uploads/commissions/{userId}/{bgFileName}";
        }

        var now = DateTime.Now;
        var commission = new Commission
        {
            UserId = userId,
            Email = Email,
            ContactNumber = ContactNumber,
            SocialAccountLink = SocialAccountLink,
            RushCommission = RushCommission == "Yes",
            Subject = Subject,
            CharacterSheetPath = characterSheetPath,
            CharacterReference = CharacterReference,
            CommissionType1 = CommissionType1,
            CommissionType2 = CommissionType2,
            CommissionType3 = CommissionType3,
            ReferencePosePath = $"/uploads/commissions/{userId}/{refPoseFileName}",
            ReferenceBackgroundPath = refBgPath,
            CanvasSize = CanvasSize == "Other" ? CustomCanvasSize ?? "" : CanvasSize,
            OtherNotes = OtherNotes,
            ModeOfPayment = ModeOfPayment,
            EstimatedBudget = EstimatedBudget,
            Status = "Pending",
            DateSubmitted = now,
            TimeSubmitted = now.ToString("h:mm tt"),
            FormTemplateVersion = _db.FormTemplates.Where(t => t.IsCurrent).Select(t => t.Version).FirstOrDefault()
        };

        _db.Commissions.Add(commission);
        await _db.SaveChangesAsync();

        TempData["Success"] = "true";
        return RedirectToPage("/Commission");
    }
}

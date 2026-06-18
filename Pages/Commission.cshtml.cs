using System.Security.Claims;
using System.Text.Json;
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
    public string CurrentTemplateJson { get; set; } = "[]";
    public int CurrentTemplateVersion { get; set; }
    public string? ViewingTemplateJson { get; set; }

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

        // Load current form template
        var template = _db.FormTemplates.FirstOrDefault(t => t.IsCurrent);
        if (template != null)
        {
            CurrentTemplateJson = template.FieldsJson;
            CurrentTemplateVersion = template.Version;
        }

        // Load selected commission for viewing/editing
        if (commissionId.HasValue && Commissions.Any(c => c.Id == commissionId.Value))
        {
            SelectedCommissionId = commissionId.Value;
            ViewingCommission = Commissions.First(c => c.Id == commissionId.Value);
            IsEditing = edit && ViewingCommission.Status == "Pending";

            // Load the template used for this commission (for dynamic rendering)
            if (ViewingCommission.CustomFieldsJson != null)
            {
                var tmpl = _db.FormTemplates.FirstOrDefault(t => t.Version == ViewingCommission.FormTemplateVersion);
                if (tmpl != null) ViewingTemplateJson = tmpl.FieldsJson;
                else
                {
                    var current = _db.FormTemplates.FirstOrDefault(t => t.IsCurrent);
                    if (current != null) ViewingTemplateJson = current.FieldsJson;
                }
            }
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

        // Check if this is a dynamic commission update
        var editCustomJson = Request.Form["CustomFieldsJson"].ToString();
        if (!string.IsNullOrWhiteSpace(editCustomJson))
        {
            // Dynamic commission: update CustomFieldsJson and handle files
            var editValues = JsonSerializer.Deserialize<Dictionary<string, string>>(editCustomJson) ?? new();

            var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "commissions", userId.ToString());
            Directory.CreateDirectory(uploadDir);

            foreach (var file in Request.Form.Files)
            {
                if (file.Length > 0)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                    var filePath = Path.Combine(uploadDir, fileName);
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(stream);
                    var savedPath = $"/uploads/commissions/{userId}/{fileName}";
                    editValues[file.Name] = savedPath;
                }
            }

            commission.CustomFieldsJson = JsonSerializer.Serialize(editValues);

            // Also update hardcoded columns for queue display
            var template = _db.FormTemplates.FirstOrDefault(t => t.Version == commission.FormTemplateVersion)
                ?? _db.FormTemplates.FirstOrDefault(t => t.IsCurrent);
            if (template != null)
            {
                var templateFieldsList = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(template.FieldsJson) ?? new();
                string GetVal(string labelContains) {
                    foreach (var tf in templateFieldsList) {
                        if (tf.ContainsKey("label") && tf["label"].GetString()?.ToLower().Contains(labelContains.ToLower()) == true) {
                            var fId = tf.ContainsKey("id") ? tf["id"].GetString() ?? "" : "";
                            var key = "field_" + fId;
                            if (editValues.ContainsKey(key)) return editValues[key];
                        }
                    }
                    return "";
                }
                commission.Subject = GetVal("subject") != "" ? GetVal("subject") : commission.Subject;
                commission.RushCommission = GetVal("rush").ToLower() == "yes";
                commission.CommissionType1 = GetVal("body") != "" ? GetVal("body") : commission.CommissionType1;
                commission.CommissionType2 = GetVal("number of characters") != "" ? GetVal("number of characters") : (GetVal("person") != "" ? GetVal("person") : commission.CommissionType2);
                commission.CommissionType3 = GetVal("style") != "" ? GetVal("style") : commission.CommissionType3;
            }

            await _db.SaveChangesAsync();
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
        var uploadDir2 = Path.Combine(_env.WebRootPath, "uploads", "commissions", userId.ToString());
        Directory.CreateDirectory(uploadDir2);

        if (CharacterSheet != null)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(CharacterSheet.FileName)}";
            var filePath = Path.Combine(uploadDir2, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await CharacterSheet.CopyToAsync(stream);
            commission.CharacterSheetPath = $"/uploads/commissions/{userId}/{fileName}";
        }

        if (ReferencePose != null)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(ReferencePose.FileName)}";
            var filePath = Path.Combine(uploadDir2, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await ReferencePose.CopyToAsync(stream);
            commission.ReferencePosePath = $"/uploads/commissions/{userId}/{fileName}";
        }

        if (ReferenceBackground != null)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(ReferenceBackground.FileName)}";
            var filePath = Path.Combine(uploadDir2, fileName);
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
        var template = _db.FormTemplates.FirstOrDefault(t => t.IsCurrent);

        // Get the custom fields JSON from the form
        var customJson = Request.Form["CustomFieldsJson"].ToString();
        var fieldValues = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(customJson))
        {
            fieldValues = JsonSerializer.Deserialize<Dictionary<string, string>>(customJson) ?? new();
        }

        // Load template fields to map by label
        var templateFieldsList = new List<Dictionary<string, JsonElement>>();
        if (template != null)
        {
            templateFieldsList = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(template.FieldsJson) ?? new();
        }

        string GetValueByLabel(string labelContains)
        {
            foreach (var tf in templateFieldsList)
            {
                if (tf.ContainsKey("label") && tf["label"].GetString()?.ToLower().Contains(labelContains.ToLower()) == true)
                {
                    var fieldId = tf.ContainsKey("id") ? tf["id"].GetString() ?? "" : "";
                    var key = "field_" + fieldId;
                    if (fieldValues.ContainsKey(key)) return fieldValues[key];
                }
            }
            return "";
        }

        // Extract known fields for backward compatibility
        var email = GetValueByLabel("email address") != "" ? GetValueByLabel("email address") : GetValueByLabel("email");
        var contactNumber = GetValueByLabel("contact");
        var socialLink = GetValueByLabel("social");
        var subject = GetValueByLabel("subject");
        var rushCommission = GetValueByLabel("rush");
        var commType1 = GetValueByLabel("body");
        var commType2 = GetValueByLabel("number of characters") != "" ? GetValueByLabel("number of characters") : GetValueByLabel("person");
        var commType3 = GetValueByLabel("style");
        var canvasSize = GetValueByLabel("canvas");
        var modeOfPayment = GetValueByLabel("payment");
        var estimatedBudget = GetValueByLabel("budget");
        var characterRef = GetValueByLabel("character ref");
        var otherNotes = GetValueByLabel("note");

        // Basic validation
        if (string.IsNullOrWhiteSpace(subject))
        {
            ErrorMessage = "Subject is required.";
            Commissions = _db.Commissions.Where(c => c.UserId == userId).OrderByDescending(c => c.DateSubmitted).ToList();
            CurrentTemplateJson = template?.FieldsJson ?? "[]";
            CurrentTemplateVersion = template?.Version ?? 1;
            var cs = _db.CommissionStatuses.FirstOrDefault();
            IsCommissionOpen = cs == null || cs.IsOpen;
            return Page();
        }

        // Handle file uploads
        var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "commissions", userId.ToString());
        Directory.CreateDirectory(uploadDir);

        string? characterSheetPath = null;
        string refPosePath = "";
        string? refBgPath = null;

        foreach (var file in Request.Form.Files)
        {
            if (file.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadDir, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);
                var savedPath = $"/uploads/commissions/{userId}/{fileName}";

                // Store file path in CustomFieldsJson for dynamic viewing
                fieldValues[file.Name] = savedPath;

                var fieldName = file.Name.ToLower();
                if (fieldName.Contains("sheet") || fieldName.Contains("character"))
                    characterSheetPath = savedPath;
                else if (fieldName.Contains("pose") || fieldName.Contains("reference pose"))
                    refPosePath = savedPath;
                else if (fieldName.Contains("background"))
                    refBgPath = savedPath;
                else if (string.IsNullOrEmpty(characterSheetPath))
                    characterSheetPath = savedPath;
                else if (string.IsNullOrEmpty(refPosePath))
                    refPosePath = savedPath;
                else
                    refBgPath = savedPath;
            }
        }

        // Update customJson with file paths included
        customJson = JsonSerializer.Serialize(fieldValues);

        var now = DateTime.Now;
        var commission = new Commission
        {
            UserId = userId,
            Email = email,
            ContactNumber = contactNumber,
            SocialAccountLink = socialLink,
            RushCommission = rushCommission.ToLower() == "yes",
            Subject = subject,
            CharacterSheetPath = characterSheetPath,
            CharacterReference = string.IsNullOrWhiteSpace(characterRef) ? null : characterRef,
            CommissionType1 = commType1,
            CommissionType2 = commType2,
            CommissionType3 = commType3,
            ReferencePosePath = refPosePath,
            ReferenceBackgroundPath = refBgPath,
            CanvasSize = canvasSize,
            OtherNotes = string.IsNullOrWhiteSpace(otherNotes) ? null : otherNotes,
            ModeOfPayment = modeOfPayment,
            EstimatedBudget = estimatedBudget,
            Status = "Pending",
            DateSubmitted = now,
            TimeSubmitted = now.ToString("h:mm tt"),
            FormTemplateVersion = template?.Version ?? 1,
            CustomFieldsJson = customJson
        };

        _db.Commissions.Add(commission);
        await _db.SaveChangesAsync();

        TempData["Success"] = "true";
        return RedirectToPage("/Commission");
    }


    public async Task<IActionResult> OnPostSendImageAsync()
    {
        var commissionId = int.Parse(Request.Form["CommissionId"]);
        var file = Request.Form.Files.FirstOrDefault();
        if (file == null || file.Length == 0) return new JsonResult(new { success = false });

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var user = _db.Users.Find(userId);

        var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "chat");
        Directory.CreateDirectory(uploadDir);
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadDir, fileName);
        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);
        var savedPath = $"/uploads/chat/{fileName}";

        var msg = new ChatMessage
        {
            CommissionId = commissionId,
            SenderId = userId,
            SenderUsername = user?.Username ?? "User",
            IsArtist = false,
            Message = file.FileName,
            ImagePath = savedPath,
            SentAt = DateTime.Now
        };
        _db.ChatMessages.Add(msg);
        await _db.SaveChangesAsync();

        return new JsonResult(new { success = true, id = msg.Id, message = msg.Message, imagePath = savedPath, isArtist = false, time = msg.SentAt.ToString("h:mm tt") });
    }

    public IActionResult OnGetMessages(int commissionId, int lastId)
    {
        var messages = _db.ChatMessages
            .Where(m => m.CommissionId == commissionId && m.Id > lastId)
            .OrderBy(m => m.SentAt)
            .Select(m => new {
                m.Id,
                m.Message,
                m.IsArtist,
                m.ImagePath,
                m.SenderUsername,
                Time = (DateTime.Now - m.SentAt).TotalHours < 24 ? m.SentAt.ToString("h:mm tt") : m.SentAt.ToString("dd/MM/yyyy")
            })
            .ToList();
        return new JsonResult(messages);
    }

    public IActionResult OnGetTemplate()
    {
        var template = _db.FormTemplates.FirstOrDefault(t => t.IsCurrent);
        if (template != null)
        {
            return new JsonResult(new { fields = template.FieldsJson, version = template.Version });
        }
        return new JsonResult(new { fields = "[]", version = 0 });
    }
}

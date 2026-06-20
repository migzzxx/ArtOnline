using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ArtOnline.Data;
using ArtOnline.Models;

namespace ArtOnline.Pages;

[Authorize]
public class ArtistCommissionModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ArtistCommissionModel(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public List<Commission> AllCommissions { get; set; } = new();
    public Commission? ViewingCommission { get; set; }
    public List<ChatMessage> ChatMessages { get; set; } = new();
    public int SelectedCommissionId { get; set; }
    public string CommissionUsername { get; set; } = "";

    public void OnGet(int? commissionId)
    {
        AllCommissions = _db.Commissions
            .Include(c => c.User)
            .OrderByDescending(c => c.DateSubmitted)
            .ToList();

        if (commissionId.HasValue && AllCommissions.Any(c => c.Id == commissionId.Value))
        {
            SelectedCommissionId = commissionId.Value;
            ViewingCommission = AllCommissions.First(c => c.Id == commissionId.Value);
            CommissionUsername = ViewingCommission.User?.Username ?? "Unknown";

            ChatMessages = _db.ChatMessages
                .Where(m => m.CommissionId == SelectedCommissionId)
                .OrderBy(m => m.SentAt)
                .ToList();
        }
    }

    public async Task<IActionResult> OnPostSendMessageAsync(int SelectedCommissionId, string ChatMessageText)
    {
        if (!string.IsNullOrWhiteSpace(ChatMessageText) && SelectedCommissionId > 0)
        {
            var msg = new ChatMessage
            {
                CommissionId = SelectedCommissionId,
                SenderId = 0,
                SenderUsername = "Amoreivc",
                IsArtist = true,
                Message = ChatMessageText,
                SentAt = TimeHelper.Now
            };

            _db.ChatMessages.Add(msg);
            await _db.SaveChangesAsync();
        }

        return RedirectToPage("/ArtistCommission", new { commissionId = SelectedCommissionId });
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(int CommissionId, string NewStatus)
    {
        var commission = await _db.Commissions.FindAsync(CommissionId);
        if (commission != null)
        {
            // Only allow Pending→Working or Working→Completed
            if ((commission.Status == "Pending" && NewStatus == "Working") ||
                (commission.Status == "Working" && NewStatus == "Completed"))
            {
                commission.Status = NewStatus;
                await _db.SaveChangesAsync();
            }
        }
        return RedirectToPage("/ArtistCommission", new { commissionId = CommissionId });
    }

    public async Task<IActionResult> OnPostUploadFinalOutputAsync(int CommissionId, IFormFile FinalOutput)
    {
        var commission = await _db.Commissions.FindAsync(CommissionId);
        if (commission == null || commission.Status != "Working") 
            return RedirectToPage("/ArtistCommission", new { commissionId = CommissionId });

        if (FinalOutput != null && FinalOutput.Length > 0)
        {
            var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "commissions", "final");
            Directory.CreateDirectory(uploadDir);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(FinalOutput.FileName)}";
            var filePath = Path.Combine(uploadDir, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await FinalOutput.CopyToAsync(stream);
            commission.FinalOutputPath = $"/uploads/commissions/final/{fileName}";
            await _db.SaveChangesAsync();
        }

        return RedirectToPage("/ArtistCommission", new { commissionId = CommissionId });
    }

    public async Task<IActionResult> OnPostRemoveFinalOutputAsync(int CommissionId)
    {
        var commission = await _db.Commissions.FindAsync(CommissionId);
        if (commission != null && commission.Status == "Working")
        {
            commission.FinalOutputPath = null;
            await _db.SaveChangesAsync();
        }
        return RedirectToPage("/ArtistCommission", new { commissionId = CommissionId });
    }


    public async Task<IActionResult> OnPostSendImageAsync()
    {
        var commissionId = int.Parse(Request.Form["CommissionId"]);
        var file = Request.Form.Files.FirstOrDefault();
        if (file == null || file.Length == 0) return new JsonResult(new { success = false });

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
            SenderId = 0,
            SenderUsername = "Amoreivc",
            IsArtist = true,
            Message = file.FileName,
            ImagePath = savedPath,
            SentAt = TimeHelper.Now
        };
        _db.ChatMessages.Add(msg);
        await _db.SaveChangesAsync();

        return new JsonResult(new { success = true, id = msg.Id, message = msg.Message, imagePath = savedPath, isArtist = true, time = msg.SentAt.ToString("h:mm tt") });
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
                m.SenderUsername,
                m.ImagePath,
                Time = (TimeHelper.Now - m.SentAt).TotalHours < 24 ? m.SentAt.ToString("h:mm tt") : m.SentAt.ToString("dd/MM/yyyy")
            })
            .ToList();
        return new JsonResult(messages);
    }
}

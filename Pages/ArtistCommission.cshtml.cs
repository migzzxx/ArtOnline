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

    public ArtistCommissionModel(AppDbContext db)
    {
        _db = db;
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
                SentAt = DateTime.Now
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
}

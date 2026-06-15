using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ArtOnline.Data;
using ArtOnline.Models;

namespace ArtOnline.Pages;

[Authorize]
public class ArtistDashboardModel : PageModel
{
    private readonly AppDbContext _db;

    public ArtistDashboardModel(AppDbContext db)
    {
        _db = db;
    }

    public int RushCount { get; set; }
    public int PendingCount { get; set; }
    public int WorkingCount { get; set; }
    public int CompletedCount { get; set; }
    public List<Review> AllReviews { get; set; } = new();
    public double AverageRating { get; set; }
    public List<Commission> RecentActivities { get; set; } = new();
    public bool IsCommissionOpen { get; set; } = true;

    public void OnGet()
    {
        // Count commissions
        var allCommissions = _db.Commissions.ToList();
        RushCount = allCommissions.Count(c => c.RushCommission && c.Status != "Completed");
        PendingCount = allCommissions.Count(c => c.Status == "Pending");
        WorkingCount = allCommissions.Count(c => c.Status == "Working");
        CompletedCount = allCommissions.Count(c => c.Status == "Completed");

        // Load reviews
        AllReviews = _db.Reviews.OrderBy(r => r.SentAt).ToList();
        AverageRating = AllReviews.Any() ? AllReviews.Average(r => r.Rating) : 0.0;

        // Recent activities (most recent 10)
        RecentActivities = allCommissions
            .OrderByDescending(c => c.DateSubmitted)
            .Take(10)
            .ToList();

        // Commission status
        var status = _db.CommissionStatuses.FirstOrDefault();
        if (status == null)
        {
            status = new CommissionStatus { IsOpen = true };
            _db.CommissionStatuses.Add(status);
            _db.SaveChanges();
        }
        IsCommissionOpen = status.IsOpen;
    }

    public IActionResult OnPostToggleStatus()
    {
        var status = _db.CommissionStatuses.FirstOrDefault();
        if (status == null)
        {
            status = new CommissionStatus { IsOpen = true };
            _db.CommissionStatuses.Add(status);
        }
        else
        {
            status.IsOpen = !status.IsOpen;
        }
        _db.SaveChanges();
        return RedirectToPage("/ArtistDashboard");
    }
}

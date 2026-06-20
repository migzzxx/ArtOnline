using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ArtOnline.Data;
using ArtOnline.Models;

namespace ArtOnline.Pages;

[Authorize]
public class ReviewsModel : PageModel
{
    private readonly AppDbContext _db;

    public ReviewsModel(AppDbContext db)
    {
        _db = db;
    }

    public List<Review> AllReviews { get; set; } = new();
    public List<Commission> CompletedCommissions { get; set; } = new();
    public double AverageRating { get; set; }
    public bool CanReview { get; set; }

    public void OnGet()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // Load all reviews from all users
        AllReviews = _db.Reviews.OrderBy(r => r.SentAt).ToList();

        // Calculate average rating
        if (AllReviews.Any())
            AverageRating = Math.Round(AllReviews.Average(r => r.Rating), 1);

        // Get current user's completed commissions that haven't been used for a review yet
        var usedCommissionIds = _db.Reviews.Where(r => r.UserId == userId).Select(r => r.CommissionId).ToList();
        CompletedCommissions = _db.Commissions
            .Where(c => c.UserId == userId && c.Status == "Completed" && !usedCommissionIds.Contains(c.Id))
            .ToList();

        CanReview = CompletedCommissions.Any();
    }

    public IActionResult OnPostSendReview(string ReviewMessage, int CommissionId, int Rating)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var username = User.FindFirst(ClaimTypes.Name)!.Value;

        // Validate
        if (string.IsNullOrWhiteSpace(ReviewMessage) || CommissionId == 0)
        {
            return RedirectToPage("/Reviews");
        }

        // Verify commission belongs to user and is completed
        var commission = _db.Commissions.FirstOrDefault(c => c.Id == CommissionId && c.UserId == userId && c.Status == "Completed");
        if (commission == null)
        {
            return RedirectToPage("/Reviews");
        }

        // Check if already reviewed
        if (_db.Reviews.Any(r => r.UserId == userId && r.CommissionId == CommissionId))
        {
            return RedirectToPage("/Reviews");
        }

        // Clamp rating
        if (Rating < 0) Rating = 0;
        if (Rating > 5) Rating = 5;

        var review = new Review
        {
            UserId = userId,
            Username = username,
            CommissionId = CommissionId,
            CommissionSubject = commission.Subject,
            Message = ReviewMessage,
            Rating = Rating,
            SentAt = TimeHelper.Now
        };

        _db.Reviews.Add(review);
        _db.SaveChanges();

        return RedirectToPage("/Reviews");
    }
}

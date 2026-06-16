using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ArtOnline.Data;
using ArtOnline.Models;

namespace ArtOnline.Pages;

[Authorize]
public class GalleryModel : PageModel
{
    private readonly AppDbContext _db;

    public GalleryModel(AppDbContext db)
    {
        _db = db;
    }

    public List<GalleryImage> Images { get; set; } = new();
    public List<GalleryTag> AllTags { get; set; } = new();
    public List<int> HeartedImageIds { get; set; } = new();
    public string? SearchQuery { get; set; }
    public string? SelectedTag { get; set; }
    public bool ShowHearted { get; set; }

    public void OnGet(string? search, string? tag, bool hearted = false)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        AllTags = _db.GalleryTags.OrderBy(t => t.Name).ToList();
        SearchQuery = search;
        SelectedTag = tag;
        ShowHearted = hearted;

        // Load user's hearted image IDs
        HeartedImageIds = _db.GalleryHearts
            .Where(h => h.UserId == userId)
            .Select(h => h.GalleryImageId)
            .ToList();

        var query = _db.GalleryImages.AsQueryable();

        if (hearted)
        {
            query = query.Where(i => HeartedImageIds.Contains(i.Id));
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            query = query.Where(i => i.Tags.ToLower().Contains(tag.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(i => i.Title.ToLower().Contains(search.ToLower()));
        }

        Images = query.OrderByDescending(i => i.DateAdded).ToList();
    }

    public async Task<IActionResult> OnPostHeartAsync(int imageId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var existing = _db.GalleryHearts.FirstOrDefault(h => h.GalleryImageId == imageId && h.UserId == userId);
        var image = await _db.GalleryImages.FindAsync(imageId);

        if (image == null) return new JsonResult(new { success = false });

        bool hearted;
        if (existing != null)
        {
            _db.GalleryHearts.Remove(existing);
            image.HeartCount = Math.Max(0, image.HeartCount - 1);
            hearted = false;
        }
        else
        {
            _db.GalleryHearts.Add(new GalleryHeart { UserId = userId, GalleryImageId = imageId });
            image.HeartCount++;
            hearted = true;
        }

        await _db.SaveChangesAsync();
        return new JsonResult(new { success = true, hearted = hearted, newCount = image.HeartCount });
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ArtOnline.Data;
using ArtOnline.Models;

namespace ArtOnline.Pages;

[Authorize]
public class ArtistGalleryModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ArtistGalleryModel(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public List<GalleryImage> Images { get; set; } = new();
    public List<GalleryTag> AllTags { get; set; } = new();
    public string? SearchQuery { get; set; }
    public string? SelectedTag { get; set; }

    public void OnGet(string? search, string? tag)
    {
        AllTags = _db.GalleryTags.OrderBy(t => t.Name).ToList();
        SearchQuery = search;
        SelectedTag = tag;

        var query = _db.GalleryImages.AsQueryable();

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

    public async Task<IActionResult> OnPostUploadAsync(string title, string description, string tags, IFormFile imageFile)
    {
        if (imageFile == null || string.IsNullOrWhiteSpace(title))
        {
            return RedirectToPage("/ArtistGallery");
        }

        var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "gallery");
        Directory.CreateDirectory(uploadDir);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
        var filePath = Path.Combine(uploadDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await imageFile.CopyToAsync(stream);
        }

        var image = new GalleryImage
        {
            ImagePath = $"/uploads/gallery/{fileName}",
            Title = title,
            Description = description ?? "",
            Tags = tags ?? "",
            HeartCount = 0,
            DateAdded = TimeHelper.Now
        };

        _db.GalleryImages.Add(image);
        await _db.SaveChangesAsync();

        return RedirectToPage("/ArtistGallery");
    }

    public async Task<IActionResult> OnPostDeleteAsync(int imageId)
    {
        var image = await _db.GalleryImages.FindAsync(imageId);
        if (image != null)
        {
            // Delete file
            var fullPath = Path.Combine(_env.WebRootPath, image.ImagePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }

            // Delete hearts
            var hearts = _db.GalleryHearts.Where(h => h.GalleryImageId == imageId);
            _db.GalleryHearts.RemoveRange(hearts);

            _db.GalleryImages.Remove(image);
            await _db.SaveChangesAsync();
        }

        return RedirectToPage("/ArtistGallery");
    }

    public async Task<IActionResult> OnPostEditAsync(int imageId, string title, string description, string tags)
    {
        var image = await _db.GalleryImages.FindAsync(imageId);
        if (image != null && !string.IsNullOrWhiteSpace(title))
        {
            image.Title = title;
            image.Description = description ?? "";
            image.Tags = tags ?? "";
            await _db.SaveChangesAsync();
        }

        return RedirectToPage("/ArtistGallery");
    }

    public async Task<IActionResult> OnPostAddTagAsync(string tagName)
    {
        if (!string.IsNullOrWhiteSpace(tagName) && !_db.GalleryTags.Any(t => t.Name == tagName))
        {
            _db.GalleryTags.Add(new GalleryTag { Name = tagName });
            await _db.SaveChangesAsync();
        }

        return new JsonResult(new { success = true });
    }

    // For future user-side heart functionality
    public async Task<IActionResult> OnPostHeartAsync(int imageId, int userId)
    {
        var existing = _db.GalleryHearts.FirstOrDefault(h => h.GalleryImageId == imageId && h.UserId == userId);
        var image = await _db.GalleryImages.FindAsync(imageId);

        if (image == null) return RedirectToPage("/ArtistGallery");

        if (existing != null)
        {
            _db.GalleryHearts.Remove(existing);
            image.HeartCount = Math.Max(0, image.HeartCount - 1);
        }
        else
        {
            _db.GalleryHearts.Add(new GalleryHeart { UserId = userId, GalleryImageId = imageId });
            image.HeartCount++;
        }

        await _db.SaveChangesAsync();
        return RedirectToPage("/ArtistGallery");
    }
}

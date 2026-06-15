using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ArtOnline.Data;
using ArtOnline.Models;

namespace ArtOnline.Pages;

[Authorize]
public class ArtistFormModel : PageModel
{
    private readonly AppDbContext _db;

    public ArtistFormModel(AppDbContext db)
    {
        _db = db;
    }

    public FormTemplate? CurrentTemplate { get; set; }
    public string FieldsJson { get; set; } = "[]";

    public void OnGet()
    {
        CurrentTemplate = _db.FormTemplates.FirstOrDefault(t => t.IsCurrent);
        if (CurrentTemplate != null)
        {
            FieldsJson = CurrentTemplate.FieldsJson;
        }
    }

    public IActionResult OnPostSave(string fieldsData)
    {
        if (string.IsNullOrWhiteSpace(fieldsData))
            return RedirectToPage("/ArtistForm");

        var currentTemplate = _db.FormTemplates.FirstOrDefault(t => t.IsCurrent);
        int newVersion = 1;

        if (currentTemplate != null)
        {
            newVersion = currentTemplate.Version + 1;
            currentTemplate.IsCurrent = false;

            // Check if any commissions reference the old template
            bool isReferenced = _db.Commissions.Any(c => c.FormTemplateVersion == currentTemplate.Version);
            if (!isReferenced)
            {
                _db.FormTemplates.Remove(currentTemplate);
            }
        }

        var newTemplate = new FormTemplate
        {
            Version = newVersion,
            IsCurrent = true,
            CreatedAt = DateTime.Now,
            FieldsJson = fieldsData
        };

        _db.FormTemplates.Add(newTemplate);
        _db.SaveChanges();

        return RedirectToPage("/ArtistForm");
    }
}

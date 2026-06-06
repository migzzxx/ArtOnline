using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ArtOnline.Data;

namespace ArtOnline.Pages;

[Authorize]
public class SettingsModel : PageModel
{
    private readonly AppDbContext _db;

    public SettingsModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty] public string Email { get; set; } = "";
    [BindProperty] public string Username { get; set; } = "";
    [BindProperty] public int? Age { get; set; }
    [BindProperty] public string? Gender { get; set; }

    [BindProperty] public string? CurrentPassword { get; set; }
    [BindProperty] public string? NewPassword { get; set; }
    [BindProperty] public string? ConfirmNewPassword { get; set; }

    public string ErrorMessage { get; set; } = "";
    public string SuccessMessage { get; set; } = "";

    public void OnGet()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = _db.Users.Find(userId)!;

        Email = user.Email;
        Username = user.Username;
        Age = user.Age;
        Gender = user.Gender;
    }

    public IActionResult OnPostProfile()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = _db.Users.Find(userId)!;

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "Email and Username are required";
            return Page();
        }

        // Check if username is taken by someone else
        if (_db.Users.Any(u => u.Username == Username && u.Id != userId))
        {
            ErrorMessage = "Username already taken";
            return Page();
        }

        user.Email = Email;
        user.Username = Username;
        user.Age = Age;
        user.Gender = Gender;

        _db.SaveChanges();
        SuccessMessage = "Profile updated successfully!";
        return Page();
    }

    public IActionResult OnPostPassword()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = _db.Users.Find(userId)!;

        // Load current values for display
        Email = user.Email;
        Username = user.Username;
        Age = user.Age;
        Gender = user.Gender;

        if (string.IsNullOrWhiteSpace(CurrentPassword) || string.IsNullOrWhiteSpace(NewPassword))
        {
            ErrorMessage = "Please fill in all password fields";
            return Page();
        }

        if (!BCrypt.Net.BCrypt.Verify(CurrentPassword, user.PasswordHash))
        {
            ErrorMessage = "Current password is incorrect";
            return Page();
        }

        if (NewPassword != ConfirmNewPassword)
        {
            ErrorMessage = "New passwords do not match";
            return Page();
        }

        if (NewPassword.Length < 6)
        {
            ErrorMessage = "New password must be at least 6 characters";
            return Page();
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
        _db.SaveChanges();
        SuccessMessage = "Password changed successfully!";
        return Page();
    }
}

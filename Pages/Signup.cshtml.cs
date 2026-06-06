using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ArtOnline.Data;
using ArtOnline.Models;

namespace ArtOnline.Pages;

public class SignupModel : PageModel
{
    private readonly AppDbContext _db;

    public SignupModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty] public string Email { get; set; } = "";
    [BindProperty] public string Username { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";
    [BindProperty] public string ConfirmPassword { get; set; } = "";
    public string ErrorMessage { get; set; } = "";

    public void OnGet() { }

    public IActionResult OnPost()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "All fields are required";
            return Page();
        }

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match";
            return Page();
        }

        if (Password.Length < 6)
        {
            ErrorMessage = "Password must be at least 6 characters";
            return Page();
        }

        // Check if username already exists
        if (_db.Users.Any(u => u.Username == Username))
        {
            ErrorMessage = "Username already taken";
            return Page();
        }

        // Check if email already exists
        if (_db.Users.Any(u => u.Email == Email))
        {
            ErrorMessage = "Email already registered";
            return Page();
        }

        var user = new User
        {
            Email = Email,
            Username = Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password)
        };

        _db.Users.Add(user);
        _db.SaveChanges();

        return RedirectToPage("/Login");
    }
}

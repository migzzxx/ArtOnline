using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ArtOnline.Data;

namespace ArtOnline.Pages;

public class LoginModel : PageModel
{
    private readonly AppDbContext _db;

    public LoginModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty] public string? Username { get; set; }
    [BindProperty] public string? Password { get; set; }
    public string ErrorMessage { get; set; } = "";

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        // Hardcoded artist credentials
        const string ArtistUsername = "Amoreivc";
        const string ArtistPassword = "Amoreivc";

        if (Username == ArtistUsername && Password == ArtistPassword)
        {
            var artistClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, ArtistUsername),
                new Claim(ClaimTypes.NameIdentifier, "0")
            };

            var artistIdentity = new ClaimsIdentity(artistClaims, "Cookies");
            await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(artistIdentity));

            return RedirectToPage("/ArtistDashboard");
        }

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Username and password are required";
            return Page();
        }

        var user = _db.Users.FirstOrDefault(u => u.Username == Username);

        // Block login if someone somehow has a DB account with the artist username
        if (user != null && user.Username == ArtistUsername)
        {
            ErrorMessage = "Invalid username or password";
            return Page();
        }

        if (user == null || !BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash))
        {
            ErrorMessage = "Invalid username or password";
            return Page();
        }

        // Create login session
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, "Cookies");
        await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(identity));

        return RedirectToPage("/Home");
    }
}

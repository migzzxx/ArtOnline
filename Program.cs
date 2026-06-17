using Microsoft.EntityFrameworkCore;
using ArtOnline.Data;
using ArtOnline.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 104857600; // 100MB
    options.ValueLengthLimit = 10485760; // 10MB
    options.ValueCountLimit = 2048;
});
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 104857600);

builder.Services.AddRazorPages();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=artonline.db"));

// Add authentication
builder.Services.AddAuthentication("Cookies")
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
    });

var app = builder.Build();

// Auto-create database and seed defaults
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    // Seed default form template if none exists
    if (!db.FormTemplates.Any())
    {
        var defaultFields = @"[
            {""id"":""main"",""type"":""mainheader"",""title"":""Commission Request"",""description"":""Fill out the form below to submit your commission request. All required fields must be completed.""},
            {""id"":""f1"",""type"":""subheader"",""label"":""Contact Information""},
            {""id"":""f2"",""type"":""text"",""label"":""Email Address"",""placeholder"":""Enter your email"",""required"":true},
            {""id"":""f3"",""type"":""text"",""label"":""Contact Number"",""placeholder"":""Enter your contact number"",""required"":true},
            {""id"":""f4"",""type"":""text"",""label"":""Social Account Link"",""placeholder"":""Enter your social account link"",""required"":true},
            {""id"":""f5"",""type"":""subheader"",""label"":""Art Details""},
            {""id"":""f6"",""type"":""dropdown"",""label"":""Rush Commission?"",""required"":true,""options"":[""Yes"",""No""]},
            {""id"":""f7"",""type"":""text"",""label"":""Subject"",""placeholder"":""Provide brief summary of your art request"",""required"":true},
            {""id"":""f8"",""type"":""file"",""label"":""Character Sheet (PNG/JPG)"",""required"":false},
            {""id"":""f9"",""type"":""text"",""label"":""Character Reference"",""placeholder"":""Name and describe the existing character/s for this artwork"",""required"":false},
            {""id"":""f10"",""type"":""dropdown"",""label"":""Body Composition"",""required"":true,""options"":[""Icon"",""Bust-Up"",""Half Body"",""Whole Body""]},
            {""id"":""f11"",""type"":""dropdown"",""label"":""Number of Characters"",""required"":true,""options"":[""One Person"",""Two"",""Three"",""Crowd""]},
            {""id"":""f12"",""type"":""dropdown"",""label"":""Art Style"",""required"":true,""options"":[""Sketch"",""Fully Rendered"",""Chibi""]},
            {""id"":""f13"",""type"":""file"",""label"":""Reference Pose"",""required"":true},
            {""id"":""f14"",""type"":""file"",""label"":""Reference Background"",""required"":false},
            {""id"":""f15"",""type"":""dropdown"",""label"":""Canvas Size"",""required"":true,""options"":[""Square (1:1)"",""Landscape (16:9)"",""A4 Portrait"",""A4 Landscape"",""Large Square (4000×4000)"",""Custom""]},
            {""id"":""f16"",""type"":""text"",""label"":""Custom Canvas Size"",""placeholder"":""Leave blank if you selected a predefined canvas size above"",""required"":false},
            {""id"":""f17"",""type"":""textarea"",""label"":""Other Notes"",""placeholder"":""Provide any additional information"",""required"":false},
            {""id"":""f18"",""type"":""subheader"",""label"":""Payment Details""},
            {""id"":""f19"",""type"":""dropdown"",""label"":""Mode of Payment"",""required"":true,""options"":[""GCash"",""PayMaya"",""Game Credits""]},
            {""id"":""f20"",""type"":""text"",""label"":""Estimated Budget"",""placeholder"":""Enter your estimated budget"",""required"":true}
        ]";

        db.FormTemplates.Add(new FormTemplate
        {
            Version = 1,
            IsCurrent = true,
            CreatedAt = DateTime.Now,
            FieldsJson = defaultFields
        });
        db.SaveChanges();
    }

    // Seed default gallery tags if none exist
    if (!db.GalleryTags.Any())
    {
        db.GalleryTags.Add(new GalleryTag { Name = "Sketch" });
        db.GalleryTags.Add(new GalleryTag { Name = "Fully Rendered" });
        db.GalleryTags.Add(new GalleryTag { Name = "OC" });
        db.GalleryTags.Add(new GalleryTag { Name = "Chibi" });
        db.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();
app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

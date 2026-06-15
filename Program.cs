using Microsoft.EntityFrameworkCore;
using ArtOnline.Data;
using ArtOnline.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
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
            {""id"":""f10"",""type"":""dropdown"",""label"":""Commission Type - Body"",""required"":true,""options"":[""Icon"",""Bust-Up"",""Half Body"",""Whole Body""]},
            {""id"":""f11"",""type"":""dropdown"",""label"":""Commission Type - Persons"",""required"":true,""options"":[""One Person"",""Two"",""Three"",""More""]},
            {""id"":""f12"",""type"":""dropdown"",""label"":""Commission Type - Style"",""required"":true,""options"":[""Sketch"",""Fully Rendered"",""Chibi""]},
            {""id"":""f13"",""type"":""file"",""label"":""Reference Pose"",""required"":true},
            {""id"":""f14"",""type"":""file"",""label"":""Reference Background"",""required"":false},
            {""id"":""f15"",""type"":""dropdown"",""label"":""Canvas Size"",""required"":true,""options"":[""1080x1080"",""1920x1080"",""2480x3508 (A4)"",""3508x2480 (A4 Landscape)"",""4000x4000"",""Other""]},
            {""id"":""f16"",""type"":""text"",""label"":""Custom Canvas Size"",""placeholder"":""Enter custom size"",""required"":false},
            {""id"":""f17"",""type"":""textarea"",""label"":""Other Notes"",""placeholder"":""Provide any additional information"",""required"":false},
            {""id"":""f18"",""type"":""subheader"",""label"":""Mode of Payment""},
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

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

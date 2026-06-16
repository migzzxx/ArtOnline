namespace ArtOnline.Models;

public class GalleryImage
{
    public int Id { get; set; }
    public string ImagePath { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Tags { get; set; } = ""; // comma-separated tag names
    public int HeartCount { get; set; } = 0;
    public DateTime DateAdded { get; set; }
}

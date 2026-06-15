namespace ArtOnline.Models;

public class FormTemplate
{
    public int Id { get; set; }
    public int Version { get; set; }
    public bool IsCurrent { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public string FieldsJson { get; set; } = "";
}

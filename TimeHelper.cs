namespace ArtOnline;

public static class TimeHelper
{
    private static readonly TimeZoneInfo PhTimezone = 
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");

    public static DateTime Now => 
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, PhTimezone);
}

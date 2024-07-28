namespace WeatherApp.Application.Models.Notifications;

public class Notification(string timeStamp, string body)
{
    public string TimeStamp { get; } = timeStamp;
    public string Body { get; } = body;
}
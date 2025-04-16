public record WeatherReportResponse(string RequestedRegion, DateTime RequestedDate, Guid RequestId, int Temperature, string Summary);

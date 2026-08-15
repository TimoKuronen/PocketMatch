using System.Collections.Generic;

public interface IAnalyticsService
{
    void LogEvent(string eventName, Dictionary<string, object> parameters = null);
}

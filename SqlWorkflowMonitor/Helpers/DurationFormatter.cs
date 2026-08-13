namespace SqlWorkflowMonitor.Helpers;

public static class DurationFormatter
{
    public static string Format(long? durationMs)
    {
        if (!durationMs.HasValue)
        {
            return "-";
        }

        TimeSpan duration =
            TimeSpan.FromMilliseconds(durationMs.Value);

        if (duration.TotalHours >= 1)
        {
            return $"{(long)duration.TotalHours} h " +
                   $"{duration.Minutes} min " +
                   $"{duration.Seconds} s";
        }

        if (duration.TotalMinutes >= 1)
        {
            return $"{duration.Minutes} min " +
                   $"{duration.Seconds} s";
        }

        if (duration.TotalSeconds >= 1)
        {
            return $"{duration.TotalSeconds:0.#} s";
        }

        return $"{durationMs.Value} ms";
    }
}
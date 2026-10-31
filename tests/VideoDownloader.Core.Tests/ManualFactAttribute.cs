namespace VideoDownloader.Core.Tests;

public sealed class ManualFactAttribute : FactAttribute
{
    public ManualFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("RUN_MANUAL_TESTS") != "1")
        {
            Skip = "Manual test: set RUN_MANUAL_TESTS=1 to run it.";
        }
    }
}
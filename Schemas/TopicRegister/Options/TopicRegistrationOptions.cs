namespace TopicRegister.Options;

public class TopicRegistrationOptions
{
    public const string SectionName = "TopicRegistration";

    public int MaxRetries { get; set; } = 30;
    public int RetryDelaySeconds { get; set; } = 2;
}

namespace SchemaManager.Options;

public class SchemaRegistrationOptions
{
    public const string SectionName = "SchemaRegistration";

    public int MaxRetries { get; set; } = 30;
    public int RetryDelaySeconds { get; set; } = 2;
}


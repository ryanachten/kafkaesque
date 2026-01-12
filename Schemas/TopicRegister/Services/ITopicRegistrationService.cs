namespace TopicRegister.Services;

public interface ITopicRegistrationService
{
    Task WaitForKafka(CancellationToken cancellationToken = default);
    Task RegisterTopics(CancellationToken cancellationToken = default);
}

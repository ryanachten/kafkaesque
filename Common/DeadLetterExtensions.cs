using Confluent.Kafka;

namespace Common;

public static class DeadLetterQueueHeaders
{
    public const string OriginalTopic = "x-original-topic";
    public const string ExceptionType = "x-exception-type";
    public const string ExceptionMessage = "x-exception-message";
    public const string FailureTimestamp = "x-failure-timestamp";
    public const string Partition = "x-partition";
    public const string Offset = "x-offset";
}

public static class DeadLetterExtensions
{
    public static IEnumerable<(string Key, byte[] Value)> ToDeadLetterHeaderPairs(this IDeadLetterMetadata metadata)
    {
        yield return (DeadLetterQueueHeaders.OriginalTopic, System.Text.Encoding.UTF8.GetBytes(metadata.Topic));
        yield return (DeadLetterQueueHeaders.FailureTimestamp, System.Text.Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")));
        yield return (DeadLetterQueueHeaders.Partition, System.Text.Encoding.UTF8.GetBytes(metadata.Partition.ToString()));
        yield return (DeadLetterQueueHeaders.Offset, System.Text.Encoding.UTF8.GetBytes(metadata.Offset.ToString()));

        if (metadata.Exception != null)
        {
            yield return (DeadLetterQueueHeaders.ExceptionType, System.Text.Encoding.UTF8.GetBytes(metadata.Exception.GetType().Name));
            yield return (DeadLetterQueueHeaders.ExceptionMessage, System.Text.Encoding.UTF8.GetBytes(metadata.Exception.Message));
        }
    }

    public static void AddDeadLetterHeaders(this Headers headers, IDeadLetterMetadata metadata)
    {
        foreach (var (key, value) in metadata.ToDeadLetterHeaderPairs())
        {
            headers.Add(key, value);
        }
    }

    public static string GetOriginalTopic(this Headers headers)
    {
        var header = headers.GetLastBytes(DeadLetterQueueHeaders.OriginalTopic);
        return header != null ? System.Text.Encoding.UTF8.GetString(header) : string.Empty;
    }

    public static string GetFailureTimestamp(this Headers headers)
    {
        var header = headers.GetLastBytes(DeadLetterQueueHeaders.FailureTimestamp);
        return header != null ? System.Text.Encoding.UTF8.GetString(header) : string.Empty;
    }
}

public interface IDeadLetterMetadata
{
    string Topic { get; }
    int Partition { get; }
    long Offset { get; }
    Exception? Exception { get; }
}

public record DeadLetterMetadata(string Topic, int Partition, long Offset, Exception? Exception = null) : IDeadLetterMetadata;
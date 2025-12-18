namespace SchemaRegister.SchemaRegistration;

public interface ISchemaRegistrationService
{
    Task WaitForSchemaRegistry(CancellationToken cancellationToken = default);
    Task RegisterSchemas(CancellationToken cancellationToken = default);
}



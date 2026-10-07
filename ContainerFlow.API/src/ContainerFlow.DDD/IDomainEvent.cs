namespace ContainerFlow.DDD;

public interface IDomainEvent
{
    Guid EventoId => Guid.NewGuid();
    DateTime OcorreuEmUtc => DateTime.UtcNow;
}

using ContainerFlow.DDD;

namespace ContainerFlow.Financeiro.Faturamento;

public class Fatura : AggregateRoot<Guid>
{
    public required string Numero { get; set; } 
    public DateTime DataEmissao { get; set; }
    public DateTime DataVencimento { get; set; }
    public Guid LocacaoId { get; set; }
    public decimal Total { get; set; }
}

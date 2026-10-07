using ContainerFlow.Contracts;
using ContainerFlow.Contracts.Eventos;

namespace ContainerFlow.Financeiro.Faturamento;

public class EmissorDeFaturas
{
    private readonly IRepository<Fatura> _repoFatura;
    private readonly IEventoManager _eventoManager;

    public EmissorDeFaturas(IRepository<Fatura> repoFatura, IEventoManager eventoManager)
    {
        _repoFatura = repoFatura;
        _eventoManager = eventoManager;
    }

    public Task ExecutarAsync() => ExecutarAsync(CancellationToken.None);

    public async Task ExecutarAsync(CancellationToken cancellationToken)
    {
        await _eventoManager.ProcessarEventoIdempotenteAsync<PropostaAprovadaEvent>(
            nameof(PropostaAprovadaEvent),
            nameof(EmissorDeFaturas),
            async (mensagem, ct) =>
            {
                var fatura = new Fatura
                {
                    Id = Guid.NewGuid(),
                    DataEmissao = DateTime.UtcNow,
                    DataVencimento = DateTime.UtcNow.AddDays(5),
                    Numero = $"FAT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}",
                    Total = mensagem.Corpo.ValorProposta
                };

                await _repoFatura.AddAsync(fatura, ct);
            },
            cancellationToken);

        // Suporte retrocompatível caso haja eventos gravados com o nome 'PropostaAprovada'
        await _eventoManager.ProcessarEventoIdempotenteAsync<PropostaAprovadaEvent>(
            "PropostaAprovada",
            nameof(EmissorDeFaturas),
            async (mensagem, ct) =>
            {
                var fatura = new Fatura
                {
                    Id = Guid.NewGuid(),
                    DataEmissao = DateTime.UtcNow,
                    DataVencimento = DateTime.UtcNow.AddDays(5),
                    Numero = $"FAT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}",
                    Total = mensagem.Corpo.ValorProposta
                };

                await _repoFatura.AddAsync(fatura, ct);
            },
            cancellationToken);
    }
}

using ContainerFlow.Api.Eventos;
using ContainerFlow.API.Data;
using ContainerFlow.API.Data.Repositories;
using ContainerFlow.API.Identity;
using ContainerFlow.Engenharia.Containers;
using ContainerFlow.Financeiro.Faturamento;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<IdentityDbContext>(options =>
{
    options
        .UseSqlServer(builder.Configuration.GetConnectionString("IdentityDB"));
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options
        .UseSqlServer(builder.Configuration.GetConnectionString("ContainerFlowDB"));
});

// Unit of Work
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

// Repositórios Semânticos por Bounded Context
builder.Services.AddScoped<ClienteRepository>();
builder.Services.AddScoped<IClienteRepository>(sp => sp.GetRequiredService<ClienteRepository>());
builder.Services.AddScoped<IRepository<Cliente>>(sp => sp.GetRequiredService<ClienteRepository>());

builder.Services.AddScoped<SolicitacaoRepository>();
builder.Services.AddScoped<ISolicitacaoRepository>(sp => sp.GetRequiredService<SolicitacaoRepository>());
builder.Services.AddScoped<IRepository<PedidoLocacao>>(sp => sp.GetRequiredService<SolicitacaoRepository>());

builder.Services.AddScoped<PropostaRepository>();
builder.Services.AddScoped<IPropostaRepository>(sp => sp.GetRequiredService<PropostaRepository>());
builder.Services.AddScoped<IRepository<Proposta>>(sp => sp.GetRequiredService<PropostaRepository>());

builder.Services.AddScoped<LocacaoRepository>();
builder.Services.AddScoped<ILocacaoRepository>(sp => sp.GetRequiredService<LocacaoRepository>());
builder.Services.AddScoped<IRepository<Locacao>>(sp => sp.GetRequiredService<LocacaoRepository>());

builder.Services.AddScoped<ConteinerRepository>();
builder.Services.AddScoped<IConteinerRepository>(sp => sp.GetRequiredService<ConteinerRepository>());
builder.Services.AddScoped<IRepository<Conteiner>>(sp => sp.GetRequiredService<ConteinerRepository>());

builder.Services.AddScoped<FaturaRepository>();
builder.Services.AddScoped<IFaturaRepository>(sp => sp.GetRequiredService<FaturaRepository>());
builder.Services.AddScoped<IRepository<Fatura>>(sp => sp.GetRequiredService<FaturaRepository>());

// Eventos e Serviços de Negócio
builder.Services.AddScoped<IEventoManager, EventoManager>();
builder.Services.AddScoped<ICalculadoraPrazosLocacao, CalculadoraPadraoPrazosLocacao>();
builder.Services.AddScoped<IPropostaService, PropostaService>();
builder.Services.AddScoped<IAcessoManager, AcessoManagerWithIdentity>();
builder.Services.AddScoped<EmissorDeFaturas>();
builder.Services.AddScoped<ReservadorDeConteiner>();

builder.Services
    .AddIdentityApiEndpoints<AppUser>(options => options.SignIn.RequireConfirmedEmail = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<IdentityDbContext>();

builder.Services.AddAuthorization();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.ClaimsIdentity.UserIdClaimType = "ClienteId";
});

builder.Services.AddHangfire(config =>
{
    config.UseSqlServerStorage(builder.Configuration.GetConnectionString("ContainerFlowDB"));
}).AddHangfireServer(options =>
    options.SchedulePollingInterval = TimeSpan.FromSeconds(5));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapIdentityEndpoints()
    .MapClientesEndpoints()
    .MapAprovacaoClientesEndpoints()
    .MapSolicitacoesEndpoints()
    .MapPropostasEndpoints()
    .MapLocacoesEndpoints()
    .MapConteineresEndpoints();

// Background Consumers (EDA Monolito Modular)
var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();

recurringJobManager.AddOrUpdate<EmissorDeFaturas>(
    nameof(EmissorDeFaturas),
    job => job.ExecutarAsync(),
    Cron.Minutely
);

recurringJobManager.AddOrUpdate<ReservadorDeConteiner>(
    nameof(ReservadorDeConteiner),
    job => job.ExecutarAsync(),
    Cron.Minutely
);

app.Run();
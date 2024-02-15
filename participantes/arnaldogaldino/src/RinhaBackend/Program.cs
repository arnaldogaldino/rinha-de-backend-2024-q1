using RinhaBackend.Models;
using FluentValidation;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<IValidator<Transacao>, TransacaoValidator>();
var app = builder.Build();

var connString = app.Configuration.GetConnectionString("DefaultConnection");
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connString);
var dataSource = dataSourceBuilder.Build();
var sinaleiro = new SemaphoreSlim(100);

app.MapPost("/clientes/{id}/transacoes", async (IValidator<Transacao> validator, int id, Transacao transacao) =>
{
    await sinaleiro.WaitAsync();

    using var conn = await dataSource.OpenConnectionAsync();
    using var trans = await conn.BeginTransactionAsync();

    var validation = await validator.ValidateAsync(transacao);    
    if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());

    var limite = 0;
    var saldo = 0;
        
    await using (var cmd = dataSource.CreateCommand("SELECT limite, valor FROM (SELECT c.limite, s.valor FROM clientes c JOIN saldos s ON s.cliente_id=c.id WHERE c.id=$1) FOR UPDATE;"))
    {
        cmd.Parameters.AddWithValue(id);
        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        if (!reader.HasRows)
        {
            validation.Errors.Add(new FluentValidation.Results.ValidationFailure("cliente_id", "o cliente não encontrado"));
            return Results.ValidationProblem(validation.ToDictionary());
        }

        limite = await reader.GetFieldValueAsync<int>(0);
        saldo = await reader.GetFieldValueAsync<int>(1);
    }

    if (transacao.Tipo.Equals("c"))
    {
        saldo = saldo + transacao.Valor;
    }

    if (transacao.Tipo.Equals("d"))
    {
        saldo = saldo - transacao.Valor;
    }

    if (transacao.Tipo.Equals("d") && (limite * -1) > saldo)
    {
        validation.Errors.Add(new FluentValidation.Results.ValidationFailure("Valor", "o cliente não tem limite suficiente para esta transação"));
        return Results.ValidationProblem(validation.ToDictionary());
    }

    await using (var cmd = dataSource.CreateCommand("INSERT INTO transacoes (cliente_id, valor, tipo, descricao, realizada_em) VALUES ($1, $2, $3, $4, now());"))
    {
        cmd.Parameters.AddWithValue(id);
        cmd.Parameters.AddWithValue(transacao.Valor);
        cmd.Parameters.AddWithValue(transacao.Tipo);
        cmd.Parameters.AddWithValue(transacao.Descricao);
        await cmd.ExecuteNonQueryAsync();
    }

    await using (var cmd = dataSource.CreateCommand("UPDATE saldos SET valor=$2 WHERE cliente_id=$1;"))
    {
        cmd.Parameters.AddWithValue(id);
        cmd.Parameters.AddWithValue(saldo);
        await cmd.ExecuteNonQueryAsync();
    }

    var limite_now = 0;

    await using (var cmd = dataSource.CreateCommand("SELECT limite, valor FROM (SELECT c.limite, s.valor FROM clientes c JOIN saldos s ON s.cliente_id=c.id WHERE c.id=$1) FOR UPDATE;"))
    {
        cmd.Parameters.AddWithValue(id);
        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        if (!reader.HasRows)
        {
            validation.Errors.Add(new FluentValidation.Results.ValidationFailure("cliente_id", "o cliente não encontrado"));
            return Results.ValidationProblem(validation.ToDictionary());
        }

        limite_now = await reader.GetFieldValueAsync<int>(0);
    }

    if (transacao.Tipo.Equals("d") && (limite_now * -1) > saldo)
    {
        validation.Errors.Add(new FluentValidation.Results.ValidationFailure("Valor", "o cliente não tem limite suficiente para esta transação"));
        await trans.RollbackAsync();
        return Results.ValidationProblem(validation.ToDictionary());
    }

    await trans.CommitAsync();
    
    sinaleiro.Release();
    await Task.Delay(100);
    return Results.Ok(new { limite = limite, saldo = saldo });
});

app.MapGet("/clientes/{id}/extrato", async (IValidator<Transacao> validator, int id) =>
{
    await sinaleiro.WaitAsync();
    using var conn = await dataSource.OpenConnectionAsync();

    if (id == 0) return Results.NotFound("cliente inválido");
    var validation = await validator.ValidateAsync(new Transacao());

    var result = new Extrato();

    await using (var cmd = dataSource.CreateCommand(@" SELECT total, data_extrato, limite FROM
                                                      (SELECT s.valor as total, now() as data_extrato, c.limite
                                                         FROM clientes c
                                                         JOIN saldos s on s.cliente_id=c.id
                                                        WHERE c.id = $1) FOR UPDATE;"))
    {
        cmd.Parameters.AddWithValue(id);
        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        if (!reader.HasRows)
        {
            validation.Errors.Add(new FluentValidation.Results.ValidationFailure("cliente_id", "o cliente não encontrado"));
            return Results.ValidationProblem(validation.ToDictionary());
        }
        result.Saldo.Total = await reader.GetFieldValueAsync<int>(0);
        result.Saldo.DataExtrato = await reader.GetFieldValueAsync<DateTime>(1);
        result.Saldo.Limite = await reader.GetFieldValueAsync<int>(2);
    }

    await using (var cmd = dataSource.CreateCommand(@"SELECT valor, tipo, descricao, realizada_em FROM
                                                      (SELECT t.valor, t.tipo, t.descricao, t.realizada_em
                                                         FROM transacoes t
                                                         JOIN clientes c on c.id=t.cliente_id
                                                         JOIN saldos s on s.cliente_id=c.id
                                                        WHERE t.cliente_id=$1
                                                        ORDER BY t.realizada_em DESC
                                                        LIMIT 10) FOR UPDATE;"))
    {
        cmd.Parameters.AddWithValue(id);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var item = new Transacao();
            item.Valor = await reader.GetFieldValueAsync<int>(0);
            item.Tipo = await reader.GetFieldValueAsync<string>(1);
            item.Descricao = await reader.GetFieldValueAsync<string>(2);
            item.RealizadaEm = await reader.GetFieldValueAsync<DateTime>(3);
            result.UltimasTransacoes.Add(item);
        }
    }
    
    sinaleiro.Release();
    await Task.Delay(100);
    return Results.Ok(result);
});

app.Run();

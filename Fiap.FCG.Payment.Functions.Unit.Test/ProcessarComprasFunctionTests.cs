using Fiap.FCG.Payment.Functions.Contracts;
using Fiap.FCG.Payment.Functions.Functions;
using Fiap.FCG.Payment.Functions.Services;
using Fiap.FCG.Payment.Functions.Services.Models;
using Fiap.FCG.Payment.Functions.Unit.Test.Helpers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using System.Text.Json;
using Xunit;

namespace Fiap.FCG.Payment.Functions.Unit.Test
{
    public class ProcessarComprasFunctionTests
    {
        private readonly ILogger<ProcessarComprasFunction> _logger =
            Substitute.For<ILogger<ProcessarComprasFunction>>();

        private readonly IPaymentApiClient _paymentApi =
            Substitute.For<IPaymentApiClient>();

        private ProcessarComprasFunction CreateSut()
            => new(_logger, _paymentApi);

        [Fact]
        public async Task Run_QuandoJsonInvalido_DeveLancarException_E_NaoChamarApi()
        {
            // Arrange
            var sut = CreateSut();
            var ctx = new TestFunctionContext();
            var invalidJson = "{ invalid-json }";

            // Act + Assert
            await Should.ThrowAsync<Exception>(() =>
                sut.Run(invalidJson, ctx, CancellationToken.None));

            await _paymentApi
                .DidNotReceiveWithAnyArgs()
                .CriarPagamentoAsync(default!, default);
        }

        [Fact]
        public async Task Run_QuandoPaymentApiRetornaFalha_DeveLancarInvalidOperation()
        {
            // Arrange
            var sut = CreateSut();
            var ctx = new TestFunctionContext();

            var compra = new CompraRealizadaEvent
            {
                CompraId = 1,
                UsuarioId = 10,
                ValorTotal = 99.90m,
                MetodoPagamento = EMetodoPagamento.CartaoCredito,
                BandeiraCartao = EBandeiraCartao.Visa,
                DataCompra = DateTime.UtcNow
            };

            var message = JsonSerializer.Serialize(compra);

            _paymentApi
                .CriarPagamentoAsync(
                    Arg.Any<CriarPagamentoRequest>(),
                    Arg.Any<CancellationToken>())
                .Returns(new CriarPagamentoResponse
                {
                    Sucesso = false,
                    Mensagem = "Erro ao criar pagamento"
                });

            // Act
            var ex = await Should.ThrowAsync<InvalidOperationException>(() =>
                sut.Run(message, ctx, CancellationToken.None));

            // Assert
            ex.Message.ShouldBe("Erro ao criar pagamento");

            await _paymentApi.Received(1).CriarPagamentoAsync(
                Arg.Is<CriarPagamentoRequest>(r =>
                    r.CompraId == compra.CompraId &&
                    r.UsuarioId == compra.UsuarioId &&
                    r.ValorTotal == compra.ValorTotal &&
                    r.MetodoPagamento == compra.MetodoPagamento &&
                    r.BandeiraCartao == compra.BandeiraCartao
                ),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Run_QuandoSucesso_DeveChamarPaymentApi_1Vez()
        {
            // Arrange
            var sut = CreateSut();
            var ctx = new TestFunctionContext();

            var compra = new CompraRealizadaEvent
            {
                CompraId = 2,
                UsuarioId = 20,
                ValorTotal = 150m,
                MetodoPagamento = EMetodoPagamento.Pix,
                BandeiraCartao = null,
                DataCompra = DateTime.UtcNow
            };

            var message = JsonSerializer.Serialize(compra);

            _paymentApi
                .CriarPagamentoAsync(
                    Arg.Any<CriarPagamentoRequest>(),
                    Arg.Any<CancellationToken>())
                .Returns(new CriarPagamentoResponse
                {
                    Sucesso = true,
                    PagamentoId = 123,
                    Status = "Pendente"
                });

            // Act
            await sut.Run(message, ctx, CancellationToken.None);

            // Assert
            await _paymentApi.Received(1).CriarPagamentoAsync(
                Arg.Is<CriarPagamentoRequest>(r =>
                    r.CompraId == compra.CompraId &&
                    r.UsuarioId == compra.UsuarioId &&
                    r.ValorTotal == compra.ValorTotal &&
                    r.MetodoPagamento == compra.MetodoPagamento &&
                    r.BandeiraCartao == compra.BandeiraCartao
                ),
                Arg.Any<CancellationToken>());
        }
    }
}

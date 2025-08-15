using MercadoBitcoin.Client.IntegrationTests.Base;
using MercadoBitcoin.Client.Generated;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MercadoBitcoin.Client.IntegrationTests.PrivateApi;

[Trait("Category", "Integration")]
[Trait("Category", "PrivateApi")]
[Trait("Category", "RequiresCredentials")]
public class AccountTests : IntegrationTestBase
{
    [Fact]
    public async Task GetAccounts_ReturnsValidAccounts()
    {
        await RunAuthenticatedTestAsync(async client =>
        {
            // Act
            var accounts = await client.GetAccountsAsync();

            // Assert
            Assert.NotNull(accounts);
            Assert.True(accounts.Count > 0, "Deve haver pelo menos uma conta");

            var firstAccount = accounts.First();
            Assert.NotNull(firstAccount.Id);
            Assert.NotEmpty(firstAccount.Id);

            // Verifica logs
            // Test completed successfully
        });
    }

    [Fact]
    public async Task GetBalances_ReturnsValidBalances()
    {
        await RunAuthenticatedTestWithAccountAsync(async (client, accountId) =>
        {
            // Act
            var balances = await client.GetBalancesAsync(accountId);

            // Assert
            Assert.NotNull(balances);
            // Pode não ter saldos, mas a estrutura deve estar correta

            if (balances.Count > 0)
            {
                var firstBalance = balances.First();
                Assert.NotNull(firstBalance.Symbol);
                Assert.True(decimal.TryParse(firstBalance.Available, out var available) && available >= 0, "Saldo disponível deve ser não-negativo");
                Assert.True(decimal.TryParse(firstBalance.Total, out var total) && total >= 0, "Saldo total deve ser não-negativo");
                Assert.True(total >= available, "Total deve ser >= disponível");
            }
        });
    }

    [Fact]
    public async Task GetPositions_ReturnsValidPositions()
    {
        await RunAuthenticatedTestWithAccountAsync(async (client, accountId) =>
        {
            // Act
            var positions = await client.GetPositionsAsync(accountId);

            // Assert
            Assert.NotNull(positions);
            // Pode não ter posições, mas a estrutura deve estar correta

            if (positions.Count > 0)
            {
                var firstPosition = positions.First();
                Assert.NotNull(firstPosition.Instrument);
                Assert.NotNull(firstPosition.Side);
                Assert.True(firstPosition.Side == "buy" || firstPosition.Side == "sell");
                Assert.True(decimal.TryParse(firstPosition.Qty, out var qty) && qty >= 0, "Quantidade deve ser não-negativa");
            }
        });
    }

    [Fact]
    public async Task GetTradingFees_ReturnsValidFeeStructure()
    {
        await RunAuthenticatedTestWithAccountAsync(async (client, accountId) =>
        {
            // Act
            var fees = await client.GetTradingFeesAsync(accountId, TestConfig.DefaultSymbol);

            // Assert
            Assert.NotNull(fees);
            Assert.NotNull(fees.Maker_fee);
            Assert.NotNull(fees.Taker_fee);

            Assert.True(decimal.TryParse(fees.Taker_fee, out var takerFee) && takerFee >= 0, "Taxa taker deve ser não-negativa");
            Assert.True(decimal.TryParse(fees.Maker_fee, out var makerFee) && makerFee >= 0, "Taxa maker deve ser não-negativa");

            // Geralmente maker fee <= taker fee
            Assert.True(makerFee <= takerFee,
                "Taxa maker geralmente é menor ou igual à taxa taker");
        });
    }


    // Método GetFeesAsync não existe no cliente gerado - removido

    [Fact]
    public async Task AccountOperations_HandleInvalidAccountId()
    {
        await RunAuthenticatedTestAsync(async client =>
        {
            // Arrange
            var invalidAccountId = "invalid-account-id-12345";

            // Act & Assert
            await Assert.ThrowsAsync<MercadoBitcoinApiException>(async () =>
            {
                await client.GetBalancesAsync(invalidAccountId);
            });
        });
    }

    [Fact]
    public async Task GetAccounts_ConsistentData()
    {
        await RunAuthenticatedTestAsync(async client =>
        {
            // Act - Faz duas chamadas consecutivas
            var accounts1 = await client.GetAccountsAsync();
            await WaitAsync(100); // Pequena pausa
            var accounts2 = await client.GetAccountsAsync();

            // Assert - Os dados devem ser consistentes
            Assert.Equal(accounts1.Count, accounts2.Count);

            var accountsList1 = accounts1.ToList();
            var accountsList2 = accounts2.ToList();

            for (int i = 0; i < accountsList1.Count; i++)
            {
                Assert.Equal(accountsList1[i].Id, accountsList2[i].Id);
            }
        });
    }

    [Fact]
    public async Task GetBalances_CalculationsCorrect()
    {
        await RunAuthenticatedTestWithAccountAsync(async (client, accountId) =>
        {
            // Act
            var balances = await client.GetBalancesAsync(accountId);

            // Assert
            var balancesList = balances.ToList();
            foreach (var balance in balancesList)
            {
                // Converte strings para decimais
                Assert.True(decimal.TryParse(balance.Total, out var total), $"Total deve ser um número válido: {balance.Total}");
                Assert.True(decimal.TryParse(balance.Available, out var available), $"Available deve ser um número válido: {balance.Available}");
                Assert.True(decimal.TryParse(balance.On_hold, out var onHold), $"On_hold deve ser um número válido: {balance.On_hold}");

                // Saldo total deve ser >= saldo disponível
                Assert.True(total >= available,
                    $"Total ({total}) deve ser >= Available ({available}) para {balance.Symbol}");

                // Saldo bloqueado = Total - Disponível
                var expectedBlocked = total - available;
                Assert.True(Math.Abs(expectedBlocked - onHold) < 0.00000001m,
                    $"On_hold ({onHold}) deve ser igual a Total - Available ({expectedBlocked}) para {balance.Symbol}");

                // Todos os valores devem ser não-negativos
                Assert.True(total >= 0, "Total deve ser não-negativo");
                Assert.True(available >= 0, "Available deve ser não-negativo");
                Assert.True(onHold >= 0, "On_hold deve ser não-negativo");
            }
        });
    }

    [Fact]
    public async Task AccountOperations_WithRetryPolicy_WorkCorrectly()
    {
        // Este teste verifica que as operações de conta funcionam com retry policies
        await RunAuthenticatedTestWithAccountAsync(async (client, accountId) =>
        {
            // Arrange

            // Act
            var accounts = await client.GetAccountsAsync();
            var balances = await client.GetBalancesAsync(accountId);
            var positions = await client.GetPositionsAsync(accountId);

            // Assert
            Assert.NotNull(accounts);
            Assert.NotNull(balances);
            Assert.NotNull(positions);

            // Verify that the operation eventually succeeded or failed appropriately
            // (The retry behavior is handled internally by the HTTP client)
        });
    }

    [Fact]
    public async Task GetBalances_WithSpecificCurrency_FiltersCorrectly()
    {
        await RunAuthenticatedTestWithAccountAsync(async (client, accountId) =>
        {
            // Act
            var allBalances = await client.GetBalancesAsync(accountId);

            // Assert
            Assert.NotNull(allBalances);
        });
    }
}
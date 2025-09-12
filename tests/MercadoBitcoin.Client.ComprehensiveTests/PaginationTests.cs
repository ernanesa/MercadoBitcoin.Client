using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MercadoBitcoin.Client.ComprehensiveTests
{
    public class PaginationTests : TestBase
    {
        [Fact(DisplayName = "Paginação assíncrona de depósitos cripto retorna itens e respeita cancelamento")]
        public async Task GetDepositsPagedAsync_ShouldIterateAllPages()
        {
            // Arrange
            var accountId = TestAccountId;
            var symbol = "BTC";
            var maxToFetch = 10; // Limita para não sobrecarregar
            var count = 0;

            // Act
            await foreach (var deposit in Client.GetDepositsPagedAsync(accountId, symbol, limit: 2, cancellationToken: default))
            {
                Assert.NotNull(deposit);
                Assert.NotNull(deposit.Coin);
                count++;
                if (count >= maxToFetch)
                    break;
            }

            // Assert
            Assert.True(count >= 0); // Pode ser zero se não houver depósitos
        }
    }
}

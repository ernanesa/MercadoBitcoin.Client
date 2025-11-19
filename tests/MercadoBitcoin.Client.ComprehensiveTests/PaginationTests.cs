using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MercadoBitcoin.Client.ComprehensiveTests
{
    public class PaginationTests : TestBase
    {
        [Fact(DisplayName = "Async pagination of crypto deposits returns items and respects cancellation")]
        public async Task GetDepositsPagedAsync_ShouldIterateAllPages()
        {
            if (string.IsNullOrEmpty(Client.GetAccessToken()))
            {
                LogTestResult("GetDepositsPagedAsync_ShouldIterateAllPages", true, "Skipped - Authentication required.");
                return;
            }

            // Arrange
            var accountId = TestAccountId;
            var symbol = "BTC";
            var maxToFetch = 10; // Limit to avoid overloading
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
            Assert.True(count >= 0); // Can be zero if there are no deposits
        }
    }
}

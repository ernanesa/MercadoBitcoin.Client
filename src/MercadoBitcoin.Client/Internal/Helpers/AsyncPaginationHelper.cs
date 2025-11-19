using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MercadoBitcoin.Client.Internal.Helpers
{
    /// <summary>
    /// Generic helper for asynchronous pagination of limit/page based APIs.
    /// </summary>
    public static class AsyncPaginationHelper
    {
        /// <summary>
        /// Asynchronously iterates over API pages, returning each item individually.
        /// </summary>
        /// <typeparam name="T">Type of item returned by the API.</typeparam>
        /// <param name="fetchPage">Function that receives (limit, page, cancellationToken) and returns a page of results.</param>
        /// <param name="pageSize">Page size (limit).</param>
        /// <param name="startPage">Start page (default 1).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>IAsyncEnumerable of T.</returns>
        public static async IAsyncEnumerable<T> PaginateAsync<T>(
            Func<int, int, CancellationToken, Task<ICollection<T>>> fetchPage,
            int pageSize,
            int startPage = 1,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var page = startPage;
            while (true)
            {
                var results = await fetchPage(pageSize, page, cancellationToken).ConfigureAwait(false);
                if (results == null || results.Count == 0)
                    yield break;
                foreach (var item in results)
                    yield return item;
                if (results.Count < pageSize)
                    yield break;
                page++;
            }
        }
    }
}

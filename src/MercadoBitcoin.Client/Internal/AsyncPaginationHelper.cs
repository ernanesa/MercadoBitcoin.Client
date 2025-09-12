using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MercadoBitcoin.Client.Internal
{
    /// <summary>
    /// Helper genérico para paginação assíncrona de APIs baseadas em limit/page.
    /// </summary>
    public static class AsyncPaginationHelper
    {
        /// <summary>
        /// Itera de forma assíncrona sobre páginas de uma API, retornando cada item individualmente.
        /// </summary>
        /// <typeparam name="T">Tipo do item retornado pela API.</typeparam>
        /// <param name="fetchPage">Função que recebe (limit, page, cancellationToken) e retorna uma página de resultados.</param>
        /// <param name="pageSize">Tamanho da página (limit).</param>
        /// <param name="startPage">Página inicial (default 1).</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>IAsyncEnumerable de T.</returns>
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

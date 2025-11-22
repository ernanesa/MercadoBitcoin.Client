using System;

namespace MercadoBitcoin.Client.Models.Fast
{
    public readonly struct FastOrderBook
    {
        public ReadOnlyMemory<FastOrder> Bids { get; init; }
        public ReadOnlyMemory<FastOrder> Asks { get; init; }
    }

    public readonly struct FastOrder
    {
        public decimal Price { get; init; }
        public decimal Quantity { get; init; }

        public FastOrder(decimal price, decimal quantity)
        {
            Price = price;
            Quantity = quantity;
        }
    }
}

using System;

namespace MercadoBitcoin.Client.Models.Fast
{
    public readonly struct FastTicker
    {
        public string Pair { get; init; }

        public decimal High 
        { 
            get; 
            init => field = value >= 0 ? value : throw new ArgumentException("High price must be non-negative"); 
        }

        public decimal Low 
        { 
            get; 
            init => field = value >= 0 ? value : throw new ArgumentException("Low price must be non-negative"); 
        }

        public decimal Vol 
        { 
            get; 
            init => field = value >= 0 ? value : throw new ArgumentException("Volume must be non-negative"); 
        }

        public decimal Last 
        { 
            get; 
            init => field = value >= 0 ? value : throw new ArgumentException("Last price must be non-negative"); 
        }

        public decimal Buy 
        { 
            get; 
            init => field = value >= 0 ? value : throw new ArgumentException("Buy price must be non-negative"); 
        }

        public decimal Sell 
        { 
            get; 
            init => field = value >= 0 ? value : throw new ArgumentException("Sell price must be non-negative"); 
        }

        public decimal Open 
        { 
            get; 
            init => field = value >= 0 ? value : throw new ArgumentException("Open price must be non-negative"); 
        }

        public long Date { get; init; }
    }
}

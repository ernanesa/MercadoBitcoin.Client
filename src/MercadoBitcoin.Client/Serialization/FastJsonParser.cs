using System;
using System.Buffers;
using System.Buffers.Text;
using System.Text.Json;
using System.Collections.Generic;
using MercadoBitcoin.Client.Models.Fast;

namespace MercadoBitcoin.Client.Serialization
{
    public static class FastJsonParser
    {
        public static FastTicker ParseTicker(ReadOnlySpan<byte> json)
        {
            var reader = new Utf8JsonReader(json);
            
            decimal high = 0, low = 0, vol = 0, last = 0, buy = 0, sell = 0, open = 0;
            long date = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (reader.ValueTextEquals("ticker"u8))
                    {
                        continue;
                    }

                    if (reader.ValueTextEquals("high"u8)) { reader.Read(); high = ParseDecimal(ref reader); }
                    else if (reader.ValueTextEquals("low"u8)) { reader.Read(); low = ParseDecimal(ref reader); }
                    else if (reader.ValueTextEquals("vol"u8)) { reader.Read(); vol = ParseDecimal(ref reader); }
                    else if (reader.ValueTextEquals("last"u8)) { reader.Read(); last = ParseDecimal(ref reader); }
                    else if (reader.ValueTextEquals("buy"u8)) { reader.Read(); buy = ParseDecimal(ref reader); }
                    else if (reader.ValueTextEquals("sell"u8)) { reader.Read(); sell = ParseDecimal(ref reader); }
                    else if (reader.ValueTextEquals("open"u8)) { reader.Read(); open = ParseDecimal(ref reader); }
                    else if (reader.ValueTextEquals("date"u8)) { reader.Read(); date = reader.GetInt64(); }
                }
            }

            return new FastTicker
            {
                High = high, Low = low, Vol = vol, Last = last,
                Buy = buy, Sell = sell, Open = open, Date = date
            };
        }

        public static FastOrderBook ParseOrderBook(ReadOnlySpan<byte> json)
        {
             var reader = new Utf8JsonReader(json);
             FastOrder[] bids = Array.Empty<FastOrder>();
             FastOrder[] asks = Array.Empty<FastOrder>();

             while (reader.Read())
             {
                 if (reader.TokenType == JsonTokenType.PropertyName)
                 {
                     if (reader.ValueTextEquals("bids"u8))
                     {
                         reader.Read(); // StartArray
                         bids = ParseOrders(ref reader);
                     }
                     else if (reader.ValueTextEquals("asks"u8))
                     {
                         reader.Read(); // StartArray
                         asks = ParseOrders(ref reader);
                     }
                 }
             }

             return new FastOrderBook
             {
                 Bids = bids,
                 Asks = asks
             };
        }

        private static FastOrder[] ParseOrders(ref Utf8JsonReader reader)
        {
            var list = new List<FastOrder>(50);

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray) break;

                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    // [price, quantity]
                    reader.Read(); // price
                    decimal price = ParseDecimal(ref reader);
                    
                    reader.Read(); // quantity
                    decimal quantity = ParseDecimal(ref reader);
                    
                    reader.Read(); // EndArray (inner)
                    
                    list.Add(new FastOrder(price, quantity));
                }
            }
            
            return list.ToArray();
        }

        private static decimal ParseDecimal(ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                if (Utf8Parser.TryParse(reader.ValueSpan, out decimal value, out _))
                {
                    return value;
                }
                var s = reader.GetString();
                return decimal.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0;
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetDecimal();
            }
            return 0;
        }
    }
}

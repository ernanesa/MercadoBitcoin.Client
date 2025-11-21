# Error Report: Candles Collection (MercadoBitcoin.Client) - RESOLVED

## Context
- Previous issues with POST /saveCandles/BTC-BRL/15m and GET /api/candles returning empty.
- Other endpoints (tickers, trades, orderbook, symbols) worked fine.

## Resolution Status
- Library now has strongly-typed `GetCandlesAsync` with symbol normalization (BTC-BRL -> btcbrl), type parameter (15m), and proper from/to/limit handling.
- CandleData.cs and CandleExtensions.cs provide full support.
- Tests cover candle retrieval and SIMD analysis.
- No further action needed; issue fixed in v4.0+.

## Historical Notes (Translated)
[Original Portuguese content translated and archived here for reference if needed.]
# Mercado Bitcoin Website Analysis

## Overview
The Mercado Bitcoin website (`mercadobitcoin.com.br`) is the primary user interface for the platform, serving over 4 million customers. It is designed for both beginners and professional traders.

## Architecture & Navigation
The site follows a modern SPA (Single Page Application) or hybrid architecture, focusing on high availability and real-time updates.

### Key Navigation Flows
1. **Onboarding**: Landing page -> Create Account -> KYC (Know Your Customer) -> Deposit.
2. **Trading (Beginner)**: Dashboard -> Select Asset -> Buy/Sell (Market Order).
3. **Trading (Pro)**: Dashboard -> Trade -> Advanced Chart (TradingView) -> Orderbook -> Limit/Stop Orders.
4. **Products**:
   - **Criptomoedas**: Direct trading of 330+ assets.
   - **Renda Fixa Digital**: Tokenized assets with fixed returns.
   - **Renda Passiva**: Staking and yield-generating products.
   - **MB Pay**: Digital account and card integration.

## Interface Elements
- **Real-time Tickers**: Dynamic price updates on the home page and dashboard.
- **Interactive Charts**: Integration with TradingView for technical analysis.
- **Orderbook Visualization**: Real-time depth of market display.
- **Asset Simulation**: Quick calculator for BRL to Crypto conversion.

## API Integration Patterns
The website integrates the API in several ways:
1. **Public Data Polling/Streaming**: Tickers and Orderbooks are updated via WebSocket or high-frequency polling.
2. **Authenticated Actions**: Trading and Wallet operations are secured via JWT tokens stored in secure cookies or local storage.
3. **BFF (Backend for Frontend)**: The website likely uses a BFF layer to aggregate multiple API calls into a single response for the UI, similar to the `ExecuteBatchAsync` pattern in our library.

## Practical Application Cases
1. **Arbitrage Bots**: Using the API to monitor price differences across exchanges.
2. **Portfolio Management**: Aggregating balances and performance metrics across multiple accounts.
3. **Automated Trading**: Executing strategies based on technical indicators (Candles) and real-time market depth (Orderbook).
4. **Payment Integration**: Using the Wallet API to accept crypto payments or automate withdrawals.

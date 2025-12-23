# Mercado Bitcoin API v4 Analysis

## Overview
The Mercado Bitcoin API v4 is a RESTful interface providing access to market data, trading, and wallet operations. It supports HTTP/2 and WebSocket for real-time data.

### Base URL
`https://api.mercadobitcoin.net/api/v4`

### Authentication
- **Type**: Bearer Token (JWT)
- **Flow**: POST `/authorize` with `login` (API Token ID) and `password` (API Token Secret).
- **Header**: `Authorization: Bearer <ACCESS_TOKEN>`

### Rate Limits
- **Global**: 500 requests/minute (combined public and private).
- **Specific Endpoints**:
  - Public Data (Tickers, Orderbook, Trades, Candles, Symbols): 1 req/sec.
  - Trading (Place/Cancel Order): 3 req/sec.
  - Trading (List Orders): 10 req/sec.
  - Account (Balances, Positions): 3 req/sec.
  - Cancel All Orders: 1 req/min.

---

## Endpoints Specification

### 🔓 Public Data
| Method | Endpoint | Description | Parameters |
|--------|----------|-------------|------------|
| GET | `/{asset}/fees` | Withdrawal fees | `asset` (path), `network` (query) |
| GET | `/{symbol}/orderbook` | Depth of market | `symbol` (path), `limit` (query, max 1000) |
| GET | `/{symbol}/trades` | Execution history | `symbol` (path), `tid`, `since`, `from`, `to`, `limit` |
| GET | `/candles` | OHLCV history | `symbol`, `resolution`, `to`, `from`, `countback` |
| GET | `/symbols` | Tradable instruments | `symbols` (comma-separated list) |
| GET | `/tickers` | Current prices | `symbols` (comma-separated list) |
| GET | `/{asset}/networks` | Available networks | `asset` (path) |

### 🔐 Account
| Method | Endpoint | Description | Parameters |
|--------|----------|-------------|------------|
| GET | `/accounts` | List user accounts | None |
| GET | `/accounts/{accountId}/balances` | Account balances | `accountId` (path) |
| GET | `/accounts/{accountId}/tier` | Fee tier | `accountId` (path) |
| GET | `/accounts/{accountId}/{symbol}/fees` | Trading fees | `accountId`, `symbol` (path) |
| GET | `/accounts/{accountId}/positions` | Open positions | `accountId` (path), `symbols` (query) |

### 📈 Trading
| Method | Endpoint | Description | Parameters |
|--------|----------|-------------|------------|
| GET | `/accounts/{accountId}/{symbol}/orders` | List orders (pair) | `symbol`, `accountId`, `status`, `side`, etc. |
| POST | `/accounts/{accountId}/{symbol}/orders` | Place order | `symbol`, `accountId`, `type`, `side`, `qty`, `limitPrice`, `async` |
| DELETE | `/accounts/{accountId}/{symbol}/orders/{orderId}` | Cancel order | `accountId`, `symbol`, `orderId`, `async` |
| GET | `/accounts/{accountId}/{symbol}/orders/{orderId}` | Get order details | `accountId`, `symbol`, `orderId` |
| DELETE | `/accounts/{accountId}/cancel_all_open_orders` | Bulk cancel | `accountId`, `symbol`, `has_executions` |
| GET | `/accounts/{accountId}/orders` | List all orders | `accountId`, `symbol`, `status`, `size` |

### 💰 Wallet
| Method | Endpoint | Description | Parameters |
|--------|----------|-------------|------------|
| GET | `/accounts/{accountId}/wallet/{symbol}/deposits` | Crypto deposits | `accountId`, `symbol`, `limit`, `page`, `from`, `to` |
| GET | `/accounts/{accountId}/wallet/{symbol}/deposits/addresses` | Deposit addresses | `accountId`, `symbol`, `network` |
| GET | `/accounts/{accountId}/wallet/fiat/{symbol}/deposits` | Fiat deposits | `accountId`, `symbol`, `limit`, `page`, `from`, `to` |
| POST | `/accounts/{accountId}/wallet/{symbol}/withdraw` | Request withdrawal | `accountId`, `symbol`, `quantity`, `address`, `network`, etc. |
| GET | `/accounts/{accountId}/wallet/{symbol}/withdraw` | List withdrawals | `accountId`, `symbol`, `page`, `page_size`, `from` |
| GET | `/accounts/{accountId}/wallet/withdraw/config/limits` | Withdrawal limits | `accountId`, `symbols` (query) |
| GET | `/accounts/{accountId}/wallet/withdraw/config/BRL` | BRL config | `accountId` |
| GET | `/accounts/{accountId}/wallet/withdraw/addresses` | Trusted addresses | `accountId` |
| GET | `/accounts/{accountId}/wallet/withdraw/bank-accounts` | Bank accounts | `accountId` |

---

## Error Handling
The API returns a JSON object with `code` and `message`.

### Error Code Pattern
`DOMAIN|MODULE|ERROR`
Example: `TRADING|GET_ORDER|ORDER_NOT_FOUND`

### Common HTTP Status Codes
- **400 Bad Request**: Invalid parameters or business logic error.
- **401 Unauthorized**: Missing or invalid authentication.
- **403 Forbidden**: Authenticated but lacks permission (e.g., read-only key).
- **429 Too Many Requests**: Rate limit exceeded.
- **500 Internal Server Error**: Unexpected server error.

---

## Rate Limit Strategy
1. **Client-Side Throttling**: Use `TokenBucketRateLimiter` to prevent 429 errors.
2. **Retry with Jitter**: Implement exponential backoff with random jitter for retries.
3. **Request Coalescing**: Deduplicate concurrent requests for the same public data.

# Summary

|||
|:---|:---|
| Generated on: | 06/01/2026 - 22:30:21 |
| Coverage date: | 06/01/2026 - 21:28:00 - 06/01/2026 - 22:29:53 |
| Parser: | MultiReport (2x Cobertura) |
| Assemblies: | 1 |
| Classes: | 174 |
| Files: | 173 |
| **Line coverage:** | 55.1% (9267 of 16811) |
| Covered lines: | 9267 |
| Uncovered lines: | 7544 |
| Coverable lines: | 16811 |
| Total lines: | 36310 |
| **Branch coverage:** | 38.5% (1705 of 4421) |
| Covered branches: | 1705 |
| Total branches: | 4421 |
| **Method coverage:** | [Feature is only available for sponsors](https://reportgenerator.io/pro) |

# Risk Hotspots

| **Assembly** | **Class** | **Method** | **Crap Score** | **Cyclomatic complexity** |
|:---|:---|:---|---:|---:|
| MercadoBitcoin.Client | MercadoBitcoin.Client.Trading.HighPerformanceOrderManager | ExecuteOrderAsync() | 1980 | 44 || MercadoBitcoin.Client | MercadoBitcoin.Client.Trading.OrderTracker | MapOrderStatus(...) | 1722 | 41 || MercadoBitcoin.Client | MercadoBitcoin.Client.Diagnostics.MercadoBitcoinHealthCheck | CheckHealthAsync() | 1332 | 36 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | OrderResponseSerializeHandler(...) | 1332 | 36 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinClient | GetResolutionSeconds(...) | 954 | 49 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | WithdrawSerializeHandler(...) | 930 | 30 || MercadoBitcoin.Client | MercadoBitcoin.Client.Generated.Client | WithdrawPOSTAsync() | 812 | 28 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | OrdersSerializeHandler(...) | 812 | 28 || MercadoBitcoin.Client | MercadoBitcoin.Client.Generated.Client | AuthorizeAsync() | 600 | 24 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | DepositSerializeHandler(...) | 600 | 24 || MercadoBitcoin.Client | MercadoBitcoin.Client.Trading.OrderTracker | OnPollingTimerCallback() | 600 | 24 || MercadoBitcoin.Client | MercadoBitcoin.Client.Extensions.CandleExtensions | GetIntervalInMilliseconds(...) | 544 | 43 || MercadoBitcoin.Client | MercadoBitcoin.Client.Internal.Converters.FastDecimalConverter | Read(...) | 506 | 22 || MercadoBitcoin.Client | MercadoBitcoin.Client.Internal.Security.ProactiveTokenRefresher | RefreshTokenInternalAsync() | 506 | 22 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | BankAccountSerializeHandler(...) | 506 | 22 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | ApiExceptionSerializeHandler(...) | 420 | 20 || MercadoBitcoin.Client | MercadoBitcoin.Client.Trading.Http3Detector | DetectAsync() | 420 | 20 || MercadoBitcoin.Client | MercadoBitcoin.Client.Internal.Optimization.ConnectionWarmUp | WarmUpAsync() | 342 | 18 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | ExecutionSerializeHandler(...) | 342 | 18 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | OrderBookDataSerializeHandler(...) | 342 | 18 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | PlaceOrderRequestSerializeHandler(...) | 342 | 18 || MercadoBitcoin.Client | MercadoBitcoin.Client.Trading.HighPerformanceMarketData | SubscribeOrderBookAsync() | 342 | 18 || MercadoBitcoin.Client | MercadoBitcoin.Client.WebSocket.MercadoBitcoinWebSocketClient | ProcessMessageAsync() | 327 | 86 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | ExceptionSerializeHandler(...) | 272 | 16 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | WithdrawCoinRequestSerializeHandler(...) | 272 | 16 || MercadoBitcoin.Client | MercadoBitcoin.Client.Trading.HighPerformanceOrderManager | CancelOrderFastAsync() | 272 | 16 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | System.Text.Json.Serialization.Metadata.IJsonTypeInfoResolver.GetTypeInfo(...) | 241 | 202 || MercadoBitcoin.Client | MercadoBitcoin.Client.Generated.OpenClient | ConvertToString(...) | 226 | 26 || MercadoBitcoin.Client | MercadoBitcoin.Client.Internal.Resilience.EndpointResiliencePipeline | BuildPipeline(...) | 210 | 14 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | AssetFeeSerializeHandler(...) | 210 | 14 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | CancelOpenOrdersResponseSerializeHandler(...) | 210 | 14 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | FiatDepositSerializeHandler(...) | 210 | 14 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | ListCandlesResponseSerializeHandler(...) | 210 | 14 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | PositionResponseSerializeHandler(...) | 210 | 14 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | SourceSerializeHandler(...) | 210 | 14 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | SubscriptionResponseSerializeHandler(...) | 210 | 14 || MercadoBitcoin.Client | MercadoBitcoin.Client.Trading.HighPerformanceMarketData | SubscribeTickerAsync() | 210 | 14 || MercadoBitcoin.Client | MercadoBitcoin.Client.Trading.HighPerformanceMarketData | SubscribeTradesAsync() | 210 | 14 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinClient | ExecuteCachedAsync() | 160 | 16 || MercadoBitcoin.Client | MercadoBitcoin.Client.Errors.MercadoBitcoinApiException | .ctor(...) | 156 | 12 || MercadoBitcoin.Client | MercadoBitcoin.Client.Internal.Optimization.BatchHelper | ResolveSymbolsAsync() | 156 | 12 || MercadoBitcoin.Client | MercadoBitcoin.Client.Internal.Optimization.ConnectionWarmUp | WarmUpEndpointAsync() | 156 | 12 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | AccountResponseSerializeHandler(...) | 156 | 12 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | BRLWithdrawConfigSerializeHandler(...) | 156 | 12 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | TradeResponseSerializeHandler(...) | 156 | 12 || MercadoBitcoin.Client | MercadoBitcoin.Client.Trading.HighPerformanceStrategy | RunAsync() | 156 | 12 || MercadoBitcoin.Client | MercadoBitcoin.Client.Trading.OrderTracker | UpdateStatus(...) | 156 | 12 || MercadoBitcoin.Client | MercadoBitcoin.Client.Trading.OrderTracker | MonitorOrderAsync() | 156 | 12 || MercadoBitcoin.Client | MercadoBitcoin.Client.Trading.SimpleMarketMakerStrategy | UpdateQuotesAsync() | 156 | 12 || MercadoBitcoin.Client | MercadoBitcoin.Client.Generated.Client | WithdrawGETAsync() | 141 | 28 || MercadoBitcoin.Client | MercadoBitcoin.Client.Generated.Client | TierAsync() | 132 | 24 || MercadoBitcoin.Client | MercadoBitcoin.Client.Internal.Security.AccountSessionManager | GetOrCreateSessionAsync() | 110 | 10 || MercadoBitcoin.Client | MercadoBitcoin.Client.Internal.Security.ProactiveTokenRefresher | ScheduleRefresh(...) | 110 | 10 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | CryptoBalanceResponseSerializeHandler(...) | 110 | 10 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | ExpandConverter(...) | 110 | 10 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | GetMarketFeesResponseSerializeHandler(...) | 110 | 10 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | OrderBookMessageSerializeHandler(...) | 110 | 10 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | TickerMessageSerializeHandler(...) | 110 | 10 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | TradeMessageSerializeHandler(...) | 110 | 10 || MercadoBitcoin.Client | MercadoBitcoin.Client.Trading.HighPerformanceMarketData | StartAsync() | 110 | 10 || MercadoBitcoin.Client | MercadoBitcoin.Client.Trading.HighPerformanceOrderManager | UpdateOrderStatus(...) | 110 | 10 || MercadoBitcoin.Client | MercadoBitcoin.Client.Trading.HighPerformanceOrderManager | CancelAllOrdersAsync() | 110 | 10 || MercadoBitcoin.Client | MercadoBitcoin.Client.Trading.HighPerformanceStrategy | .ctor(...) | 110 | 10 || MercadoBitcoin.Client | MercadoBitcoin.Client.Trading.OrderTracker | RefreshOrderAsync() | 110 | 10 || MercadoBitcoin.Client | MercadoBitcoin.Client.Trading.PerformanceMonitor | OnReportingTimerCallback(...) | 110 | 10 || MercadoBitcoin.Client | MercadoBitcoin.Client.WebSocket.MercadoBitcoinWebSocketClient | HandleDisconnectionAsync() | 106 | 14 || MercadoBitcoin.Client | MercadoBitcoin.Client.Diagnostics.MercadoBitcoinTelemetry | SetActivityResult(...) | 72 | 8 || MercadoBitcoin.Client | MercadoBitcoin.Client.Generated.Client | ReadObjectResponseAsync() | 72 | 8 || MercadoBitcoin.Client | MercadoBitcoin.Client.Generated.OpenClient | ReadObjectResponseAsync() | 72 | 8 || MercadoBitcoin.Client | MercadoBitcoin.Client.Internal.Optimization.BatchHelper | NormalizeAndValidateSymbols(...) | 72 | 8 || MercadoBitcoin.Client | MercadoBitcoin.Client.Internal.Optimization.BatchHelper | ExecuteParallelFanOutAsync() | 72 | 8 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | AddressesSerializeHandler(...) | 72 | 8 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | WebSocketMessageBaseSerializeHandler(...) | 72 | 8 || MercadoBitcoin.Client | MercadoBitcoin.Client.Trading.HighPerformanceOrderManager | .ctor(...) | 72 | 8 || MercadoBitcoin.Client | MercadoBitcoin.Client.Trading.HighPerformanceOrderManager | PlaceOrderInternalAsync() | 72 | 8 || MercadoBitcoin.Client | MercadoBitcoin.Client.Trading.OrderTracker | Track(...) | 72 | 8 || MercadoBitcoin.Client | MercadoBitcoin.Client.Trading.OrderTrackerExtensions | WaitForCompletionAsync() | 72 | 8 || MercadoBitcoin.Client | Microsoft.Extensions.DependencyInjection.MercadoBitcoinServiceCollectionExtensions | AddMercadoBitcoinHighPerformanceTrading(...) | 72 | 8 || MercadoBitcoin.Client | MercadoBitcoin.Client.Generated.Client | ConvertToString(...) | 60 | 26 || MercadoBitcoin.Client | MercadoBitcoin.Client.WebSocket.MercadoBitcoinWebSocketClient | PingLoopAsync() | 60 | 14 || MercadoBitcoin.Client | MercadoBitcoin.Client.WebSocket.MercadoBitcoinWebSocketClient | DisposeAsync() | 54 | 24 || MercadoBitcoin.Client | MercadoBitcoin.Client.Generated.Client | OrdersAllAsync() | 53 | 44 || MercadoBitcoin.Client | MercadoBitcoin.Client.Extensions.CandleExtensions | NormalizeSymbol(...) | 44 | 8 || MercadoBitcoin.Client | MercadoBitcoin.Client.Diagnostics.MercadoBitcoinTelemetry | StartTradingActivity(...) | 42 | 6 || MercadoBitcoin.Client | MercadoBitcoin.Client.Errors.MercadoBitcoinApiException | get_Error() | 42 | 6 || MercadoBitcoin.Client | MercadoBitcoin.Client.Http.RetryPolicyConfig | CalculateDelay(...) | 42 | 6 || MercadoBitcoin.Client | MercadoBitcoin.Client.Internal.Caching.AdvancedCacheManager | GetOrComputeAsync() | 42 | 6 || MercadoBitcoin.Client | MercadoBitcoin.Client.Internal.Optimization.BatchHelper | ExecuteNativeBatchAsync() | 42 | 6 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | AuthorizeResponseSerializeHandler(...) | 42 | 6 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | CryptoWalletAddressSerializeHandler(...) | 42 | 6 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | DepositAddressesSerializeHandler(...) | 42 | 6 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | ErrorResponseSerializeHandler(...) | 42 | 6 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | FeesSerializeHandler(...) | 42 | 6 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | ListSymbolInfoResponseSerializeHandler(...) | 42 | 42 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | NetworkSerializeHandler(...) | 42 | 6 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | PingRequestSerializeHandler(...) | 42 | 6 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | QrcodeSerializeHandler(...) | 42 | 6 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | WebSocketSubscriptionRequestSerializeHandler(...) | 42 | 6 || MercadoBitcoin.Client | MercadoBitcoin.Client.Trading.HighPerformanceStrategy | CancelAsync() | 42 | 6 || MercadoBitcoin.Client | MercadoBitcoin.Client.Trading.HighPerformanceStrategy | PlaceBuyAsync() | 42 | 6 || MercadoBitcoin.Client | MercadoBitcoin.Client.Trading.HighPerformanceStrategy | PlaceMarketBuyAsync() | 42 | 6 || MercadoBitcoin.Client | MercadoBitcoin.Client.Trading.HighPerformanceStrategy | PlaceMarketSellAsync() | 42 | 6 || MercadoBitcoin.Client | MercadoBitcoin.Client.Trading.HighPerformanceStrategy | PlaceSellAsync() | 42 | 6 || MercadoBitcoin.Client | MercadoBitcoin.Client.Trading.HighPerformanceStrategy | StopAsync() | 42 | 6 || MercadoBitcoin.Client | MercadoBitcoin.Client.Trading.OrderTracker | Untrack(...) | 42 | 6 || MercadoBitcoin.Client | MercadoBitcoin.Client.Trading.OrderTracker | Dispose() | 42 | 6 || MercadoBitcoin.Client | MercadoBitcoin.Client.Trading.OrderTracker | .ctor(...) | 42 | 6 || MercadoBitcoin.Client | MercadoBitcoin.Client.Trading.OrderTracker | DisposeAsync() | 42 | 6 || MercadoBitcoin.Client | MercadoBitcoin.Client.Trading.OrderTracker | RefreshAllOrdersAsync() | 42 | 6 || MercadoBitcoin.Client | MercadoBitcoin.Client.Trading.SimpleMarketMakerStrategy | OnOrderFilled(...) | 42 | 6 || MercadoBitcoin.Client | Microsoft.Extensions.DependencyInjection.MercadoBitcoinServiceCollectionExtensions | AddMercadoBitcoinOrderBook(...) | 42 | 6 || MercadoBitcoin.Client | MercadoBitcoin.Client.Http.RetryHandler | ClassifyOutcome(...) | 40 | 26 || MercadoBitcoin.Client | MercadoBitcoin.Client.Generated.Client | Deposits2Async() | 39 | 34 || MercadoBitcoin.Client | MercadoBitcoin.Client.Generated.Client | DepositsAsync() | 39 | 34 || MercadoBitcoin.Client | MercadoBitcoin.Client.WebSocket.MercadoBitcoinWebSocketClient | ResubscribeAsync() | 38 | 8 || MercadoBitcoin.Client | MercadoBitcoin.Client.Generated.Client | OrdersGET2Async() | 36 | 32 || MercadoBitcoin.Client | MercadoBitcoin.Client.Generated.Client | TradesAsync() | 35 | 34 || MercadoBitcoin.Client | MercadoBitcoin.Client.Generated.Client | WithdrawAllAsync() | 35 | 32 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinClient | StreamCandlesAsync() | 35 | 26 || MercadoBitcoin.Client | MercadoBitcoin.Client.Generated.Client | OrdersDELETEAsync() | 33 | 30 || MercadoBitcoin.Client | MercadoBitcoin.Client.Generated.Client | CandlesAsync() | 32 | 30 || MercadoBitcoin.Client | MercadoBitcoin.Client.Generated.Client | OrdersGETAsync() | 32 | 28 || MercadoBitcoin.Client | MercadoBitcoin.Client.Generated.Client | OrdersPOSTAsync() | 31 | 28 || MercadoBitcoin.Client | MercadoBitcoin.Client.Generated.Client | AddressesAsync() | 30 | 28 || MercadoBitcoin.Client | MercadoBitcoin.Client.Generated.OpenClient | OrdersAsync() | 29 | 28 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinClient | GetSymbolsAsync() | 28 | 28 || MercadoBitcoin.Client | MercadoBitcoin.Client.Generated.Client | Fees2Async() | 28 | 26 || MercadoBitcoin.Client | MercadoBitcoin.Client.Generated.Client | FeesAsync() | 28 | 26 || MercadoBitcoin.Client | MercadoBitcoin.Client.Generated.Client | LimitsAsync() | 28 | 26 || MercadoBitcoin.Client | MercadoBitcoin.Client.Generated.Client | OrderbookAsync() | 28 | 26 || MercadoBitcoin.Client | MercadoBitcoin.Client.Generated.Client | PositionsAsync() | 27 | 26 || MercadoBitcoin.Client | MercadoBitcoin.Client.Generated.Client | AddressesAllAsync() | 26 | 24 || MercadoBitcoin.Client | MercadoBitcoin.Client.Generated.Client | BRLAsync() | 26 | 24 || MercadoBitcoin.Client | MercadoBitcoin.Client.Generated.Client | BalancesAsync() | 26 | 24 || MercadoBitcoin.Client | MercadoBitcoin.Client.Generated.Client | BankAccountsAsync() | 26 | 24 || MercadoBitcoin.Client | MercadoBitcoin.Client.Generated.Client | NetworksAsync() | 26 | 24 || MercadoBitcoin.Client | MercadoBitcoin.Client.Generated.Client | SymbolsAsync() | 25 | 24 || MercadoBitcoin.Client | MercadoBitcoin.Client.Generated.Client | TickersAsync() | 26 | 24 || MercadoBitcoin.Client | MercadoBitcoin.Client.Generated.Client | AccountsAsync() | 23 | 22 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinClient | StreamTradesAsync() | 29 | 22 || MercadoBitcoin.Client | MercadoBitcoin.Client.AuthHttpClient | SendAsync() | 20 | 20 || MercadoBitcoin.Client | MercadoBitcoin.Client.Http.AuthenticationHandler | SendAsync() | 20 | 20 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | TickerResponseSerializeHandler(...) | 20 | 20 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinClient | StreamOrdersAsync() | 27 | 18 || MercadoBitcoin.Client | MercadoBitcoin.Client.Internal.Optimization.ResiliencePipelineProvider | ShouldRetry(...) | 16 | 16 || MercadoBitcoin.Client | MercadoBitcoin.Client.MercadoBitcoinClient | GetAllSymbolsAsync() | 16 | 16 || MercadoBitcoin.Client | MercadoBitcoin.Client.WebSocket.MercadoBitcoinWebSocketClient | ReceiveLoopAsync() | 18 | 16 |
# Coverage

| **Name** | **Covered** | **Uncovered** | **Coverable** | **Total** | **Line coverage** | **Covered** | **Total** | **Branch coverage** |
|:---|---:|---:|---:|---:|---:|---:|---:|---:|
| **MercadoBitcoin.Client** | **9267** | **7544** | **16811** | **287972** | **55.1%** | **1705** | **4421** | **38.5%** |
| MercadoBitcoin.Client.AuthHttpClient | 35 | 4 | 39 | 125 | 89.7% | 27 | 40 | 67.5% |
| MercadoBitcoin.Client.Configuration.CacheConfig | 5 | 0 | 5 | 190 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Configuration.MercadoBitcoinClientOptions | 30 | 0 | 30 | 190 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Configuration.RateLimiterConfig | 5 | 0 | 5 | 190 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Diagnostics.ActivityExtensions | 0 | 19 | 19 | 583 | 0% | 0 | 20 | 0% |
| MercadoBitcoin.Client.Diagnostics.MercadoBitcoinHealthCheck | 0 | 153 | 153 | 389 | 0% | 0 | 52 | 0% |
| MercadoBitcoin.Client.Diagnostics.MercadoBitcoinHealthCheckExtensions | 0 | 40 | 40 | 389 | 0% | 0 | 8 | 0% |
| MercadoBitcoin.Client.Diagnostics.MercadoBitcoinHealthCheckOptions | 0 | 5 | 5 | 389 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Diagnostics.MercadoBitcoinTelemetry | 0 | 180 | 180 | 583 | 0% | 0 | 32 | 0% |
| MercadoBitcoin.Client.Errors.ErrorResponse | 2 | 3 | 5 | 17 | 40% | 0 | 0 |  |
| MercadoBitcoin.Client.Errors.MercadoBitcoinApiException | 9 | 26 | 35 | 98 | 25.7% | 8 | 34 | 23.5% |
| MercadoBitcoin.Client.Errors.MercadoBitcoinException | 12 | 0 | 12 | 28 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Errors.MercadoBitcoinRateLimitException | 0 | 3 | 3 | 98 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Errors.MercadoBitcoinUnauthorizedException | 0 | 3 | 3 | 98 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Errors.MercadoBitcoinValidationException | 0 | 3 | 3 | 98 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Extensions.CandleExtensions | 79 | 43 | 122 | 243 | 64.7% | 21 | 77 | 27.2% |
| MercadoBitcoin.Client.Extensions.MercadoBitcoinClientExtensions | 16 | 31 | 47 | 114 | 34% | 2 | 10 | 20% |
| MercadoBitcoin.Client.Generated.AccountResponse | 5 | 0 | 5 | 5290 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.Addresses | 3 | 0 | 3 | 5290 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.ApiException | 8 | 1 | 9 | 5290 | 88.8% | 2 | 4 | 50% |
| MercadoBitcoin.Client.Generated.ApiException<T> | 0 | 4 | 4 | 5290 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.AssetFee | 6 | 0 | 6 | 5290 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.AuthorizeRequest | 2 | 0 | 2 | 5290 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.AuthorizeResponse | 2 | 0 | 2 | 5290 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.BankAccount | 0 | 10 | 10 | 5290 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.BRLWithdrawConfig | 5 | 0 | 5 | 5290 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.CancelOpenOrdersResponse | 6 | 0 | 6 | 5290 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.CancelOrderResponse | 1 | 0 | 1 | 5290 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.Client | 909 | 336 | 1245 | 5361 | 73% | 539 | 818 | 65.8% |
| MercadoBitcoin.Client.Generated.Config | 1 | 0 | 1 | 5290 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.CryptoBalanceResponse | 4 | 0 | 4 | 5290 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.CryptoWalletAddress | 0 | 2 | 2 | 5290 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.Deposit | 10 | 1 | 11 | 5290 | 90.9% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.DepositAddresses | 2 | 0 | 2 | 5290 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.Execution | 8 | 0 | 8 | 5290 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.Extra | 1 | 0 | 1 | 5290 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.Fees | 2 | 0 | 2 | 5290 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.FiatDeposit | 6 | 0 | 6 | 5290 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.GetMarketFeesResponse | 4 | 0 | 4 | 5290 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.GetTierResponse | 0 | 1 | 1 | 5290 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.ListAllOrdersResponse | 1 | 0 | 1 | 5290 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.ListCandlesResponse | 6 | 0 | 6 | 5290 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.ListSymbolInfoResponse | 20 | 0 | 20 | 5290 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.Network | 2 | 0 | 2 | 5290 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.OpenClient | 60 | 56 | 116 | 5361 | 51.7% | 36 | 68 | 52.9% |
| MercadoBitcoin.Client.Generated.OrderBookResponse | 3 | 0 | 3 | 5290 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.OrderResponse | 17 | 0 | 17 | 5290 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.Orders | 11 | 2 | 13 | 5290 | 84.6% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.PlaceOrderRequest | 8 | 0 | 8 | 5290 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.PlaceOrderResponse | 1 | 0 | 1 | 5290 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.PositionResponse | 6 | 0 | 6 | 5290 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.Qrcode | 2 | 0 | 2 | 5290 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.Response | 2 | 0 | 2 | 5290 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.Source | 4 | 2 | 6 | 5290 | 66.6% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.TickerResponse | 9 | 0 | 9 | 5290 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.TradeResponse | 5 | 0 | 5 | 5290 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.Withdraw | 12 | 2 | 14 | 5290 | 85.7% | 0 | 0 |  |
| MercadoBitcoin.Client.Generated.WithdrawCoinRequest | 0 | 7 | 7 | 5290 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Http.AuthenticationHandler | 52 | 8 | 60 | 141 | 86.6% | 31 | 40 | 77.5% |
| MercadoBitcoin.Client.Http.CircuitBreakerEvent | 1 | 0 | 1 | 148 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Http.ClientSideRateLimitException | 0 | 1 | 1 | 64 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Http.HttpClientConfiguration | 0 | 69 | 69 | 128 | 0% | 0 | 8 | 0% |
| MercadoBitcoin.Client.Http.HttpConfiguration | 53 | 30 | 83 | 165 | 63.8% | 1 | 2 | 50% |
| MercadoBitcoin.Client.Http.RateLimitingHandler | 13 | 8 | 21 | 64 | 61.9% | 1 | 6 | 16.6% |
| MercadoBitcoin.Client.Http.RetryEvent | 0 | 1 | 1 | 148 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Http.RetryHandler | 72 | 8 | 80 | 169 | 90% | 34 | 48 | 70.8% |
| MercadoBitcoin.Client.Http.RetryPolicyConfig | 17 | 14 | 31 | 148 | 54.8% | 0 | 6 | 0% |
| MercadoBitcoin.Client.Internal.BatchHelper | 0 | 47 | 47 | 113 | 0% | 0 | 10 | 0% |
| MercadoBitcoin.Client.Internal.Caching.AdvancedCacheManager | 0 | 91 | 91 | 242 | 0% | 0 | 24 | 0% |
| MercadoBitcoin.Client.Internal.Caching.MicroCache | 0 | 87 | 87 | 313 | 0% | 0 | 26 | 0% |
| MercadoBitcoin.Client.Internal.Caching.MicroCacheConfiguration | 0 | 8 | 8 | 313 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Internal.Caching.MicroCacheStatistics | 0 | 5 | 5 | 313 | 0% | 0 | 2 | 0% |
| MercadoBitcoin.Client.Internal.Converters.FastDecimalConverter | 0 | 36 | 36 | 103 | 0% | 0 | 22 | 0% |
| MercadoBitcoin.Client.Internal.Diagnostics.MetricsCollector | 0 | 91 | 91 | 197 | 0% | 0 | 18 | 0% |
| MercadoBitcoin.Client.Internal.Helpers.AsyncPaginationHelper | 11 | 1 | 12 | 42 | 91.6% | 9 | 14 | 64.2% |
| MercadoBitcoin.Client.Internal.Helpers.BatchHelper | 71 | 9 | 80 | 182 | 88.7% | 28 | 38 | 73.6% |
| MercadoBitcoin.Client.Internal.Helpers.JsonHelper | 0 | 8 | 8 | 34 | 0% | 0 | 6 | 0% |
| MercadoBitcoin.Client.Internal.JsonOptionsCache | 0 | 6 | 6 | 18 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Internal.Optimization.BatchHelper | 0 | 110 | 110 | 296 | 0% | 0 | 42 | 0% |
| MercadoBitcoin.Client.Internal.Optimization.ConnectionWarmUp | 0 | 134 | 134 | 449 | 0% | 0 | 46 | 0% |
| MercadoBitcoin.Client.Internal.Optimization.ConnectionWarmUpExtensions | 0 | 24 | 24 | 449 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Internal.Optimization.ConnectionWarmUpOptions | 0 | 9 | 9 | 449 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Internal.Optimization.OptimizedGeneratedClient | 17 | 4 | 21 | 110 | 80.9% | 5 | 8 | 62.5% |
| MercadoBitcoin.Client.Internal.Optimization.OptimizedOpenClient | 17 | 4 | 21 | 110 | 80.9% | 5 | 8 | 62.5% |
| MercadoBitcoin.Client.Internal.Optimization.RequestCoalescer | 26 | 7 | 33 | 117 | 78.7% | 7 | 8 | 87.5% |
| MercadoBitcoin.Client.Internal.Optimization.ResiliencePipelineProvider | 85 | 10 | 95 | 204 | 89.4% | 27 | 40 | 67.5% |
| MercadoBitcoin.Client.Internal.Optimization.WarmUpCompletedEventArgs | 0 | 4 | 4 | 449 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Internal.Optimization.WarmUpEndpointResult | 0 | 5 | 5 | 449 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Internal.Optimization.WarmUpFailedEventArgs | 0 | 4 | 4 | 449 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Internal.RateLimiting.PerAccountRateLimiter | 0 | 32 | 32 | 95 | 0% | 0 | 14 | 0% |
| MercadoBitcoin.Client.Internal.Resilience.EndpointResiliencePipeline | 0 | 65 | 65 | 191 | 0% | 0 | 24 | 0% |
| MercadoBitcoin.Client.Internal.Security.AccountSessionManager | 0 | 74 | 74 | 228 | 0% | 0 | 26 | 0% |
| MercadoBitcoin.Client.Internal.Security.DefaultMercadoBitcoinCredentialProvider | 6 | 0 | 6 | 32 | 100% | 6 | 8 | 75% |
| MercadoBitcoin.Client.Internal.Security.ProactiveTokenRefresher | 0 | 113 | 113 | 360 | 0% | 0 | 58 | 0% |
| MercadoBitcoin.Client.Internal.Security.ProactiveTokenRefreshOptions | 0 | 5 | 5 | 360 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Internal.Security.TokenRefreshedEventArgs | 0 | 3 | 3 | 360 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Internal.Security.TokenRefreshFailedEventArgs | 0 | 4 | 4 | 360 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Internal.Security.TokenStore | 2 | 0 | 2 | 18 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Internal.Time.ServerTimeEstimator | 16 | 12 | 28 | 83 | 57.1% | 2 | 8 | 25% |
| MercadoBitcoin.Client.MercadoBitcoinClient | 565 | 181 | 746 | 1684 | 75.7% | 137 | 255 | 53.7% |
| MercadoBitcoin.Client.MercadoBitcoinJsonSerializerContext | 5891 | 3848 | 9739 | 15594 | 60.4% | 475 | 1368 | 34.7% |
| MercadoBitcoin.Client.Models.CandleData | 10 | 11 | 21 | 62 | 47.6% | 0 | 0 |  |
| MercadoBitcoin.Client.Models.MercadoBitcoinCredentials | 1 | 0 | 1 | 42 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Models.StaticCredentialProvider | 0 | 5 | 5 | 42 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Models.UniversalFilter | 0 | 9 | 9 | 57 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Models.ValueTypes.SymbolSpan | 0 | 9 | 9 | 25 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.HighPerformanceMarketData | 0 | 199 | 199 | 655 | 0% | 0 | 78 | 0% |
| MercadoBitcoin.Client.Trading.HighPerformanceOrderManager | 0 | 193 | 193 | 723 | 0% | 0 | 106 | 0% |
| MercadoBitcoin.Client.Trading.HighPerformanceStrategy | 0 | 151 | 151 | 762 | 0% | 0 | 80 | 0% |
| MercadoBitcoin.Client.Trading.Http3DetectionStatus | 6 | 0 | 6 | 381 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.Http3Detector | 40 | 65 | 105 | 381 | 38% | 12 | 48 | 25% |
| MercadoBitcoin.Client.Trading.Http3DetectorExtensions | 3 | 4 | 7 | 381 | 42.8% | 0 | 2 | 0% |
| MercadoBitcoin.Client.Trading.Http3DetectorOptions | 5 | 0 | 5 | 381 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.Http3StatusChangedEventArgs | 3 | 0 | 3 | 381 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.IncrementalOrderBook | 236 | 2 | 238 | 884 | 99.1% | 76 | 94 | 80.8% |
| MercadoBitcoin.Client.Trading.IncrementalOrderBookExtensions | 7 | 0 | 7 | 884 | 100% | 2 | 2 | 100% |
| MercadoBitcoin.Client.Trading.IncrementalOrderBookOptions | 2 | 1 | 3 | 884 | 66.6% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.LatencyThresholdExceededEventArgs | 4 | 0 | 4 | 689 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.MeasurementScope | 7 | 0 | 7 | 689 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.OperationMetrics | 38 | 12 | 50 | 689 | 76% | 13 | 18 | 72.2% |
| MercadoBitcoin.Client.Trading.OperationStats | 10 | 0 | 10 | 689 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.OrderBookDelta | 3 | 0 | 3 | 884 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.OrderBookSnapshot | 0 | 10 | 10 | 655 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.OrderBookState | 11 | 0 | 11 | 884 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.OrderBookUpdate | 0 | 2 | 2 | 655 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.OrderBookUpdatedEventArgs | 6 | 0 | 6 | 884 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.OrderCancelledEventArgs | 0 | 3 | 3 | 723 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.OrderFailedEventArgs | 0 | 5 | 5 | 723 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.OrderFilledEventArgs | 0 | 5 | 5 | 1078 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.OrderPlacedEventArgs | 0 | 6 | 6 | 723 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.OrderStatusChangedEventArgs | 0 | 6 | 6 | 1078 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.OrderTracker | 0 | 251 | 251 | 1078 | 0% | 0 | 159 | 0% |
| MercadoBitcoin.Client.Trading.OrderTrackerCancelledEventArgs | 0 | 4 | 4 | 1078 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.OrderTrackerExtensions | 0 | 17 | 17 | 1078 | 0% | 0 | 12 | 0% |
| MercadoBitcoin.Client.Trading.OrderTrackerOptions | 0 | 7 | 7 | 1078 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.OrderTrackerStats | 0 | 7 | 7 | 1078 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.OrderTrackingErrorEventArgs | 0 | 4 | 4 | 1078 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.PerformanceMonitor | 90 | 23 | 113 | 689 | 79.6% | 19 | 36 | 52.7% |
| MercadoBitcoin.Client.Trading.PerformanceMonitorExtensions | 19 | 0 | 19 | 689 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.PerformanceMonitorOptions | 5 | 0 | 5 | 689 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.PerformanceReport | 5 | 0 | 5 | 689 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.PerformanceReportEventArgs | 0 | 1 | 1 | 689 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.RateLimitBudget | 112 | 34 | 146 | 482 | 76.7% | 40 | 64 | 62.5% |
| MercadoBitcoin.Client.Trading.RateLimitHitEventArgs | 3 | 0 | 3 | 482 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.RateLimitStatus | 9 | 0 | 9 | 482 | 100% | 1 | 2 | 50% |
| MercadoBitcoin.Client.Trading.RateLimitWarningEventArgs | 0 | 3 | 3 | 482 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.SimpleMarketMakerStrategy | 0 | 36 | 36 | 762 | 0% | 0 | 22 | 0% |
| MercadoBitcoin.Client.Trading.SpreadChangedEventArgs | 5 | 0 | 5 | 884 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.StatusChange | 0 | 3 | 3 | 1078 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.StrategyOptions | 0 | 4 | 4 | 762 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.StrategyStats | 0 | 12 | 12 | 762 | 0% | 0 | 4 | 0% |
| MercadoBitcoin.Client.Trading.TickerSnapshot | 0 | 12 | 12 | 655 | 0% | 0 | 2 | 0% |
| MercadoBitcoin.Client.Trading.TickerUpdate | 0 | 2 | 2 | 655 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.TrackedOrder | 0 | 11 | 11 | 723 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.TrackedOrderInfo | 0 | 11 | 11 | 1078 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.TradeSnapshot | 0 | 8 | 8 | 655 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.Trading.TradeUpdate | 0 | 2 | 2 | 655 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.WebSocket.MercadoBitcoinWebSocketClient | 236 | 95 | 331 | 750 | 71.2% | 114 | 226 | 50.4% |
| MercadoBitcoin.Client.WebSocket.Messages.OrderBookData | 2 | 10 | 12 | 67 | 16.6% | 0 | 24 | 0% |
| MercadoBitcoin.Client.WebSocket.Messages.OrderBookMessage | 1 | 0 | 1 | 67 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.WebSocket.Messages.PingRequest | 0 | 2 | 2 | 21 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.WebSocket.Messages.SubscriptionDetails | 3 | 0 | 3 | 78 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.WebSocket.Messages.SubscriptionRequest | 2 | 0 | 2 | 78 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.WebSocket.Messages.SubscriptionResponse | 0 | 5 | 5 | 78 | 0% | 0 | 2 | 0% |
| MercadoBitcoin.Client.WebSocket.Messages.TickerData | 7 | 2 | 9 | 73 | 77.7% | 0 | 2 | 0% |
| MercadoBitcoin.Client.WebSocket.Messages.TickerMessage | 1 | 0 | 1 | 73 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.WebSocket.Messages.TradeData | 5 | 3 | 8 | 66 | 62.5% | 0 | 0 |  |
| MercadoBitcoin.Client.WebSocket.Messages.TradeMessage | 1 | 0 | 1 | 66 | 100% | 0 | 0 |  |
| MercadoBitcoin.Client.WebSocket.Messages.WebSocketMessageBase | 7 | 0 | 7 | 51 | 100% | 2 | 4 | 50% |
| MercadoBitcoin.Client.WebSocket.Messages.WebSocketSubscriptionRequest | 0 | 2 | 2 | 15 | 0% | 0 | 0 |  |
| MercadoBitcoin.Client.WebSocket.WebSocketClientOptions | 28 | 0 | 28 | 107 | 100% | 17 | 18 | 94.4% |
| MercadoBitcoin.Client.WebSocket.WebSocketSubscription | 0 | 12 | 12 | 39 | 0% | 0 | 2 | 0% |
| Microsoft.Extensions.DependencyInjection.HighPerformanceTradingOptions | 0 | 6 | 6 | 535 | 0% | 0 | 0 |  |
| Microsoft.Extensions.DependencyInjection.MercadoBitcoinServiceCollectionExtensions | 72 | 140 | 212 | 535 | 33.9% | 6 | 68 | 8.8% |


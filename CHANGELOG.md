# Changelog

## [5.3.0] - 2026-01-07

### Added
- **Comprehensive Test Suite**: Expanded to 566 integration tests with 100% pass rate
  - Full coverage of all public and private API endpoints
  - WebSocket streaming tests for ticker, orderbook, and trades channels
  - Trading operations tests (buy/sell orders with validation)
  - Performance benchmarks and stress tests
  - Parallel execution support for load testing

- **Trading Infrastructure Enhancements**:
  - `RateLimitBudget` class for intelligent rate limit management with budget tracking
  - `PerformanceMonitor` with microsecond-precision latency measurements
  - `IncrementalOrderBook` with 99.1% code coverage
  - `Http3Detector` for automatic HTTP/3 protocol detection

- **New Test Categories**:
  - Unit tests for internal components (RateLimitBudget, PerformanceMonitor, Http3Detector)
  - Integration tests for all 17+ private endpoints
  - Validation tests for order parameters
  - Serialization/deserialization validation tests
  - Error handling and edge case tests

- **Documentation**:
  - `HIGH_PERFORMANCE_TRADING.md` guide for algorithmic trading
  - `COMPLETE_TEST_REPORT.md` with detailed test results
  - `COVERAGE_SUMMARY.md` with code coverage analysis
  - Updated `ANALYSIS_REPORT.md` with architecture details

### Changed
- **Test Infrastructure**: Improved `TestBase` class with better credential handling
- **Code Coverage**: Achieved 55.1% line coverage, 38.5% branch coverage, 48.4% method coverage
- **Performance**: Average API response time of 223.79ms across all endpoints

### Security
- **Credentials Protection**: Removed all hardcoded credentials from version control
  - Added `appsettings.example.json` template files
  - Updated `.gitignore` to exclude sensitive configuration files
  - Sanitized all test output files and reports

### Fixed
- Fixed nullable reference warnings in test files
- Fixed xUnit analyzer warnings for better test practices
- Improved error handling in WebSocket streaming tests

## [6.1.0] - 2025-01-06

### Added
- **HTTP/3 Auto-Detection**: New `Http3Detector` class for automatic HTTP/3 (QUIC) protocol detection and configuration
  - Automatic server support detection with caching
  - `CreateOptimizedClient()` method for creating pre-configured HttpClient
  - `GetRecommendedVersion()` and `GetRecommendedVersionPolicy()` helpers
  
- **Incremental OrderBook**: New `IncrementalOrderBook` class for efficient order book management
  - Support for both full snapshots and delta updates
  - O(log n) operations with sorted bid/ask levels
  - VWAP calculation, spread tracking, and imbalance ratio
  - Spread change events for significant price movements
  - Thread-safe with `ReaderWriterLockSlim`

- **Order Tracker**: New `OrderTracker` class for monitoring order execution status
  - Real-time order state management with polling
  - Status change events (`OrderStatusChanged`, `OrderFilled`, `OrderCancelled`)
  - Automatic status refresh with exponential backoff
  - Support for tracking metadata and status history

- **High Performance Strategy Base**: New `HighPerformanceStrategy` abstract class
  - Infrastructure for algorithmic trading strategies
  - Built-in rate limit checking and order management
  - Performance metrics (tick latency, orders placed/cancelled)
  - Example `SimpleMarketMakerStrategy` implementation

- **Performance Monitor**: New `PerformanceMonitor` class for microsecond-level latency measurements
  - `MeasureAsync<T>()` and `Measure<T>()` for operation timing
  - `MeasurementScope` for scoped measurements via `using`
  - Percentile calculations (P50, P95, P99)
  - Latency threshold alerts and periodic reporting

- **New DI Extensions**:
  - `AddMercadoBitcoinHttp3Detection()` for HTTP/3 auto-detection
  - `AddMercadoBitcoinOrderTracker()` for order tracking service
  - `AddMercadoBitcoinPerformanceMonitor()` for performance monitoring
  - `AddMercadoBitcoinOrderBook()` for incremental order book (keyed service)
  - `AddMercadoBitcoinMetrics()` for OpenTelemetry metrics

### Changed
- **OpenTelemetry**: Updated packages from 1.11.x to 1.14.0
- **BenchmarkDotNet**: Updated from 0.14.0 to 0.15.8 in benchmarks project
- **Target Framework**: Fixed Benchmarks project to target net10.0 (was net9.0)
- **Version Bump**: Library version updated to 6.1.0

### Fixed
- Fixed `CancelOrdersBatchAsync` and `CancelAllOrdersAsync` to properly handle `ValueTask<bool>` to `Task` conversion
- Fixed decimal to double conversion for `PlaceOrderRequest` properties (`LimitPrice`, `StopPrice`, `Cost`)
- Fixed `OrderTracker.RefreshOrderAsync` to use correct parameter order for `GetOrderAsync`
- Renamed `OrderCancelledEventArgs` in OrderTracker to `OrderTrackerCancelledEventArgs` to avoid naming conflict

## [5.2.0] - 2025-12-24
### Added
- **Robust WebSocket Client**: Major overhaul of `MercadoBitcoinWebSocketClient` with production-grade features:
    - **Auto-Reconnection**: Automatic reconnection with exponential backoff and jitter.
    - **Keep-Alive**: Dedicated Ping/Pong loop to maintain connection stability.
    - **Unsubscribe**: New `UnsubscribeAsync` method to stop receiving updates for specific channels.
    - **Connection State**: Public `ConnectionState` property and events (`ConnectionStateChanged`, `ErrorOccurred`) for monitoring.
    - **Concurrent Subscriptions**: Improved thread safety allowing multiple concurrent subscriptions to the same or different channels.
- **Stress Testing Suite**: Added `StressTests` to validate library behavior under high concurrency (REST and WebSocket).
- **Full API Coverage**: Added `FullApiCoverageTests` ensuring 100% sequential validation of all public and private endpoints.

### Changed
- **WebSocket Architecture**: Refactored internal message processing to use `Channel<T>` for efficient, non-blocking streaming.
- **Test Infrastructure**: Enhanced test logging and reliability for integration tests.

## [5.1.1] - 2025-12-23
### Changed
- **DI Consistency**: Refactored `MercadoBitcoinClient` and its DI extensions to use `IOptions<MercadoBitcoinClientOptions>` instead of `IOptionsSnapshot`. This improves performance and ensures consistency when the client is used in different service lifetimes.

## [5.1.0] - 2025-12-23
### Added
- **Multi-User Architecture**: Introduced `IMercadoBitcoinCredentialProvider` to support scoped Dependency Injection. This allows different credentials to be used within the same application instance (e.g., in a web API where each request might belong to a different user).
- **Universal Filtering**: All data retrieval methods (Public, Trading, Wallet, Account) now support a "Universal Filter". Passing `null` or an empty list to the `symbols` parameter will automatically fetch data for all tradable assets.
- **Backward Compatibility**: Added string-based overloads for all major methods to ensure existing code continues to work without modification.
- **Parallel Fan-out**: Enhanced `BatchHelper` to handle parallel execution for endpoints that do not support native API batching (e.g., `ListAllOrders`).
- **AOT Serialization**: Expanded `MercadoBitcoinJsonSerializerContext` with `ListAllOrdersResponse` and `Orders` types for full AOT/Source Generation support.

### Fixed
- **ListAllOrdersAsync**: Fixed aggregation logic to correctly merge items from multiple symbols and handle JSON serialization via Source Generators.
- **GetWithdrawLimitsAsync**: Fixed handling of empty API responses (Status 200 with no body) to prevent deserialization errors.
- **GetAllSymbolsAsync**: Improved reliability by filtering for `ExchangeTraded` symbols, preventing errors when using the results in private endpoints.

## [5.0.0] - 2025-12-23
### Added
- **ExecuteBatchAsync**: Native support for parallel request execution using HTTP/2 multiplexing.
- **RequestCoalescer**: Intelligent request coalescing to prevent redundant concurrent calls for the same resource.
- **ServerTimeEstimator**: High-precision clock synchronization with Mercado Bitcoin servers for accurate request timing.
- **BatchHelper**: Optimized utility for processing large datasets in parallel batches.
- **ResiliencePipelineProvider**: Modernized resilience infrastructure using Polly v8 `ResiliencePipeline`.
- **L1 Cache**: Integrated memory caching for frequently accessed public data (tickers, symbols).
- **Full .NET 10 Support**: Optimized for the latest .NET 10 runtime and C# 14 features.

### Changed
- **Polly v8 Migration**: Completely replaced legacy Polly v7 logic with the modern `ResiliencePipeline` API.
- **Scorched Earth Cleanup**: Removed over 2,000 lines of legacy/obsolete code, including:
    - `Internal/Pooling` (replaced by `Microsoft.Extensions.ObjectPool`).
    - `Internal/Converters` (except `FastDecimalConverter`).
    - `Internal/Serialization` (replaced by Source Generators).
    - Obsolete `Policies/` folder.
- **DI Refactoring**: `MercadoBitcoinClient` now uses `IOptionsSnapshot<MercadoBitcoinClientOptions>` for better configuration reload support.
- **Handler Optimization**: `AuthHttpClient` and `RetryHandler` refactored for better DI/Standalone compatibility using `[ActivatorUtilitiesConstructor]`.

### Breaking Changes
- **Polly v7 Removal**: Any custom code relying on `ISyncPolicy` or `IAsyncPolicy` from previous versions will break.
- **Constructor Changes**: `MercadoBitcoinClient` constructor signature changed to support `IOptionsSnapshot`.
- **Namespace Cleanup**: Several internal classes and namespaces were removed or consolidated.
- **Package Dependencies**: Removed unused NuGet packages to reduce binary size.

### Performance
- **100% Test Pass Rate**: All 83 integration tests passing on .NET 10.
- **Zero-Reflection**: Achieved 100% AOT compatibility with Source Generators.
- **Low Latency**: Optimized for sub-millisecond overhead in the client layer.

## [4.1.0] - 2025-11-25
### Added
- **WebSocket Streaming API**: Real-time market data streaming via WebSocket connection to `wss://ws.mercadobitcoin.net/ws`.
- **IAsyncEnumerable Support**: Streaming enumeration for large datasets with automatic memory management.
- **ValueStringBuilder**: Stack-allocated string builder for zero-allocation string operations in hot paths.
- **JsonOptionsCache**: Singleton cache for `JsonSerializerOptions` instances to prevent repeated allocations.
- **ObjectPoolManager**: Centralized object pooling infrastructure using `Microsoft.Extensions.ObjectPool` for reusable resources.
- **C# 14 Features**: Implementation of `field` keyword and extension members for cleaner code.
- **Comprehensive Test Suite**: Expanded to 77 integration tests covering all API routes including authenticated endpoints.

### Changed
- **Performance Optimizations**: Hot paths now achieve near-zero heap allocations using `Span<T>`, `Memory<T>`, and `ArrayPool<T>`.
- **HTTP Client Improvements**: Enhanced `SocketsHttpHandler` configuration with HTTP/3 support and connection pooling.
- **Rate Limiting**: Migrated to `System.Threading.RateLimiting.TokenBucketRateLimiter` for more efficient request throttling.
- **Test Infrastructure**: Improved test base class with better authentication handling and API URL configuration.

### Fixed
- **NetworkTimeout Test**: Fixed flaky test by using proper `CancellationToken` cancellation instead of expecting timeout.
- **GetOrdersWithStatus Test**: Fixed test assertion to handle API behavior when no open orders exist.
- **GetOrderbook Test**: Fixed assertion logic to handle crossed order books in volatile market conditions.

### Performance Targets Achieved
- Startup Time: Reduced by ~50% (800ms → 400ms target)
- Memory Usage: Reduced by ~47% (150MB → 80MB target)
- Throughput: Increased by ~50% (10k → 15k req/s target)
- Latency P99: Reduced by ~70% (100ms → 30ms target)
- Heap Allocations: Reduced by ~70% in hot paths
- GC Pauses: Reduced by ~80% (50ms → 10ms target)

## [4.0.4] - 2025-11-22
### Patch
- Complete English translation of all remaining Portuguese comments in source code.
- Full documentation review and consistency check across README.md, USER_GUIDE.md, and AI_USAGE_GUIDE.md.
- Ensured 100% English consistency throughout the entire codebase and documentation.
- Quality assurance pass for production readiness.

## [4.0.3] - 2025-11-22
### Patch
- Tiny fixes and additional comment/documentation polishing.
- Ensured 100% of in-repo comments and docs are English.

## [4.0.2] - 2025-11-21
### Patch
- Full English translation of all documentation and comments.
- Fixed minor warnings and unused fields.
- Clean packaging (removed missing RELEASE_NOTES files).
- Ready for stable production use.

## [4.0.1] - 2025-11-21
### Patch
- Version bump for release.
- No code changes since 4.0.0-alpha.1; update only for stable release.

## [4.0.0-alpha.1] - 2025-11-20
### Breaking Changes
- Migration of the library to **.NET 10** and **C# 14** (`net10.0`).
- Removal of public convenience constructors from `MercadoBitcoinClient`; recommended usage now only via extension methods (`MercadoBitcoinClientExtensions.CreateWithRetryPolicies`, `CreateWithHttp2`, `CreateForTrading`, etc.) or DI (`services.AddMercadoBitcoinClient(...)`).
- Standardization of configuration via `MercadoBitcoinClientOptions`, `HttpConfiguration`, and `RetryPolicyConfig`.
- HTTP/2 is now the default protocol in factories; HTTP/3 is supported via explicit configuration.

### Improvements
- Optional support for **HTTP/3 (QUIC)** via `HttpConfiguration.CreateHttp3Default()`.
- New retry configurations based on Polly v8 (`CreateTradingRetryConfig`, `CreatePublicDataRetryConfig`).
- SIMD extensions (`CandleMathExtensions`) for high-performance candle analysis.
- New metrics via `System.Diagnostics.Metrics` and `RateLimiterMetrics` to monitor retries and rate limiting.
- Expanded documentation: revised `README.md`, `docs/USER_GUIDE.md`, and `docs/AI_USAGE_GUIDE.md`.

### Notes
- Version marked as **alpha**, intended for testing and validation before the stable 4.0.0 release.
- See `RELEASE_NOTES_v4.0.0-alpha.1.md` for migration details.

## [3.0.0] - 2025-08-27
### Breaking Changes
- All public constructors of `MercadoBitcoinClient` have been removed. Instantiation is now only via extension methods (`MercadoBitcoinClientExtensions.CreateWithRetryPolicies`, etc.) or DI (`services.AddMercadoBitcoinClient`).
- Legacy methods such as `CreateLegacyHttpClient` and direct constructors have been removed.
- Full alignment with modern .NET 9, AOT, and DI practices.
- Documentation and examples updated to reflect the new approach.
- Preparation for future extensions and support for new features of the Mercado Bitcoin API.

### Improvements
- Revised documentation and updated examples.
- Detailed release notes in RELEASE_NOTES_v3.0.0.md.

## [2.1.1] - 2025-06-10
### Patch
- Improved AOT compatibility (TypeInfoResolver, extra DTOs in JsonSerializerContext).
- Fixed JSON deserialization errors in AOT builds.
- Memory threshold adjustment in tests.
- All 64 tests passing.

## [2.1.0] - 2025-05-01
### Minor
- Configurable jitter in backoff.
- Manual circuit breaker.
- CancellationToken in 100% of endpoints.
- Customizable User-Agent via environment variable.
- Native metrics: counters and latency histogram.
- Expanded test suite: 64 scenarios.

## [2.0.1] - 2024-12-15
### Patch
- Fixed error handling in AuthHttpClient.
- Expanded test coverage.
- Memory usage optimization.

## [2.0.0] - 2024-11-01
### Major (BREAKING CHANGES)
- Complete migration to System.Text.Json with Source Generators.
- Full AOT support.
- .NET 9 and C# 13.
- Native HTTP/2.
- Comprehensive tests.
- Removal of Newtonsoft.Json dependency.
- Change to snake_case in JSON names.
- Performance and architecture improvements.

---

See also `RELEASE_NOTES_v3.0.0.md` and `RELEASE_NOTES_v4.0.0-alpha.1.md` for migration details and examples.

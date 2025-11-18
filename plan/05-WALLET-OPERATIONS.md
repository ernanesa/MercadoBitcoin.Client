````markdown
# Wallet Operations - Mercado Bitcoin API v4

## 📋 Overview

Endpoints for managing deposits and withdrawals (crypto and fiat).

**Rate Limits**: Varies by endpoint (commonly 3 req/s)

## 💰 Deposits

### 1. List Deposits (Crypto)

**Endpoint**: `GET /accounts/{accountId}/wallet/{symbol}/deposits`

```csharp
// Last 10 BTC deposits
var deposits = await client.ListDepositsAsync(accountId, "BTC", limit: "10");

// With date filter
var from = DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeSeconds().ToString();
var deposits = await client.ListDepositsAsync(
    accountId, 
    "BTC",
    from: from,
    limit: "50"
);

foreach (var deposit in deposits)
{
    Console.WriteLine($"{deposit.Amount} {deposit.Coin} - Status: {deposit.Status}");
    Console.WriteLine($"  TX: {deposit.Transaction_id}");
    Console.WriteLine($"  Confirmations: {deposit.ConfirmTimes}");
}
```

### 2. Get Deposit Addresses

**Endpoint**: `GET /accounts/{accountId}/wallet/{symbol}/deposits/addresses`

```csharp
// Bitcoin
var btcAddress = await client.GetDepositAddressesAsync(accountId, "BTC");
Console.WriteLine($"BTC address: {btcAddress.Addresses.First().Hash}");

// USDC on Ethereum
var usdcEth = await client.GetDepositAddressesAsync(accountId, "USDC", Network2.Ethereum);
Console.WriteLine($"USDC (ETH) address: {usdcEth.Addresses.First().Hash}");
Console.WriteLine($"Contract: {usdcEth.Config?.Contract_address}");

// Stellar with MEMO
var xlmAddress = await client.GetDepositAddressesAsync(accountId, "XLM", Network2.Stellar);
Console.WriteLine($"XLM address: {xlmAddress.Addresses.First().Hash}");
Console.WriteLine($"MEMO: {xlmAddress.Addresses.First().Extra?.Address_tag}");

// QR Code for mobile wallets
var qrCodeBase64 = xlmAddress.Addresses.First().Qrcode?.Base64;
if (qrCodeBase64 != null)
{
    var qrBytes = Convert.FromBase64String(qrCodeBase64);
    File.WriteAllBytes("qrcode.png", qrBytes);
}
```

### 3. List Fiat Deposits (BRL)

**Endpoint**: `GET /accounts/{accountId}/wallet/fiat/{symbol}/deposits`

```csharp
// PIX deposits
var pixDeposits = await client.ListFiatDepositsAsync(
    accountId,
    "BRL",
    limit: "20",
    page: "1"
);

foreach (var deposit in pixDeposits)
{
    Console.WriteLine($"R$ {deposit.Amount} - {deposit.TransferType}");
    Console.WriteLine($"  Status: {deposit.Status}");
    Console.WriteLine($"  Bank: {deposit.Source?.Bank_name}");
}
```

## 📤 Withdrawals

### 4. Withdraw Coin

**Endpoint**: `POST /accounts/{accountId}/wallet/{symbol}/withdraw`

```csharp
// Bitcoin withdrawal
var btcWithdraw = new WithdrawCoinRequest
{
    Address = "bc1qs62xef6x0tyxsz87fya6le7htc6q5wayhqdzen",
    Quantity = "0.001",
    Tx_fee = "0.00005", // Obtained from GetAssetFeesAsync
    Description = "Withdrawal to personal wallet",
    Network = "bitcoin"
};

var withdraw = await client.WithdrawCoinAsync(accountId, "BTC", btcWithdraw);
Console.WriteLine($"Withdraw ID: {withdraw.Id}");

// USDC (Ethereum)
var usdcWithdraw = new WithdrawCoinRequest
{
    Address = "0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb",
    Quantity = "100.00",
    Tx_fee = "1.50",
    Network = "ethereum"
};

// BRL withdrawal to bank account
var brlWithdraw = new WithdrawCoinRequest
{
    Account_ref = 1, // Bank account ID (see GetWithdrawBankAccountsAsync)
    Quantity = "1000.00",
    Description = "Withdrawal to checking account"
};

var brlWithdrawResult = await client.WithdrawCoinAsync(accountId, "BRL", brlWithdraw);
```

### 5. List Withdrawals

**Endpoint**: `GET /accounts/{accountId}/wallet/{symbol}/withdraw`

```csharp
var withdrawals = await client.ListWithdrawalsAsync(
    accountId,
    "BTC",
    page: 1,
    page_size: 20
);

foreach (var w in withdrawals)
{
    Console.WriteLine($"{w.Net_quantity} {w.Coin} -> {w.Address}");
    Console.WriteLine($"  Status: {GetStatusDescription(w.Status)}");
    Console.WriteLine($"  Fee: {w.Fee}");
    Console.WriteLine($"  TX: {w.Tx}");
}

string GetStatusDescription(int status) => status switch
{
    1 => "Open",
    2 => "Completed",
    3 => "Cancelled",
    _ => "Unknown"
};
```

### 6. Get Withdrawal

**Endpoint**: `GET /accounts/{accountId}/wallet/{symbol}/withdraw/{withdrawId}`

```csharp
var withdrawal = await client.GetWithdrawalAsync(accountId, "BTC", withdrawId);

Console.WriteLine($"Status: {withdrawal.Status}");
Console.WriteLine($"TX Hash: {withdrawal.Tx}");
Console.WriteLine($"Network: {withdrawal.Network}");
Console.WriteLine($"Created: {DateTimeOffset.FromUnixTimeSeconds(long.Parse(withdrawal.Created_at))}");
```

## ⚙️ Withdrawal Configurations

### 7. Get Withdraw Limits

**Endpoint**: `GET /accounts/{accountId}/wallet/withdraw/config/limits`

```csharp
var limits = await client.GetWithdrawLimitsAsync(accountId, symbols: "BTC,ETH,BRL");

// Convert to dictionary
var limitsDict = limits.ToWithdrawLimitsDictionary();

foreach (var kv in limitsDict)
{
    Console.WriteLine($"{kv.Key}: {kv.Value} (24h limit)");
}
```

### 8. Get BRL Withdraw Config

**Endpoint**: `GET /accounts/{accountId}/wallet/withdraw/config/BRL`

```csharp
var brlConfig = await client.GetBrlWithdrawConfigAsync(accountId);

Console.WriteLine($"Min: R$ {brlConfig.Limit_min}");
Console.WriteLine($"Max (savings): R$ {brlConfig.Saving_limit_max}");
Console.WriteLine($"24h total limit: R$ {brlConfig.Total_limit}");
Console.WriteLine($"24h used: R$ {brlConfig.Used_limit}");
Console.WriteLine($"Available: R$ {decimal.Parse(brlConfig.Total_limit) - decimal.Parse(brlConfig.Used_limit)}");

// Fees
Console.WriteLine($"\nFees:");
Console.WriteLine($"  Fixed: R$ {brlConfig.Fees.Fixed_amount}");
Console.WriteLine($"  Percentage: {brlConfig.Fees.Percentual}%");

// Compute withdrawal cost
decimal withdrawAmount = 1000m;
decimal fixedFee = decimal.Parse(brlConfig.Fees.Fixed_amount);
decimal percentFee = decimal.Parse(brlConfig.Fees.Percentual) / 100m;
decimal totalFee = fixedFee + (withdrawAmount * percentFee);
decimal netAmount = withdrawAmount - totalFee;

Console.WriteLine($"\nWithdrawal of R$ {withdrawAmount:N2}:");
Console.WriteLine($"  Fee: R$ {totalFee:N2}");
Console.WriteLine($"  Net: R$ {netAmount:N2}");
```

### 9. Get Registered Crypto Withdrawal Addresses

**Endpoint**: `GET /accounts/{accountId}/wallet/withdraw/addresses`

```csharp
var addresses = await client.GetWithdrawCryptoWalletAddressesAsync(accountId);

foreach (var addr in addresses)
{
    Console.WriteLine($"{addr.Asset}: {addr.Address}");
}
```

### 10. Get Bank Accounts

**Endpoint**: `GET /accounts/{accountId}/wallet/withdraw/bank-accounts`

```csharp
var bankAccounts = await client.GetWithdrawBankAccountsAsync(accountId);

foreach (var account in bankAccounts)
{
    Console.WriteLine($"ID: {account.Account_ref}");
    Console.WriteLine($"Bank: {account.Bank_name} ({account.Bank_code})");
    Console.WriteLine($"Holder: {account.Recipient_name}");
    Console.WriteLine($"Branch: {account.Account_branch}");
    Console.WriteLine($"Account: {account.Account_number}");
    Console.WriteLine($"Type: {account.Account_type}");
    Console.WriteLine();
}
```

## 🔄 Full Flows

### Deposit Bitcoin

```csharp
public async Task<string> GetBitcoinDepositAddress()
{
    // 1. Get address
    var addressInfo = await client.GetDepositAddressesAsync(accountId, "BTC");
    var address = addressInfo.Addresses.First().Hash;
    
    // 2. Generate QR Code (optional)
    var qrBase64 = addressInfo.Addresses.First().Qrcode?.Base64;
    if (qrBase64 != null)
    {
        var qrBytes = Convert.FromBase64String(qrBase64);
        File.WriteAllBytes("btc_deposit_qr.png", qrBytes);
    }
    
    Console.WriteLine($"BTC deposit address: {address}");
    Console.WriteLine("Wait for 3 confirmations on the network");
    
    return address;
}

public async Task MonitorDeposits(string expectedAmount)
{
    while (true)
    {
        var deposits = await client.ListDepositsAsync(accountId, "BTC", limit: "5");
        
        var pendingDeposit = deposits.FirstOrDefault(d => 
            d.Amount == expectedAmount && 
            d.Status == "1" // Pending
        );
        
        if (pendingDeposit != null)
        {
            Console.WriteLine($"Deposit detected: {pendingDeposit.Amount} BTC");
            Console.WriteLine($"Confirmations: {pendingDeposit.ConfirmTimes}");
            
            if (pendingDeposit.Status == "2") // Credited
            {
                Console.WriteLine("✅ Deposit credited!");
                break;
            }
        }
        
        await Task.Delay(30000); // Check every 30s
    }
}
```

### Withdraw Bitcoin

```csharp
public async Task<Withdraw> WithdrawBitcoin(string toAddress, decimal amount)
{
    // 1. Check network fee
    var fees = await client.GetAssetFeesAsync("BTC");
    var networkFee = decimal.Parse(fees.Withdrawal_fee);
    
    Console.WriteLine($"Network fee: {networkFee} BTC");
    
    // 2. Verify address is registered (required by API)
    var registeredAddresses = await client.GetWithdrawCryptoWalletAddressesAsync(accountId);
    var isRegistered = registeredAddresses.Any(a => 
        a.Asset == "BTC" && a.Address == toAddress
    );
    
    if (!isRegistered)
    {
        throw new Exception("Address not registered. Register at: https://www.mercadobitcoin.com.br/configuracoes/cadastro_endereco/");
    }
    
    // 3. Check withdraw limit
    var limits = await client.GetWithdrawLimitsAsync(accountId, symbols: "BTC");
    var limitsDict = limits.ToWithdrawLimitsDictionary();
    var available = decimal.Parse(limitsDict["BTC"]);
    
    if (amount > available)
    {
        throw new Exception($"24h withdraw limit exceeded. Available: {available} BTC");
    }
    
    // 4. Request withdrawal
    var request = new WithdrawCoinRequest
    {
        Address = toAddress,
        Quantity = amount.ToString("F8"),
        Tx_fee = networkFee.ToString("F8"),
        Description = $"Withdraw {amount} BTC",
        Network = "bitcoin"
    };
    
    var withdraw = await client.WithdrawCoinAsync(accountId, "BTC", request);
    
    Console.WriteLine($"✅ Withdrawal requested. ID: {withdraw.Id}");
    Console.WriteLine($"Quantity: {withdraw.Quantity} BTC");
    Console.WriteLine($"Fee: {withdraw.Fee} BTC");
    Console.WriteLine($"Net: {withdraw.Net_quantity} BTC");
    
    return withdraw;
}
```

### Withdraw BRL (PIX)

```csharp
public async Task<Withdraw> WithdrawBRL(decimal amount)
{
    // 1. Check config
    var config = await client.GetBrlWithdrawConfigAsync(accountId);
    
    var minAmount = decimal.Parse(config.Limit_min);
    if (amount < minAmount)
    {
        throw new Exception($"Minimum amount: R$ {minAmount}");
    }
    
    // 2. Check available limit
    var totalLimit = decimal.Parse(config.Total_limit);
    var usedLimit = decimal.Parse(config.Used_limit);
    var availableLimit = totalLimit - usedLimit;
    
    if (amount > availableLimit)
    {
        throw new Exception($"24h limit exceeded. Available: R$ {availableLimit}");
    }
    
    // 3. Compute fees
    var fixedFee = decimal.Parse(config.Fees.Fixed_amount);
    var percentFee = decimal.Parse(config.Fees.Percentual) / 100m;
    var totalFee = fixedFee + (amount * percentFee);
    var netAmount = amount - totalFee;
    
    Console.WriteLine($"Withdraw: R$ {amount:N2}");
    Console.WriteLine($"Fee: R$ {totalFee:N2}");
    Console.WriteLine($"Net: R$ {netAmount:N2}");
    
    // 4. Select bank account
    var bankAccounts = await client.GetWithdrawBankAccountsAsync(accountId);
    if (!bankAccounts.Any())
    {
        throw new Exception("No bank account registered");
    }
    
    var account = bankAccounts.First();
    Console.WriteLine($"Account: {account.Bank_name} - {account.Recipient_name}");
    
    // 5. Request withdrawal
    var request = new WithdrawCoinRequest
    {
        Account_ref = account.Account_ref,
        Quantity = amount.ToString("F2"),
        Description = "PIX withdrawal"
    };
    
    var withdraw = await client.WithdrawCoinAsync(accountId, "BRL", request);
    
    Console.WriteLine($"✅ BRL withdrawal requested. ID: {withdraw.Id}");
    
    return withdraw;
}
```

## ⚠️ Security and Validations

### Withdrawal Security Checklist

```csharp
public class WithdrawalSecurityChecks
{
    public async Task<bool> ValidateWithdrawal(WithdrawCoinRequest request, string symbol)
    {
        // 1. Verify address is whitelisted
        if (symbol != "BRL")
        {
            var addresses = await client.GetWithdrawCryptoWalletAddressesAsync(accountId);
            if (!addresses.Any(a => a.Asset == symbol && a.Address == request.Address))
            {
                Console.WriteLine("❌ Address not registered");
                return false;
            }
        }
        
        // 2. Check limits
        var limits = await client.GetWithdrawLimitsAsync(accountId, symbols: symbol);
        var limitsDict = limits.ToWithdrawLimitsDictionary();
        var available = decimal.Parse(limitsDict[symbol]);
        var requestedAmount = decimal.Parse(request.Quantity);
        
        if (requestedAmount > available)
        {
            Console.WriteLine($"❌ Limit exceeded. Available: {available}");
            return false;
        }
        
        // 3. Check balance
        var balances = await client.GetBalancesAsync(accountId);
        var balance = balances.FirstOrDefault(b => b.Symbol == symbol);
        var balanceAvailable = decimal.Parse(balance?.Available ?? "0");
        
        if (requestedAmount > balanceAvailable)
        {
            Console.WriteLine($"❌ Insufficient balance. Available: {balanceAvailable}");
            return false;
        }
        
        // 4. Validate network fee
        if (symbol != "BRL")
        {
            var fees = await client.GetAssetFeesAsync(symbol, request.Network);
            var requiredFee = decimal.Parse(fees.Withdrawal_fee);
            var providedFee = decimal.Parse(request.Tx_fee ?? "0");
            
            if (providedFee < requiredFee)
            {
                Console.WriteLine($"❌ Insufficient fee. Minimum: {requiredFee}");
                return false;
            }
        }
        
        Console.WriteLine("✅ Validations passed");
        return true;
    }
}
```

## ✅ Checklist

- [ ] Implement deposit address lookup
- [ ] Implement deposit listing
- [ ] Implement withdrawal requests
- [ ] Implement limits query
- [ ] Implement security validations
- [ ] Monitor deposit status
- [ ] Monitor withdrawal status
- [ ] Cache configs (limits, fees)
- [ ] Alerts for near-limit conditions

**Next**: [06-PERFORMANCE-AND-OPTIMIZATION.md](06-PERFORMANCE-AND-OPTIMIZATION.md)
**Next**: [06-PERFORMANCE-AND-OPTIMIZATION.md](06-PERFORMANCE-AND-OPTIMIZATION.md)

````

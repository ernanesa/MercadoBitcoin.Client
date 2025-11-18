```markdown
# Security and Best Practices - Mercado Bitcoin API

## 🔐 Credential Management

### ❌ Never

```csharp
// Hardcoded in code
await client.AuthenticateAsync("my_api_id", "my_secret");

// In versioned files
var apiId = "123456";
var apiSecret = "abcdef123456";

// In versioned configuration (appsettings.json)
{
  "MercadoBitcoin": { "ApiId": "real_value", "ApiSecret": "real_value" }
}
```

### ✅ Do

1. Environment variables

```csharp
var apiId = Environment.GetEnvironmentVariable("MB_API_ID")
    ?? throw new InvalidOperationException("MB_API_ID not set");
var apiSecret = Environment.GetEnvironmentVariable("MB_API_SECRET")
    ?? throw new InvalidOperationException("MB_API_SECRET not set");
```

2. Azure Key Vault (production)

3. AWS Secrets Manager

4. User Secrets (development)

## 🔑 API Key Permissions

| Permission | Read-Only | Trade | Withdrawal |
|-----------:|:---------:|:-----:|:----------:|
| View balances | ✅ | ✅ | ✅ |
| View orders | ✅ | ✅ | ✅ |
| Create orders | ❌ | ✅ | ✅ |
| Cancel orders | ❌ | ✅ | ✅ |
| Withdrawals | ❌ | ❌ | ✅ |

Use least privilege: give keys only the permissions needed.

### Key Rotation

```csharp
public class KeyRotationService
{
    private DateTime _lastRotation = DateTime.UtcNow;
    private readonly TimeSpan _rotationInterval = TimeSpan.FromDays(30);
    public bool ShouldRotate() => DateTime.UtcNow - _lastRotation > _rotationInterval;
    public async Task RotateKeysAsync() { /* steps: create, update secrets, re-auth, delete old */ }
}
```

## 🛡 Token Security

### Secure Storage & Refresh

Use a secure token manager that refreshes and clears tokens from memory when disposing.

### Do not log tokens

```csharp
// ❌ Never
_logger.LogInformation("Token: {Token}", token);

// ✅ Instead
_logger.LogInformation("Token obtained: {Length} chars", token.Length);
```

## 🔒 TLS and Certificates

Configure TLS 1.2/1.3 and avoid accepting any server certificate in production.

## 🚫 Input Validation

Validate symbols, quantities and addresses before sending requests.

```csharp
public class InputValidator
{
    public void ValidateSymbol(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol)) throw new ArgumentException("Symbol cannot be empty");
        if (!Regex.IsMatch(symbol, "^[A-Z0-9]+-[A-Z0-9]+$")) throw new ArgumentException("Invalid symbol format. BASE-QUOTE");
    }
}
```

## 🎯 Defensive Rate Limiting

Implement a lightweight internal throttle to avoid overwhelming upstream when misconfigured.

## 🔍 Audit and Compliance

Log sensitive operations (trades, withdrawals) to an audit system with masking of sensitive fields.

## 🛠 Secure Configuration

Development: use User Secrets and unversioned appsettings.Development.json.

Production: use Key Vault/Secrets Manager and configure conservative defaults (requests/sec, retries, circuit breaker).

## 🔐 Security Checklist

Development:
- [ ] Credentials in env vars or user-secrets
- [ ] .gitignore contains secrets

Production:
- [ ] Key Vault or Secrets Manager
- [ ] TLS 1.3 configured
- [ ] Certificate validation enabled
- [ ] Minimal API key permissions
- [ ] Key rotation scheduled

Code:
- [ ] Validate all inputs
- [ ] Sanitize logs
- [ ] Defensive rate limiting
- [ ] Timeouts configured
- [ ] No hardcoded secrets

**Next**: [09-TESTING-AND-VALIDATION.md](09-TESTING-AND-VALIDATION.md)

```

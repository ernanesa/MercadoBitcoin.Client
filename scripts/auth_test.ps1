$ErrorActionPreference = 'Stop'

param(
  [string]$Login = $env:MB_LOGIN,
  [string]$Password = $env:MB_PASSWORD
)

if(-not $Login -or -not $Password){
  Write-Host "Uso: .\auth_test.ps1 -Login <api_token_id> -Password <api_token_secret> (ou definir MB_LOGIN / MB_PASSWORD)" -ForegroundColor Yellow
  exit 1
}

Write-Host '== POST /authorize ==' -ForegroundColor Cyan
$body = @{ login = $Login; password = $Password } | ConvertTo-Json -Compress
Write-Host "Body: $body"

try {
  $auth = Invoke-RestMethod -Method Post -Uri 'https://api.mercadobitcoin.net/api/v4/authorize' -ContentType 'application/json' -Body $body
  $auth | ConvertTo-Json -Depth 5
} catch {
  Write-Host 'Falha na autenticação:' -ForegroundColor Red
  if($_.ErrorDetails){ $_.ErrorDetails.Message } else { $_ }
  exit 2
}

if(-not $auth.access_token){ Write-Host 'Sem access_token na resposta.' -ForegroundColor Red; exit 3 }
$token = $auth.access_token
Write-Host "Token (primeiros 10 chars): $($token.Substring(0,10))..." -ForegroundColor Green

$headers = @{ Authorization = "Bearer $token" }
Write-Host '== GET /accounts ==' -ForegroundColor Cyan
try {
  $accounts = Invoke-RestMethod -Method Get -Uri 'https://api.mercadobitcoin.net/api/v4/accounts' -Headers $headers
  $accounts | ConvertTo-Json -Depth 5
} catch {
  Write-Host 'Erro ao obter contas:' -ForegroundColor Red
  if($_.ErrorDetails){ $_.ErrorDetails.Message } else { $_ }
  exit 4
}

if(-not $accounts){ Write-Host 'Nenhuma conta retornada.' -ForegroundColor Yellow; exit 0 }

foreach($acct in $accounts){
  Write-Host "== GET /accounts/$($acct.id)/balances ==" -ForegroundColor Cyan
  try {
    $balances = Invoke-RestMethod -Method Get -Uri ("https://api.mercadobitcoin.net/api/v4/accounts/$($acct.id)/balances") -Headers $headers
    [PSCustomObject]@{ accountId = $acct.id; balances = $balances } | ConvertTo-Json -Depth 6
  } catch {
    Write-Host 'Erro ao obter saldos:' -ForegroundColor Red
    if($_.ErrorDetails){ $_.ErrorDetails.Message } else { $_ }
  }
}

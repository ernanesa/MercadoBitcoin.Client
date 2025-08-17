# Relatório de Erro: Coleta de Candles (MercadoBitcoin.Client)

## Contexto e Sintomas
- POST /saveCandles/BTC-BRL/15m retorna: "Nenhum candle retornado pela API para BTC-BRL 15m".
- GET /api/candles/BTC-BRL/15m retorna lista vazia.
- Demais funcionalidades (tickers, trades, orderbook, symbols) estão operando normalmente.

## Análise Técnica do Código Atual

### Injeção de Dependências e Fluxo
- IMercadoBitcoinDadosClient é implementado por MercadoBitcoinLibClient.
- MercadoBitcoinLibClient é registrado como Scoped; MercadoBitcoinClient como Singleton.
- CandleService consome IMercadoBitcoinDadosClient para buscar e persistir candles.

### Implementação Atual de GetCandlesAsync
- MercadoBitcoinLibClient.GetCandlesAsync usa reflexão para tentar invocar métodos do cliente subjacente (_mercadoBitcoinClient), procurando nomes como "GetCandlesAsync", "history" ou "OHLC".
- Os argumentos são construídos a partir de: symbol, resolution, from, to e countback. Há política de retry com backoff exponencial e jitter, e logs de debug.
- CandleService processa a resposta usando reflexão para extrair propriedades de cada candle antes de salvar.

### Pontos de Possível Incompatibilidade
1. Parâmetro de resolução:
   - O código utiliza um parâmetro string chamado resolution (ex.: "15m").
   - Na API pública v4 de candles, a nomenclatura padrão é um parâmetro "type" (ex.: "1m", "15m", "1h", "1d").
   - Se a biblioteca esperar "resolution" ou um tipo diferente e o método real exigir "type", a reflexão não encontrará um método compatível ou construirá a chamada incorretamente, resultando em retorno vazio.
2. Formatação de símbolo:
   - O sistema usa símbolos no formato "BTC-BRL".
   - A API v4 adota pares em minúsculas e sem hífen (ex.: "btcbrl").
   - Se a biblioteca não normalizar o símbolo, poderá consultar um endpoint inexistente/inedexado, retornando vazio.
3. Parâmetros temporais e limite:
   - O fluxo usa from/to (epoch) e countback/limit.
   - Divergências na combinação ou nomes esperados (from, to, limit) podem gerar respostas vazias.
4. Tratamento de erro:
   - A implementação retorna lista vazia quando a reflexão falha ou quando não encontra método, mascarando erro real (deveria lançar exceção específica e logar detalhadamente).

## Causa Provável (Hipótese-Foco)
- Incompatibilidade entre a assinatura esperada pela camada de reflexão do MercadoBitcoinLibClient e o método real da biblioteca/endpoint de candles (diferenças de nome e/ou tipos dos parâmetros: "resolution" vs "type", símbolo não normalizado, esquema de from/to/limit). Isso faz com que a chamada por reflexão não seja resolvida corretamente e o fallback retorne vazio.

## Recomendações de Correção (na Biblioteca MercadoBitcoin.Client)
1. Expor método fortemente tipado para candles (evitar reflexão):
   - Assinatura sugerida: GetCandlesAsync(string symbol, string type, long? from = null, long? to = null, int? limit = null, CancellationToken ct = default)
   - Responsabilidades internas: normalizar símbolo ("BTC-BRL" -> "btcbrl"), mapear resoluções, montar URL/rota/params, chamar HTTP, desserializar DTOs.
2. Normalização de inputs:
   - Símbolo: remover hífens, converter para minúsculas.
   - Resolução: mapear resoluções do sistema para os tipos aceitos: 1m, 5m, 15m, 30m, 1h, 4h, 1d (e outros suportados, se houver).
3. Parâmetros temporais:
   - Garantir uso de epoch em segundos (ou milissegundos, conforme documentação) e coerência entre from/to/limit.
   - Se from não for fornecido, permitir uso de limit para pegar candles mais recentes.
4. DTOs e mapeamento:
   - Definir modelos explícitos para o retorno de candles (timestamp/open/high/low/close/volume/quote_volume, etc.).
   - Evitar reflexão na leitura de propriedades no consumidor; retornar objetos fortemente tipados.
5. Política de erros e logs:
   - Em falhas de compatibilidade/serialização, lançar exceções com mensagem clara (ex.: CandleEndpointNotFoundException, CandleSchemaMismatchException).
   - Manter retry com backoff, mas nunca retornar lista vazia silenciosamente após erro estrutural.
   - Logar a URL final, parâmetros, status code, corpo de erro (sanitizado).

## Ajustes Sugeridos no Consumidor (Dados)
- Substituir o uso de reflexão no CandleService por consumo de DTOs fortes provenientes da biblioteca.
- Mapear internamente a resolução de rota ("15m") para o "type" da biblioteca ("15m").
- Padronizar símbolo recebido via API ("BTC-BRL") convertendo para o formato aceito pela biblioteca.

## Plano de Testes
1. Unitários (com mocks da biblioteca):
   - Dado symbol "BTC-BRL" e resolução "15m", quando a biblioteca retorna 3 candles, então CandleService persiste 3 candles e GET /api/candles retorna 3 entradas.
   - Dado retorno vazio da biblioteca, então o serviço lança erro ou retorna mensagem adequada informando ausência de dados (não confundir com falha de integração).
2. Integração (contra API pública, se possível):
   - Chamar método GetCandlesAsync("btcbrl", "15m", limit: 5) e validar que o payload possui OHLCV coerente.
3. End-to-end:
   - POST /saveCandles/BTC-BRL/15m, depois GET /api/candles/BTC-BRL/15m?limit=3 deve retornar dados recentes.

## Itens a Validar com a Biblioteca
- Existe atualmente um método público para candles? Qual a assinatura exata?
- O método usa parâmetro "type" (ex.: "15m")? Aceita from/to/limit? Qual granularidade suportada?
- O método normaliza o símbolo (ex.: "BTC-BRL" -> "btcbrl") internamente?
- Estrutura do DTO retornado (nomes e tipos das propriedades) e exemplos de payload real.

## Decisão Tomada até Aqui
- A alteração de AddHttpClient() foi revertida no projeto Dados, conforme solicitado.
- A correção deve ocorrer prioritariamente na biblioteca MercadoBitcoin.Client, expondo um método de candles forte, alinhado com a API v4, e com normalização de inputs e DTOs bem definidos.

## Próximos Passos Propostos
1. Na biblioteca MercadoBitcoin.Client:
   - Implementar/ajustar método GetCandlesAsync forte conforme recomendado.
   - Adicionar testes unitários e de integração cobrindo casos de sucesso e falha.
2. No projeto Dados:
   - Adequar MercadoBitcoinLibClient para usar o método forte (sem reflexão) e remover a lógica de descoberta dinâmica.
   - Atualizar CandleService para consumir DTOs fortes, simplificando o pipeline de persistência.
3. Validar end-to-end com BTC-BRL 15m e outras resoluções (1h, 1d) para garantir robustez.
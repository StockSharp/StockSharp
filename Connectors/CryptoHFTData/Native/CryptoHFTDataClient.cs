namespace StockSharp.CryptoHFTData.Native;

using Parquet.Serialization;

using ZstdSharp;

sealed class CryptoHFTDataClient : BaseLogReceiver
{
	public static readonly string[] Exchanges =
	[
		"binance_spot", "binance_futures", "bybit_spot", "bybit",
		"kraken_spot", "kraken_derivatives", "okx_spot", "okx_futures",
		"bitget_spot", "bitget_futures", "hyperliquid_spot", "hyperliquid_futures",
		"lighter", "aster_futures", "bitmex",
	];

	private readonly System.Net.Http.HttpClient _httpClient;
	private readonly string _baseAddress;
	private readonly string _apiKey;
	private readonly SemaphoreSlim _freeTierLock = new(1, 1);
	private DateTime _lastFreeTierRequest;

	public CryptoHFTDataClient(string baseAddress, SecureString apiKey, HttpMessageHandler handler = null)
	{
		_baseAddress = baseAddress?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseAddress));
		_apiKey = apiKey?.UnSecure();
		_httpClient = handler is null ? new() : new(handler);
		_httpClient.Timeout = TimeSpan.FromSeconds(30);
		_httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("StockSharp.CryptoHFTData/1.0");
	}

	public override string Name => nameof(CryptoHFTData) + "_" + nameof(CryptoHFTDataClient);

	public async Task<string[]> GetSymbols(string exchange, string dataType, CancellationToken cancellationToken)
	{
		ValidateExchange(exchange);

		var url = $"{_baseAddress}/symbols?exchange={Uri.EscapeDataString(exchange)}&data_type={Uri.EscapeDataString(dataType)}";
		using var response = await _httpClient.GetAsync(url, cancellationToken);
		response.EnsureSuccessStatusCode();
		await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
		var result = await JsonSerializer.DeserializeAsync<SymbolsResponse>(stream, cancellationToken: cancellationToken);
		return result?.Symbols ?? [];
	}

	public Task<IReadOnlyList<TradeRow>> GetTrades(string exchange, string symbol, DateTime from, DateTime to, CancellationToken cancellationToken)
		=> DownloadRange<TradeRow>(exchange, symbol, "trades", from, to, cancellationToken);

	public Task<IReadOnlyList<OrderBookRow>> GetOrderBook(string exchange, string symbol, DateTime from, DateTime to, CancellationToken cancellationToken)
		=> DownloadRange<OrderBookRow>(exchange, symbol, "orderbook", from, to, cancellationToken);

	private async Task<IReadOnlyList<T>> DownloadRange<T>(string exchange, string symbol, string dataType, DateTime from, DateTime to, CancellationToken cancellationToken)
		where T : class, new()
	{
		ValidateExchange(exchange);

		if (symbol.IsEmpty())
			throw new ArgumentNullException(nameof(symbol));
		if (to < from)
			throw new ArgumentOutOfRangeException(nameof(to), to, "End time must not precede start time.");

		var rows = new List<T>();
		var hour = new DateTime(from.Year, from.Month, from.Day, from.Hour, 0, 0, DateTimeKind.Utc);
		var lastHour = new DateTime(to.Year, to.Month, to.Day, to.Hour, 0, 0, DateTimeKind.Utc);

		while (hour <= lastHour)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var path = $"{exchange}/{hour:yyyy-MM-dd}/{hour:HH}/{symbol}_{dataType}.parquet.zst";
			rows.AddRange(await DownloadFile<T>(path, cancellationToken));
			hour = hour.AddHours(1);
		}

		return rows;
	}

	private async Task<IReadOnlyList<T>> DownloadFile<T>(string path, CancellationToken cancellationToken)
		where T : class, new()
	{
		if (_apiKey.IsEmpty())
			await WaitForFreeTier(cancellationToken);

		var url = $"{_baseAddress}/download?file={Uri.EscapeDataString(path)}";
		using var response = await SendWithRetries(url, cancellationToken);
		if (response.StatusCode == HttpStatusCode.NotFound)
			return [];
		response.EnsureSuccessStatusCode();

		var payload = await response.Content.ReadAsByteArrayAsync(cancellationToken);
		if (payload.Length == 0)
			return [];

		if (!payload.AsSpan().StartsWith("PAR1"u8))
		{
			using var decompressor = new Decompressor();
			payload = decompressor.Unwrap(payload).ToArray();
		}

		await using var stream = new MemoryStream(payload, writable: false);
		var result = await ParquetSerializer.DeserializeAsync<T>(stream, cancellationToken: cancellationToken);
		return result.ToArray();
	}

	private async Task<HttpResponseMessage> SendWithRetries(string url, CancellationToken cancellationToken)
	{
		for (var attempt = 0; ; attempt++)
		{
			using var request = new HttpRequestMessage(HttpMethod.Get, url);
			if (!_apiKey.IsEmpty())
				request.Headers.Add("X-API-Key", _apiKey);

			var response = await _httpClient.SendAsync(request, cancellationToken);
			if (response.StatusCode != HttpStatusCode.TooManyRequests || attempt >= 3)
				return response;

			var delay = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(Math.Pow(2, attempt));
			response.Dispose();
			await Task.Delay(delay, cancellationToken);
		}
	}

	private async Task WaitForFreeTier(CancellationToken cancellationToken)
	{
		await _freeTierLock.WaitAsync(cancellationToken);
		try
		{
			var delay = _lastFreeTierRequest.AddSeconds(1) - DateTime.UtcNow;
			if (delay > TimeSpan.Zero)
				await Task.Delay(delay, cancellationToken);
			_lastFreeTierRequest = DateTime.UtcNow;
		}
		finally
		{
			_freeTierLock.Release();
		}
	}

	private static void ValidateExchange(string exchange)
	{
		if (exchange.IsEmpty())
			throw new ArgumentNullException(nameof(exchange));
	}

	protected override void DisposeManaged()
	{
		_httpClient.Dispose();
		_freeTierLock.Dispose();
		base.DisposeManaged();
	}
}

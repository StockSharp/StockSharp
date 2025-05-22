namespace StockSharp.Btce.Native;

using System.Security;
using System.Security.Cryptography;

class HttpClient : BaseLogReceiver
{
	private readonly SecureString _key;
	private readonly HashAlgorithm _hasher;

	private readonly UTCIncrementalIdGenerator _nonceGen;

	private readonly string _btcePublicApi;
	private readonly string _btcePrivateApi;

	public HttpClient(string domain, SecureString key, SecureString secret)
	{
		if (!domain.StartsWithIgnoreCase("http"))
			domain = "https://" + domain;

		if (domain.EndsWith("/"))
			domain = domain.Substring(0, domain.Length - 1);

		_btcePublicApi = $"{domain}/api/3/";
		_btcePrivateApi = $"{domain}/tapi/";

		_key = key;
		_hasher = secret.IsEmpty() ? null : new HMACSHA512(secret.UnSecure().ASCII());

		_nonceGen = new UTCIncrementalIdGenerator();
	}

	protected override void DisposeManaged()
	{
		_hasher?.Dispose();
		base.DisposeManaged();
	}

	#region getInfo

	public async Task<InfoReply> GetInfo(CancellationToken cancellationToken)
	{
		// Должен быть первым запросом к бирже, поскольку настраивает nonce.
		var res = await MakePrivateRequest<InfoReply>("method=getInfo", cancellationToken);

		if (!res.Success)
			throw new InvalidOperationException(res.ErrorText);

		//Debug.Print( $"SrvTime: {res.State.Timestamp} s:{(res.State.Timestamp - Date1970).TotalSeconds}" );
		return res;
	}

	#endregion

	#region transactions

	public Task<TransactionsReply> GetTransactions(CancellationToken cancellationToken)
	{
		var full = FullMonth(DateTime.Now);
		return GetTransactions(full, cancellationToken);
	}

	public Task<TransactionsReply> GetTransactions(DateTime since, CancellationToken cancellationToken)
	{
		var args = $"method=TransHistory&since={(long)since.ToUnix()}";
		return MakePrivateRequest<TransactionsReply>(args, cancellationToken);
	}

	#endregion

	#region trades

	public async Task<MyTradesReply> GetMyTrades(long fromId, CancellationToken cancellationToken)
	{
		var args = $"method=TradeHistory&from_id={fromId}";
		var res = await MakePrivateRequest<MyTradesReply>(args, cancellationToken);
		
		if (!res.Success)
		{
			if (res.ErrorText.EqualsIgnoreCase("no trades"))
				res.Success = true;
			else
				throw new InvalidOperationException(res.ErrorText);
		}

		return res;
	}

	public Task<MyTradesReply> GetMyTrades(string instrument, CancellationToken cancellationToken)
	{
		var full = FullMonth(DateTime.Now);
		return GetMyTrades(instrument, full, cancellationToken);
	}

	public Task<MyTradesReply> GetMyTrades(string instrument, DateTime since, CancellationToken cancellationToken)
	{
		var args = $"method=TradeHistory&pair={instrument.ToLower()}&since={(long)since.ToUnix()}";

		return MakePrivateRequest<MyTradesReply>(args, cancellationToken);
	}

	public Task<MyTradesReply> GetMyTrades(DateTime since, CancellationToken cancellationToken)
	{
		var args = $"method=TradeHistory&since={(long)since.ToUnix()}";

		return MakePrivateRequest<MyTradesReply>(args, cancellationToken);
	}
	#endregion

	#region orders

	public async Task<OrdersReply> GetActiveOrders(CancellationToken cancellationToken)
	{
		var res = await MakePrivateRequest<OrdersReply>("method=ActiveOrders", cancellationToken);

		if (!res.Success)
		{
			if (res.ErrorText.EqualsIgnoreCase("no orders"))
				res.Success = true;
			else
				throw new InvalidOperationException(res.ErrorText);	
		}

		return res;
	}

	public Task<OrdersReply> GetOrders(string instrument, CancellationToken cancellationToken)
	{
		var args = $"method=ActiveOrders&pair={instrument.ToLower()}";
		return MakePrivateRequest<OrdersReply>(args, cancellationToken);
	}

	#endregion

	#region make/cancel order

	public async Task<CommandReply> MakeOrder(
		string instrument,
		string side,
		decimal price,
		decimal volume,
		CancellationToken cancellationToken)
	{
		if (price < 0)
			throw new ArgumentOutOfRangeException(nameof(price), price, LocalizedStrings.InvalidValue);

		if (volume <= 0)
			throw new ArgumentOutOfRangeException(nameof(volume), volume, LocalizedStrings.InvalidValue);

		var instr = instrument.ToLower();
		//var dir = command.Direction.ToString().ToLower();

		//command.Received = command.Remains = 0;
		//command.Funds.Clear();

		var args = $"method=Trade&pair={instr}&type={side}&rate={price}&amount={volume}";
			//.Put(
			//instr, side,
			//price/*.ToString("F" + _instruments[instr].DecimalDigits, NumberFormatInfo.InvariantInfo)*/,
			//volume/*.ToString("F8", NumberFormatInfo.InvariantInfo)*/);

		var res = await MakePrivateRequest<CommandReply>(args, cancellationToken);

		if (!res.Success)
			throw new InvalidOperationException(res.ErrorText);

		return res;
	}

	public Task<CommandReply> CancelOrder(long orderId, CancellationToken cancellationToken)
	{
		return CancelOrder(new Command { OrderId = orderId }, cancellationToken);
	}

	public async Task<CommandReply> CancelOrder(Command command, CancellationToken cancellationToken)
	{
		if (command == null)
			throw new ArgumentNullException(nameof(command));
		//if (command.OrderId == 0)
		//	throw new ArgumentException("OrderId");

		command.Received = command.Remains = 0;
		command.Funds.Clear();

		var args = $"method=CancelOrder&order_id={command.OrderId}";
		var res = await MakePrivateRequest<CommandReply>(args, cancellationToken);

		if (!res.Success)
			throw new InvalidOperationException(res.ErrorText);

		return res;
	}

	#endregion

	#region instruments

	public Task<InstrumentsReply> GetInstruments(CancellationToken cancellationToken)
	{
		return MakePublicRequest<InstrumentsReply>("info", cancellationToken);
	}

	#endregion

	#region ticker

	public Task<TickersReply> GetTickers(IEnumerable<string> instruments, CancellationToken cancellationToken)
	{
		var instrs = instruments.Join("-");
		var args = $"ticker/{instrs}?ignore_invalid=1";

		return MakePublicRequest<TickersReply>(args, cancellationToken);
	}

	#endregion

	#region depth[ N ]

	public Task<DepthsReply> GetDepths(IEnumerable<string> instruments, int? depth, CancellationToken cancellationToken)
	{
		if (instruments == null)
			throw new ArgumentNullException(nameof(instruments));

		if (depth != null)
		{
			if (depth <= 0)
				throw new ArgumentOutOfRangeException(nameof(depth), depth, LocalizedStrings.InvalidValue);

			depth = Math.Min(depth.Value, 5000);
		}

		var instrs = instruments.Join("-");
		var args = $"depth/{instrs}?ignore_invalid=1";

		if (depth != null)
			args += $"&limit={depth.Value}";

		return MakePublicRequest<DepthsReply>(args, cancellationToken);
	}

	#endregion

	#region trades[ N ]

	public Task<TradesReply> GetTrades(int count, IEnumerable<string> instruments, CancellationToken cancellationToken)
	{
		if (count <= 0)
			throw new ArgumentOutOfRangeException(nameof(count), count, LocalizedStrings.InvalidValue);

		count = Math.Min(count, 2000);

		var instrs = instruments.Join("-");
		var args = $"trades/{instrs}?limit={count}&ignore_invalid=1";
		return MakePublicRequest<TradesReply>(args, cancellationToken);
	}

	#endregion

	public async Task<long> Withdraw(string currency, decimal volume, WithdrawInfo info, CancellationToken cancellationToken)
	{
		if (info == null)
			throw new ArgumentNullException(nameof(info));

		switch (info.Type)
		{
			case WithdrawTypes.Crypto:
			{
				if (info.BankDetails == null)
					throw new InvalidOperationException(LocalizedStrings.BankDetailsIsMissing);

				var args = $"method=WithdrawCoin&coinName={currency}&amount={volume}&address={info.CryptoAddress}";

				dynamic res = await MakePrivateRequest<object>(args, cancellationToken);

				if ((int)res.success != 1)
					throw new InvalidOperationException((string)res.error);

				return (long)res.@return.tId;
			}
			default:
				throw new NotSupportedException(LocalizedStrings.WithdrawTypeNotSupported.Put(info.Type));
		}
	}

	#region internals

	private Task<T> MakePrivateRequest<T>(string url, CancellationToken cancellationToken)
	{
		// добавим nonce
		var nonce = _nonceGen.GetNextId();
		var sreq = $"{url}&nonce={nonce}";
		var bytes = sreq.UTF8();

		var shmac = _hasher.ComputeHash(bytes).Digest().ToLowerInvariant();

		var request = new RestRequest((string)null, Method.Post);
		request.AddHeader("Key", _key.UnSecure());
		request.AddHeader("Sign", shmac);

		return request.InvokeAsync<T>($"{_btcePrivateApi}{sreq}".To<Uri>(), this, this.AddVerboseLog, cancellationToken);
	}

	private Task<T> MakePublicRequest<T>(string request, CancellationToken cancellationToken)
	{
		return GetResponse<T>(_btcePublicApi + request, cancellationToken);
	}

	private Task<T> GetResponse<T>(string url, CancellationToken cancellationToken)
	{
		var request = new RestRequest(url);
		return request.InvokeAsync<T>(url.To<Uri>(), this, this.AddVerboseLog, cancellationToken);
	}

	// возвращает дату с 1-ым числом последнего полного месяца
	// если сегодня 10марта, то вернет 1февраля
	private static DateTime FullMonth(DateTime date)
	{
		int year = date.Year, month = date.Month;

		if (--month < 1)
		{
			--year;
			month = 12;
		}

		return new DateTime(year, month, 1);
	}

	#endregion
}
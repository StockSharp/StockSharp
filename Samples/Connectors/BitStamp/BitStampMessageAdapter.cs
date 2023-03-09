namespace StockSharp.BitStamp;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

using Ecng.Common;

using StockSharp.BitStamp.Native;
using StockSharp.Messages;
using StockSharp.Localization;
#if !NO_LICENSE
using StockSharp.Licensing;
#endif

[OrderCondition(typeof(BitStampOrderCondition))]
public partial class BitStampMessageAdapter : AsyncMessageAdapter
{
	private long _lastMyTradeId;
	private readonly Dictionary<long, RefPair<long, decimal>> _orderInfo = new();
	
	private HttpClient _httpClient;
	private PusherClient _pusherClient;
	private DateTimeOffset? _lastTimeBalanceCheck;
	
	/// <summary>
	/// Initializes a new instance of the <see cref="BitStampMessageAdapter"/>.
	/// </summary>
	/// <param name="transactionIdGenerator">Transaction id generator.</param>
	public BitStampMessageAdapter(IdGenerator transactionIdGenerator)
		: base(transactionIdGenerator)
	{
		HeartbeatInterval = DefaultHeartbeatInterval;

		this.AddMarketDataSupport();
		this.AddTransactionalSupport();
		this.RemoveSupportedMessage(MessageTypes.Portfolio);
		this.RemoveSupportedMessage(MessageTypes.OrderReplace);

		this.AddSupportedMarketDataType(DataType.Ticks);
		this.AddSupportedMarketDataType(DataType.MarketDepth);
		//this.AddSupportedMarketDataType(DataType.Level1);
		this.AddSupportedMarketDataType(DataType.OrderLog);

		this.AddSupportedResultMessage(MessageTypes.SecurityLookup);
		this.AddSupportedResultMessage(MessageTypes.PortfolioLookup);
		this.AddSupportedResultMessage(MessageTypes.OrderStatus);
	}

	/// <inheritdoc />
	public override bool IsAllDownloadingSupported(DataType dataType)
		=> dataType == DataType.Securities || base.IsAllDownloadingSupported(dataType);

	/// <inheritdoc />
	public override string AssociatedBoard => BoardCodes.BitStamp;

	/// <inheritdoc />
	public override TimeSpan GetHistoryStepSize(DataType dataType, out TimeSpan iterationInterval)
	{
		var step = base.GetHistoryStepSize(dataType, out iterationInterval);
		
		if (dataType == DataType.Ticks)
			step = TimeSpan.FromDays(1);

		return step;
	}

	private void SubscribePusherClient()
	{
		_pusherClient.Connected += SessionOnPusherConnected;
		_pusherClient.Disconnected += SessionOnPusherDisconnected;
		_pusherClient.Error += SessionOnPusherError;
		_pusherClient.NewOrderBook += SessionOnNewOrderBook;
		_pusherClient.NewOrderLog += SessionOnNewOrderLog;
		_pusherClient.NewTrade += SessionOnNewTrade;
	}

	private void UnsubscribePusherClient()
	{
		_pusherClient.Connected -= SessionOnPusherConnected;
		_pusherClient.Disconnected -= SessionOnPusherDisconnected;
		_pusherClient.Error -= SessionOnPusherError;
		_pusherClient.NewOrderBook -= SessionOnNewOrderBook;
		_pusherClient.NewOrderLog -= SessionOnNewOrderLog;
		_pusherClient.NewTrade -= SessionOnNewTrade;
	}

#if !NO_LICENSE
	/// <inheritdoc />
	public override string FeatureName => nameof(BitStamp);
#endif

	/// <inheritdoc />
	protected override async ValueTask OnConnectAsync(ConnectMessage connectMsg, CancellationToken cancellationToken)
	{
		if (this.IsTransactional())
		{
			if (!AuthV2 && ClientId.IsEmpty())
				throw new InvalidOperationException(LocalizedStrings.Str3835);

			if (Key.IsEmpty())
				throw new InvalidOperationException(LocalizedStrings.Str3689);

			if (Secret.IsEmpty())
				throw new InvalidOperationException(LocalizedStrings.Str3690);
		}

#if !NO_LICENSE
		var msg = await "Crypto".ValidateLicenseAsync(component: GetType(), cancellationToken: cancellationToken);
		if (!msg.IsEmpty())
		{
			msg = await nameof(BitStamp).ValidateLicenseAsync(component: GetType(), cancellationToken: cancellationToken);

			if (!msg.IsEmpty())
				throw new InvalidOperationException(msg);
		}
#endif

		if (_httpClient != null)
			throw new InvalidOperationException(LocalizedStrings.Str1619);

		if (_pusherClient != null)
			throw new InvalidOperationException(LocalizedStrings.Str1619);

		_httpClient = new HttpClient(ClientId, Key, Secret, AuthV2) { Parent = this };

		_pusherClient = new PusherClient { Parent = this };
		SubscribePusherClient();
		await _pusherClient.Connect(cancellationToken);
	}

	/// <inheritdoc />
	protected override ValueTask OnDisconnectAsync(DisconnectMessage disconnectMsg, CancellationToken cancellationToken)
	{
		if (_httpClient == null)
			throw new InvalidOperationException(LocalizedStrings.Str1856);

		if (_pusherClient == null)
			throw new InvalidOperationException(LocalizedStrings.Str1856);

		_httpClient.Dispose();
		_httpClient = null;

		_pusherClient.Disconnect();

		return default;
	}

	/// <inheritdoc />
	protected override ValueTask OnResetAsync(ResetMessage resetMsg, CancellationToken cancellationToken)
	{
		_lastMyTradeId = 0;
		_lastTimeBalanceCheck = null;

		if (_httpClient != null)
		{
			try
			{
				_httpClient.Dispose();
			}
			catch (Exception ex)
			{
				SendOutError(ex);
			}

			_httpClient = null;
		}

		if (_pusherClient != null)
		{
			try
			{
				UnsubscribePusherClient();
				_pusherClient.Disconnect();
			}
			catch (Exception ex)
			{
				SendOutError(ex);
			}

			_pusherClient = null;
		}

		SendOutMessage(new ResetMessage());

		return default;
	}

	/// <inheritdoc />
	protected override async ValueTask OnTimeMessageAsync(TimeMessage timeMsg, CancellationToken cancellationToken)
	{
		if (_orderInfo.Count > 0)
		{
			await OnOrderStatusAsync(null, cancellationToken);
			await OnPortfolioLookupAsync(null, cancellationToken);
		}
		else if (BalanceCheckInterval > TimeSpan.Zero &&
			(_lastTimeBalanceCheck == null || (CurrentTime - _lastTimeBalanceCheck) > BalanceCheckInterval))
		{
			await OnPortfolioLookupAsync(null, cancellationToken);
		}

		if (_pusherClient is not null)
			await _pusherClient.ProcessPing(cancellationToken);
	}

	private void SessionOnPusherConnected()
	{
		SendOutMessage(new ConnectMessage());
	}

	private void SessionOnPusherError(Exception exception)
	{
		SendOutError(exception);
	}

	private void SessionOnPusherDisconnected(bool expected)
	{
		SendOutDisconnectMessage(expected);
	}
}
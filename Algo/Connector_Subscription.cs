namespace StockSharp.Algo;

partial class Connector
{
	/// <summary>
	/// Find subscriptions for the specified security and data type.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <param name="dataType">Data type info.</param>
	/// <returns>Subscriptions.</returns>
	public IEnumerable<Subscription> FindSubscriptions(Security security, DataType dataType)
	{
		if (dataType is null)
			throw new ArgumentNullException(nameof(dataType));

		var secId = security.ToSecurityId();
		return Subscriptions.Where(s => s.DataType == dataType && s.SecurityId == secId);
	}

	/// <inheritdoc />
	public IEnumerable<Security> RegisteredSecurities => _subscriptionManager.GetSubscribers(DataType.Level1);

	/// <inheritdoc />
	public IEnumerable<Security> RegisteredMarketDepths => _subscriptionManager.GetSubscribers(DataType.MarketDepth);

	/// <inheritdoc />
	public IEnumerable<Security> RegisteredTrades => _subscriptionManager.GetSubscribers(DataType.Ticks);

	/// <inheritdoc />
	public IEnumerable<Security> RegisteredOrderLogs => _subscriptionManager.GetSubscribers(DataType.OrderLog);

	/// <inheritdoc />
	public IEnumerable<Portfolio> RegisteredPortfolios => _subscriptionManager.SubscribedPortfolios;

	/// <inheritdoc />
	public void RegisterPortfolio(Portfolio portfolio) => _subscriptionManager.RegisterPortfolio(portfolio);

	/// <inheritdoc />
	public void UnRegisterPortfolio(Portfolio portfolio) => _subscriptionManager.UnRegisterPortfolio(portfolio);

	/// <inheritdoc />
	public void RequestNewsStory(News news, IMessageAdapter adapter = null)
	{
		if (news is null)
			throw new ArgumentNullException(nameof(news));

		this.SubscribeMarketData(new MarketDataMessage
		{
			TransactionId = TransactionIdGenerator.GetNextId(),
			DataType2 = DataType.News,
			IsSubscribe = true,
			NewsId = news.Id.To<string>(),
			Adapter = adapter,
		});
	}

	/// <inheritdoc />
	public IEnumerable<Subscription> Subscriptions => _subscriptionManager.Subscriptions;

	/// <inheritdoc />
	public void Subscribe(Subscription subscription) => _subscriptionManager.Subscribe(subscription);

	/// <inheritdoc />
	public void UnSubscribe(Subscription subscription) => _subscriptionManager.UnSubscribe(subscription);

	/// <summary>
	/// Try get subscription by id.
	/// </summary>
	/// <param name="subscriptionId">Subscription id.</param>
	/// <returns>Subscription.</returns>
	public Subscription TryGetSubscriptionById(long subscriptionId)
		=> _subscriptionManager.TryGetSubscription(subscriptionId, true, false, null)?.Subscription;
}
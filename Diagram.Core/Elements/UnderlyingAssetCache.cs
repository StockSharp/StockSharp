namespace StockSharp.Diagram.Elements;

/// <summary>
/// Underlying assets resolved before the start, so option models are built during synchronous value processing.
/// </summary>
internal sealed class UnderlyingAssetCache
{
	private readonly Dictionary<SecurityId, Security> _assets = [];

	/// <summary>
	/// Collects the start securities of the strategy and the underlying assets of the derivatives among them.
	/// </summary>
	/// <param name="strategy"><see cref="DiagramStrategy"/></param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask"/></returns>
	public async ValueTask LoadAsync(DiagramStrategy strategy, CancellationToken cancellationToken)
	{
		if (strategy is null)
			throw new ArgumentNullException(nameof(strategy));

		_assets.Clear();

		foreach (var security in strategy.StartSecurities)
		{
			_assets.TryAdd(security.ToSecurityId(), security);

			if (security.UnderlyingSecurityId.IsEmpty())
				continue;

			if (await security.GetUnderlyingAssetAsync(strategy, cancellationToken) is { } asset)
				_assets.TryAdd(asset.ToSecurityId(), asset);
		}
	}

	/// <summary>
	/// Gets the underlying asset of the option.
	/// </summary>
	/// <param name="option">Option.</param>
	/// <returns>Underlying asset.</returns>
	/// <exception cref="InvalidOperationException">The asset was not known at the start.</exception>
	public Security Get(Security option)
	{
		if (option is null)
			throw new ArgumentNullException(nameof(option));

		return _assets.TryGetValue(option.UnderlyingSecurityId.ToSecurityId(), out var asset)
			? asset
			: throw new InvalidOperationException(LocalizedStrings.SecurityNoFound.Put(option.UnderlyingSecurityId));
	}
}

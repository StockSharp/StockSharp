namespace StockSharp.Algo.Derivatives;

/// <summary>
/// Black-Scholes "greeks".
/// </summary>
public enum BlackScholesGreeks
{
	/// <summary>
	/// Delta.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.DeltaKey)]
	Delta,

	/// <summary>
	/// Gamma.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.GammaKey)]
	Gamma,

	/// <summary>
	/// Vega.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.VegaKey)]
	Vega,

	/// <summary>
	/// Theta.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ThetaKey)]
	Theta,

	/// <summary>
	/// Rho.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.RhoKey)]
	Rho,

	/// <summary>
	/// Premium.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PremiumKey)]
	Premium,

	/// <summary>
	/// Premium.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.IVKey)]
	IV,
}
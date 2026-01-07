namespace StockSharp.Algo.Testing;

/// <summary>
/// Provides random values for market emulation.
/// </summary>
public interface IRandomProvider
{
	/// <summary>
	/// Gets next random volume for synthetic order book generation.
	/// </summary>
	/// <returns>Random volume.</returns>
	decimal NextVolume();

	/// <summary>
	/// Gets next spread step multiplier for order book generation.
	/// </summary>
	/// <param name="maxSpreadSize">Maximum spread size from settings.</param>
	/// <returns>Spread step multiplier (1 to maxSpreadSize).</returns>
	int NextSpreadStep(int maxSpreadSize);

	/// <summary>
	/// Determines whether order should be matched when processing order book changes.
	/// </summary>
	/// <returns>True if should match.</returns>
	bool ShouldMatch();

	/// <summary>
	/// Determines whether operation should fail (for failure simulation).
	/// </summary>
	/// <param name="failingPercent">Failing percentage from settings (0-100).</param>
	/// <returns>True if should fail.</returns>
	bool ShouldFail(double failingPercent);
}

/// <summary>
/// Default random provider using system Random.
/// </summary>
public class DefaultRandomProvider : IRandomProvider
{
	private readonly Random _random;

	/// <summary>
	/// Initializes a new instance with current time seed.
	/// </summary>
	public DefaultRandomProvider()
		: this(Environment.TickCount)
	{
	}

	/// <summary>
	/// Initializes a new instance with specified seed.
	/// </summary>
	/// <param name="seed">Random seed.</param>
	public DefaultRandomProvider(int seed)
	{
		_random = new Random(seed);
	}

	/// <inheritdoc />
	public decimal NextVolume() => _random.Next(10, 100);

	/// <inheritdoc />
	public int NextSpreadStep(int maxSpreadSize) => _random.Next(1, maxSpreadSize);

	/// <inheritdoc />
	public bool ShouldMatch() => _random.Next(2) == 1;

	/// <inheritdoc />
	public bool ShouldFail(double failingPercent) => _random.NextDouble() < (failingPercent / 100.0);
}

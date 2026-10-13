namespace StockSharp.Algo.Testing;

/// <summary>
/// Provides random values for market emulation.
/// </summary>
public interface IEmulationRandomizer
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
/// The decisions an emulated venue makes, drawn from a source of random values.
/// </summary>
public class DefaultEmulationRandomizer : IEmulationRandomizer
{
	private readonly IRandomProvider _random;

	/// <summary>
	/// Initializes a new instance with a source nobody holds the seed of, so runs vary.
	/// </summary>
	public DefaultEmulationRandomizer()
		: this(new DefaultRandomProvider())
	{
	}

	/// <summary>
	/// Initializes a new instance drawing from the stated point, so the run can be had again.
	/// </summary>
	/// <param name="seed">Where the series starts.</param>
	public DefaultEmulationRandomizer(int seed)
		: this(new SeededRandomProvider(seed))
	{
	}

	/// <summary>
	/// Initializes a new instance drawing from the given source.
	/// </summary>
	/// <param name="random">Source of random values.</param>
	public DefaultEmulationRandomizer(IRandomProvider random)
	{
		_random = random ?? throw new ArgumentNullException(nameof(random));
	}

	/// <inheritdoc />
	public decimal NextVolume() => _random.GetInt(10, 99);

	/// <inheritdoc />
	// A spread of one step leaves nothing to choose: the step is that one.
	public int NextSpreadStep(int maxSpreadSize) => _random.GetInt(1, maxSpreadSize > 1 ? maxSpreadSize - 1 : 1);

	/// <inheritdoc />
	public bool ShouldMatch() => _random.GetBool();

	/// <inheritdoc />
	public bool ShouldFail(double failingPercent) => _random.GetDouble() < (failingPercent / 100.0);
}

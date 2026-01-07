namespace StockSharp.Tests;

using StockSharp.Algo.Testing;

/// <summary>
/// Mock random provider for testing with configurable behavior.
/// </summary>
public class MockRandomProvider : IRandomProvider
{
	private Func<decimal> _nextVolume;
	private Func<int, int> _nextSpreadStep;
	private Func<bool> _shouldMatch;
	private Func<double, bool> _shouldFail;

	/// <summary>
	/// Function to generate volume. Default returns 50.
	/// </summary>
	public Func<decimal> NextVolumeFunc
	{
		get => _nextVolume;
		set => _nextVolume = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Function to generate spread step. Default returns 1.
	/// </summary>
	public Func<int, int> NextSpreadStepFunc
	{
		get => _nextSpreadStep;
		set => _nextSpreadStep = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Function to determine if should match. Default returns false.
	/// </summary>
	public Func<bool> ShouldMatchFunc
	{
		get => _shouldMatch;
		set => _shouldMatch = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Function to determine if should fail. Default returns false.
	/// </summary>
	public Func<double, bool> ShouldFailFunc
	{
		get => _shouldFail;
		set => _shouldFail = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Creates mock provider with default deterministic behavior.
	/// </summary>
	public MockRandomProvider()
	{
		_nextVolume = () => 50;
		_nextSpreadStep = max => 1;
		_shouldMatch = () => false;
		_shouldFail = _ => false;
	}

	/// <inheritdoc />
	public decimal NextVolume() => _nextVolume();

	/// <inheritdoc />
	public int NextSpreadStep(int maxSpreadSize) => _nextSpreadStep(maxSpreadSize);

	/// <inheritdoc />
	public bool ShouldMatch() => _shouldMatch();

	/// <inheritdoc />
	public bool ShouldFail(double failingPercent) => _shouldFail(failingPercent);
}

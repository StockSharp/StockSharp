#pragma warning disable CS0618 // equivalence tests deliberately exercise the obsolete StrategyOld engine
namespace StockSharp.Tests;

using StockSharp.Algo.Strategies;

/// <summary>
/// Focused, component-level parity tests between the monolith <see cref="StrategyOld"/> and the
/// <see cref="Strategy"/>. Each test isolates one behaviour so a divergence is pinned to a single
/// place instead of being buried inside the full-stream equivalence comparison.
/// </summary>
[TestClass]
public class StrategyDecomposedParityTests : BaseTestClass
{
	/// <summary>
	/// The monolith promotes <see cref="StrategyOld.ErrorState"/> from inside RaiseLog whenever a warning/error
	/// is logged. The decomposed strategy must behave identically; otherwise the ErrorState PropertyChanged
	/// stream diverges from the monolith's (which is exactly what desynced the full-equivalence run).
	/// </summary>
	[TestMethod]
	public void ErrorState_AfterWarningLog_MatchesBetweenImplementations()
	{
		var mono = new StrategyOld();
		var deco = new Strategy();

		AreEqual(mono.ErrorState, deco.ErrorState, "initial ErrorState must match");

		mono.LogWarning("pos manager issue");
		deco.LogWarning("pos manager issue");

		AreEqual(mono.ErrorState, deco.ErrorState, "ErrorState after a warning log must match");
	}

	/// <summary>
	/// Same contract for an error-level log: both implementations must escalate ErrorState to Error.
	/// </summary>
	[TestMethod]
	public void ErrorState_AfterErrorLog_MatchesBetweenImplementations()
	{
		var mono = new StrategyOld();
		var deco = new Strategy();

		mono.LogError("boom");
		deco.LogError("boom");

		AreEqual(mono.ErrorState, deco.ErrorState, "ErrorState after an error log must match");
	}
}

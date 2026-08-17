namespace StockSharp.Tests;

/// <summary>
/// Independence guarantees of mutable indicator message payloads.
/// </summary>
[TestClass]
public class IndicatorMessageCloneTests : BaseTestClass
{
	[TestMethod]
	public void PointClone_HasIndependentPointAndValues()
	{
		var original = new IndicatorMessage
		{
			Point = new IndicatorPoint
			{
				Time = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc),
				Values = [1m, 2m],
			},
		};

		var clone = (IndicatorMessage)original.Clone();
		clone.Point.Time = clone.Point.Time.AddMinutes(1);
		clone.Point.Values[0] = 10m;

		AreNotSame(original.Point, clone.Point);
		AreNotSame(original.Point.Values, clone.Point.Values);
		AreEqual(new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc), original.Point.Time);
		AreEqual(1m, original.Point.Values[0]);
	}

	[TestMethod]
	public void InfoClone_HasIndependentOutputNames()
	{
		var original = new IndicatorInfoMessage
		{
			OriginalTransactionId = 7,
			OutputNames = ["upper", "middle", "lower"],
		};

		var clone = (IndicatorInfoMessage)original.Clone();
		clone.OutputNames[0] = "value";

		AreNotSame(original.OutputNames, clone.OutputNames);
		AreEqual("upper", original.OutputNames[0]);
		AreEqual(7L, clone.OriginalTransactionId);
	}
}

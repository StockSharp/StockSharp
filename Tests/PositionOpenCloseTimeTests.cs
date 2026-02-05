namespace StockSharp.Tests;

[TestClass]
public class PositionOpenCloseTimeTests : BaseTestClass
{
	private static PositionChangeMessage CreateChangeMessage(decimal currentValue, DateTime serverTime)
	{
		var msg = new PositionChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			PortfolioName = "test",
			ServerTime = serverTime,
			LocalTime = serverTime,
		};

		msg.Add(PositionChangeTypes.CurrentValue, currentValue);
		return msg;
	}

	[TestMethod]
	public void PositionOpenCloseTime_Open()
	{
		var position = new Position();
		var time = DateTime.UtcNow;

		position.ApplyChanges(CreateChangeMessage(10m, time));

		position.OpenTime.AssertEqual(time);
		position.CloseTime.AssertNull();
	}

	[TestMethod]
	public void PositionOpenCloseTime_Close()
	{
		var position = new Position();
		var openTime = DateTime.UtcNow;
		var closeTime = openTime.AddMinutes(5);

		position.ApplyChanges(CreateChangeMessage(10m, openTime));
		position.ApplyChanges(CreateChangeMessage(0m, closeTime));

		position.OpenTime.AssertEqual(openTime);
		position.CloseTime.AssertEqual(closeTime);
	}

	[TestMethod]
	public void PositionOpenCloseTime_Reversal()
	{
		var position = new Position();
		var openTime = DateTime.UtcNow;
		var reversalTime = openTime.AddMinutes(5);

		position.ApplyChanges(CreateChangeMessage(10m, openTime));
		position.ApplyChanges(CreateChangeMessage(-5m, reversalTime));

		position.OpenTime.AssertEqual(reversalTime);
		position.CloseTime.AssertNull();
	}

	[TestMethod]
	public void PositionOpenCloseTime_SameDirectionUpdate()
	{
		var position = new Position();
		var openTime = DateTime.UtcNow;
		var updateTime = openTime.AddMinutes(5);

		position.ApplyChanges(CreateChangeMessage(10m, openTime));
		position.ApplyChanges(CreateChangeMessage(20m, updateTime));

		position.OpenTime.AssertEqual(openTime);
		position.CloseTime.AssertNull();
	}

	[TestMethod]
	public void PositionOpenCloseTime_CopyTo()
	{
		var position = new Position();
		var openTime = DateTime.UtcNow;

		position.ApplyChanges(CreateChangeMessage(10m, openTime));

		var clone = position.Clone();

		clone.OpenTime.AssertEqual(openTime);
		clone.CloseTime.AssertNull();
	}

	[TestMethod]
	public void PositionOpenCloseTime_ReopenAfterClose()
	{
		var position = new Position();
		var openTime = DateTime.UtcNow;
		var closeTime = openTime.AddMinutes(5);
		var reopenTime = openTime.AddMinutes(10);

		position.ApplyChanges(CreateChangeMessage(10m, openTime));
		position.ApplyChanges(CreateChangeMessage(0m, closeTime));
		position.ApplyChanges(CreateChangeMessage(5m, reopenTime));

		position.OpenTime.AssertEqual(reopenTime);
		position.CloseTime.AssertNull();
	}

	[TestMethod]
	public void PositionOpenCloseTime_NoChangeWhenSameValue()
	{
		var position = new Position();
		var openTime = DateTime.UtcNow;
		var sameTime = openTime.AddMinutes(5);

		position.ApplyChanges(CreateChangeMessage(10m, openTime));
		position.ApplyChanges(CreateChangeMessage(10m, sameTime));

		position.OpenTime.AssertEqual(openTime);
		position.CloseTime.AssertNull();
	}

	[TestMethod]
	public void PositionOpenCloseTime_ShortOpen()
	{
		var position = new Position();
		var time = DateTime.UtcNow;

		position.ApplyChanges(CreateChangeMessage(-5m, time));

		position.OpenTime.AssertEqual(time);
		position.CloseTime.AssertNull();
	}

	[TestMethod]
	public void PositionOpenCloseTime_ShortClose()
	{
		var position = new Position();
		var openTime = DateTime.UtcNow;
		var closeTime = openTime.AddMinutes(5);

		position.ApplyChanges(CreateChangeMessage(-5m, openTime));
		position.ApplyChanges(CreateChangeMessage(0m, closeTime));

		position.OpenTime.AssertEqual(openTime);
		position.CloseTime.AssertEqual(closeTime);
	}
}

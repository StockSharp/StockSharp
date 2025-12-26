namespace StockSharp.Tests;

internal abstract class TestLogReceiver : BaseLogReceiver
{
	private DateTime _timeUtc = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	protected TestLogReceiver()
	{
		Log += message => Logs.Add(message);
	}

	public List<LogMessage> Logs { get; } = [];

	public DateTime TimeUtc
	{
		get => _timeUtc;
		set => _timeUtc = value;
	}

	public override DateTime CurrentTimeUtc => _timeUtc;
}

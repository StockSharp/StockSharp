namespace StockSharp.Diagram.Elements;

/// <summary>
/// Working time verification element for a specified security.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.WorkingTimeKey,
	Description = LocalizedStrings.WorkingTimeElementKey,
	GroupName = LocalizedStrings.TimeKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/time/working_time.html")]
public class TimeCheckDiagramElement : DiagramElement
{
	private readonly DiagramSocket _outputSocket;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "51FDF3E6-36F7-454B-A706-D7B492110558".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "Clock";

	private readonly DiagramElementParam<TimeSpan?> _timeBegin;

	/// <summary>
	/// Begin time.
	/// </summary>
	public TimeSpan? TimeBegin
	{
		get => _timeBegin.Value;
		set => _timeBegin.Value = value;
	}

	private readonly DiagramElementParam<TimeSpan?> _timeEnd;

	/// <summary>
	/// End time.
	/// </summary>
	public TimeSpan? TimeEnd
	{
		get => _timeEnd.Value;
		set => _timeEnd.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TimeCheckDiagramElement"/>.
	/// </summary>
	public TimeCheckDiagramElement()
	{
		AddInput(StaticSocketIds.Time, LocalizedStrings.Time, DiagramSocketType.Any, OnProcessTime);

		_outputSocket = AddOutput(StaticSocketIds.Output, LocalizedStrings.Output, DiagramSocketType.Bool);

		_timeBegin = AddParam(nameof(TimeBegin), (TimeSpan?)null)
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Time, LocalizedStrings.From, LocalizedStrings.WorkStartTime, 150);

		_timeEnd = AddParam(nameof(TimeEnd), (TimeSpan?)null)
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Time, LocalizedStrings.Until, LocalizedStrings.WorkEndTime, 151);
	}

	private void OnProcessTime(DiagramSocketValue value)
	{
		var time = value.Time;
		var tod = time.TimeOfDay;

		if (TimeBegin is not null && tod < TimeBegin)
		{
			RaiseProcessOutput(_outputSocket, time, false, value);
			return;
		}

		if (TimeEnd is not null && tod > TimeEnd)
		{
			RaiseProcessOutput(_outputSocket, time, false, value);
			return;
		}

		RaiseProcessOutput(_outputSocket, value.Time, true, value);
	}
}
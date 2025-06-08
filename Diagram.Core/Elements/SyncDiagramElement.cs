namespace StockSharp.Diagram.Elements;

/// <summary>
/// The element used for grouping incoming values within a specified range.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.SyncKey,
	Description = LocalizedStrings.SyncElementKey,
	GroupName = LocalizedStrings.CommonKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/common/sync.html")]
public class SyncDiagramElement : DiagramElement
{
	private class RangeData(int connectedCount)
	{
		private readonly int _connectedCount = connectedCount;
		private bool _timeFrozen;
		private bool _waitAllFinished;

		public Dictionary<DiagramSocket, object> Values { get; } = [];

		public bool IsFull => Values.Count == _connectedCount;
		public bool NeedFlush { get; private set; }

		public int CandlesTotal { get; private set; }
		public int CandlesFinished { get; private set; }
		public bool NoCandles => CandlesTotal == 0;
		public bool IsAllCandlesFinished => CandlesTotal == CandlesFinished;

		public DateTimeOffset Time { get; private set; }

		public void AddValue(DiagramSocket output, DateTimeOffset time, object value)
		{
			var isNewValue = true;

			if (Values.TryGetValue(output, out var oldValue))
			{
				isNewValue = false;

				if (oldValue is ICandleMessage oldCandle && oldCandle.State == CandleStates.Finished)
				{
					// TODO
					return;
				}
			}

			Values[output] = value;

			if (value is ICandleMessage newCandle)
			{
				if (isNewValue)
					CandlesTotal++;

				if (newCandle.State == CandleStates.Finished)
					CandlesFinished++;
			}

			if (Time == default || (!_timeFrozen && Time > time))
				Time = time;

			if (_waitAllFinished)
				NeedFlush = IsAllCandlesFinished;
			else
				NeedFlush = IsFull && (NoCandles || CandlesFinished == 0 || IsAllCandlesFinished);
		}

		public void FreezeTime()
		{
			_timeFrozen = true;
			_waitAllFinished = CandlesFinished > 0;
			NeedFlush = false;
		}

		public void ClearValues()
		{
			foreach (var (s, v) in Values.ToArray())
			{
				if (v is ICandleMessage candle)
				{
					if (candle.State == CandleStates.Finished)
						continue;

					CandlesTotal--;
				}

				Values.Remove(s);
			}

			_waitAllFinished = CandlesFinished > 0;
			NeedFlush = false;
		}
	}

	private readonly SortedList<DateTimeOffset, RangeData> _ranges = [];
	private readonly Dictionary<DiagramSocket, DiagramSocket> _map = [];
	private DateTimeOffset _minTime;

	private int _connectedCount;
	private int _lastIndex;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "E569D5C4-7B9B-4F42-A190-0E454876AC79".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "Gears";

	private readonly DiagramElementParam<TimeSpan> _interval;

	/// <summary>
	/// Interval.
	/// </summary>
	public TimeSpan Interval
	{
		get => _interval.Value;
		set => _interval.Value = value;
	}

	private readonly DiagramElementParam<bool> _clearSockets;

	/// <summary>
	/// Clear socket values.
	/// </summary>
	public bool ClearSockets
	{
		get => _clearSockets.Value;
		set => _clearSockets.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SyncDiagramElement"/>.
	/// </summary>
	public SyncDiagramElement()
    {
		AddSocketPair();

		_interval = AddParam(nameof(Interval), TimeSpan.FromMinutes(1))
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Sync, LocalizedStrings.Interval, LocalizedStrings.Interval, 55)
			.SetOnValueChangingHandler((oldValue, newValue) =>
			{
				if (newValue <= TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(newValue), newValue, LocalizedStrings.IntervalMustBePositive);
			});

		_clearSockets = AddParam(nameof(ClearSockets), true)
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Sync, LocalizedStrings.ClearItems, LocalizedStrings.ClearItems, 56);
	}

	/// <inheritdoc />
	protected override void OnPrepare()
	{
		foreach (var (input, output) in _map)
		{
			if (input.IsConnected != output.IsConnected)
				throw new InvalidOperationException(LocalizedStrings.SocketPairNoConnection.Put(input.Name, output.Name));
		}

		_connectedCount = _map.Keys.Count(s => s.IsConnected);

		base.OnPrepare();
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		_ranges.Clear();
		_connectedCount = default;
		_minTime = default;

		base.OnReseted();
	}

	private void AddSocketPair()
	{
		AddSocketPair(_lastIndex + 1, DiagramSocketType.Any);
	}

	private void AddSocketPair(int index, DiagramSocketType type)
	{
		var input = AddInput($"in {index}", $"{LocalizedStrings.Input} {index}", type, OnProcess, 1, int.MaxValue, true);
		var output = AddOutput($"out {index}", $"{LocalizedStrings.Output} {index}", type, isDynamic: true);

		input.Connected += OnInputSocketConnected;

		_map.Add(input, output);
		_lastIndex = index;
	}

	private void OnProcess(DiagramSocketValue value)
	{
		var output = _map[value.Socket];

		var time = value.Time;

		if (TryGetOrCreateRange(time) is not RangeData range)
			return;

		var noCandles = range.NoCandles;
		var candlesFinished = range.CandlesFinished;

		range.AddValue(output, time, value.Value);

		var isFull = candlesFinished != range.CandlesFinished && range.IsFull;

		if (isFull || noCandles != range.NoCandles)
		{
			foreach (var (k, v) in _ranges.ToArray())
			{
				if (v == range)
					break;

				if (isFull || v.NoCandles)
					_ranges.Remove(k);
			}
		}

		ProcessRangesIfPossible();
	}

	private RangeData TryGetOrCreateRange(DateTimeOffset time)
	{
		if (_ranges.Count > 0 && time < _ranges.Keys[0])
			return null;

		var from = time.Truncate(Interval);

		if (_minTime > from)
			return null;

		return _ranges.SafeAdd(from, k => new(_connectedCount));
	}

	private void ProcessRangesIfPossible()
	{
		while (_ranges.Count > 0)
		{
			var (from, range) = _ranges.First();

			if (!range.NeedFlush)
				break;

			var time = range.Time;

			foreach (var (socket, value) in range.Values)
				RaiseProcessOutput(socket, time, value);

			Strategy.Flush(time);

			_minTime = from;

			if (_ranges.Count > 1 && (range.NoCandles || range.IsAllCandlesFinished))
			{
				_ranges.RemoveAt(0);
				continue;
			}

			if (ClearSockets)
			{
				range.ClearValues();

				if (range.Values.Count == 0 || range.Values.Count == range.CandlesFinished)
				{
					_ranges.RemoveAt(0);
					continue;
				}
			}
			else
				range.FreezeTime();

			break;
		}
	}

	private void OnInputSocketConnected(DiagramSocket target, DiagramSocket source)
	{
		if (IsUndoRedoing)
			return;

		var output = _map[target];

		using var _ = SaveUndoState();

		target.Type = source.Type;
		target.IsDynamic = true;

		output.Type = source.Type;

		var lastInput = InputSockets.LastOrDefault();

		if (lastInput is null || lastInput.IsConnected || _map[lastInput].IsConnected)
			AddSocketPair();
	}

	private const string _inKey = "in";

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		foreach (var (input, output) in _map.CopyAndClear())
		{
			RemoveSocket(input);
			RemoveSocket(output);
		}

		var inSockets = storage.GetValue<SettingsStorage[]>(_inKey);

		if (inSockets is not null)
		{
			foreach (var ss in inSockets)
			{
				var id = ss.GetValue<string>(nameof(DiagramSocket.Id));
				var type = DiagramSocketType.GetSocketType(ss.GetValue<string>(nameof(DiagramSocketType.Type)).To<Type>());

				var index = id.Split(' ').Last().To<int>();
				AddSocketPair(index, type);
			}
		}
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.Set(_inKey, _map
			.Select(p => p.Key)
			.Select(s => new SettingsStorage()
				.Set(nameof(s.Id), s.Id)
				.Set(nameof(s.Type), s.Type.Type.GetTypeAsString(false))
			).ToArray());
	}
}
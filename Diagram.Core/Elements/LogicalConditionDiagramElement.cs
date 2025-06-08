namespace StockSharp.Diagram.Elements;

/// <summary>
/// Logical condition element.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.LogicalConditionKey,
	Description = LocalizedStrings.LogicalConditionElementDescriptionKey,
	GroupName = LocalizedStrings.CommonKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html")]
public class LogicalConditionDiagramElement : DiagramElement
{
	/// <summary>
	/// The logical condition type.
	/// </summary>
	public enum Condition
	{
		/// <summary>
		/// AND.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ANDKey)]
		And,

		/// <summary>
		/// OR.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ORKey)]
		Or,

		/// <summary>
		/// Exclusive OR.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.XORKey)]
		Xor,

		/// <summary>
		/// NOT.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.NOTKey)]
		Not,
	}

	private readonly DiagramSocket _outputSocket;
	private readonly HashSet<string> _loadedSockets = new(StringComparer.InvariantCultureIgnoreCase);

	/// <inheritdoc />
	public override Guid TypeId { get; } = "DED60960-7595-461D-997F-8F6287ADEC2E".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "Workflow";

	private readonly DiagramElementParam<Condition?> _operator;

	/// <summary>
	/// Operator.
	/// </summary>
	public Condition? Operator
	{
		get => _operator.Value;
		set => _operator.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="LogicalConditionDiagramElement"/>.
	/// </summary>
	public LogicalConditionDiagramElement()
	{
		_outputSocket = AddOutput(StaticSocketIds.Signal, LocalizedStrings.Signal, DiagramSocketType.Bool);

		_operator = AddParam<Condition?>(nameof(Operator))
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Condition, LocalizedStrings.Operator, LocalizedStrings.LogicalConditionDesc, 20)
			.SetOnValueChangedHandler(value =>
			{
				SetElementName(value?.GetDisplayName());
				UpdateSockets();
			});

		UpdateSockets();
	}

	/// <inheritdoc />
	protected override bool WaitAllInput => Operator != Condition.Or;

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);
		storage.SetValue(InputSocketIds, InputSockets.Select(s => s.Id).ToArray());
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		RemoveSockets(false);

		_loadedSockets.Clear();
		_loadedSockets.AddRange(storage.GetValue(InputSocketIds, Array.Empty<string>()));

		UpdateSockets();
	}

	private const string InputSocketIds = nameof(InputSocketIds);

	private void UpdateSockets()
	{
		int minSockets, maxSockets;
		var needEmpty = false;

		switch (_operator.Value)
		{
			case Condition.And:
			case Condition.Or:
				minSockets = 2;
				maxSockets = int.MaxValue;
				needEmpty = true;
				break;
			case Condition.Xor:
				minSockets = maxSockets = 2;
				break;
			case Condition.Not:
				minSockets = maxSockets = 1;
				break;
			case null:
				minSockets = maxSockets = 0;
				break;
			default:
				throw new InvalidOperationException(_operator.Value.To<string>());
		}

		minSockets = minSockets.Max(_loadedSockets.Count);

		if (_loadedSockets.Count > 0)
			InputSockets.ToArray().Where(s => !_loadedSockets.Contains(s.Id) && GetNumConnections(s) == 0).ForEach(RemoveSocket);

		while (InputSockets.Count > maxSockets)
		{
			var s = InputSockets.FirstOrDefault(s1 => GetNumConnections(s1) == 0);
			if (s != null)
			{
				RemoveSocket(s);
				continue;
			}

			RemoveSocket(InputSockets.Last());
		}

		if (needEmpty && InputSockets.All(s => GetNumConnections(s) > 0) && minSockets <= InputSockets.Count)
			++minSockets;

		var needIds = new HashSet<string>(_loadedSockets, StringComparer.InvariantCultureIgnoreCase);
		while (InputSockets.Count < minSockets)
		{
			string nextId;
			if (needIds.Count > 0)
			{
				nextId = needIds.First();
				needIds.Remove(nextId);

				if (InputSockets.FindById(nextId) != null)
					continue;
			}
			else
			{
				nextId = Guid.NewGuid().ToN();
			}

			AddSocket($"{LocalizedStrings.Input} {InputSockets.Count + 1}", nextId);
		}
	}

	/// <inheritdoc />
	protected override void OnProcess(DateTimeOffset time, IDictionary<DiagramSocket, DiagramSocketValue> values, DiagramSocketValue source)
	{
		var bools = values.Select(p => p.Value.GetValue<bool>()).ToArray();

		if (bools.Length == 0)
			return;

		bool result;

		switch (Operator)
		{
			case Condition.And:
				if (bools.Length < 2)
					return;

				result = bools.All(v => v);
				break;

			case Condition.Or:
				result = bools.Any(v => v);
				break;

			case Condition.Xor:
				if (bools.Length < 2)
					return;

				result = bools.Aggregate((a, b) => a ^ b);
				break;

			case Condition.Not:
				result = !bools[0];
				break;

			default:
				throw new ArgumentOutOfRangeException(Operator.To<string>());
		}

		RaiseProcessOutput(_outputSocket, time, result, source);
	}

	private void AddSocket(string fullName, string socketId)
	{
		var socket = AddInput(socketId, fullName, DiagramSocketType.Bool);

		socket.Connected += OnInputSocketConnected;
		socket.Disconnected += OnInputSocketDisconnected;
	}

	private void OnInputSocketConnected(DiagramSocket socket, DiagramSocket source)
	{
		if (IsUndoRedoing)
			return;

		using var _ = SaveUndoState();

		UpdateSockets();
	}

	private void OnInputSocketDisconnected(DiagramSocket socket, DiagramSocket source)
	{
		if (socket.IsOutput || IsUndoRedoing)
			return;

		using var _ = SaveUndoState();

		_loadedSockets.Remove(socket.Id);
		RemoveSocket(socket);
		UpdateSockets();
	}
}
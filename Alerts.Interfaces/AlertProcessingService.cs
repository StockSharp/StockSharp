namespace StockSharp.Alerts;

using System.Threading.Channels;

/// <summary>
/// Alert processing service.
/// </summary>
public class AlertProcessingService : BaseLogReceiver, IAlertProcessingService
{
	private readonly CachedSynchronizedDictionary<Type, CachedSynchronizedSet<AlertSchema>> _schemas = [];
	private readonly Channel<Message> _channel;
	private readonly CancellationTokenSource _cts = new();
	private long _droppedMessages;

	// Schemas whose alert reached the user, and which therefore stop matching.
	private readonly SynchronizedSet<AlertSchema> _delivered = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="AlertProcessingService"/>.
	/// </summary>
	/// <param name="maxQueue">Max queue for process.</param>
	public AlertProcessingService(int maxQueue)
	{
		_channel = Channel.CreateBounded<Message>(new BoundedChannelOptions(maxQueue)
		{
			FullMode = BoundedChannelFullMode.Wait,
			SingleReader = true,
		});

		var messages = _channel.Reader;
		var token = _cts.Token;

		Task.Run(async () =>
		{
			try
			{
				await foreach (var message in messages.ReadAllAsync(token))
				{
					if (!_schemas.TryGetValue(message.GetType(), out var schemas))
						continue;

					foreach (var schema in schemas.Cache)
					{
						if (!schema.IsEnabled || schema.AlertType is null || _delivered.Contains(schema))
							continue;

						bool isMatch;

						try
						{
							isMatch = IsMatch(schema, message);
						}
						catch (Exception ex)
						{
							// a schema that cannot be evaluated says nothing about the ones next to it.
							LogError(ex);
							continue;
						}

						if (!isMatch)
							continue;

						try
						{
							await AlertServicesRegistry.NotificationService.NotifyAsync(schema.AlertType.Value,
								schema.ExternalId, schema.LogLevel, schema.Caption, schema.Message, message.LocalTime, token);
							_delivered.Add(schema);
						}
						catch (Exception ex)
						{
							if (token.IsCancellationRequested)
								break;

							// A refused delivery leaves the schema eligible for the next matching message.
							LogError(ex);
						}
					}
				}
			}
			catch (Exception ex)
			{
				if (!token.IsCancellationRequested)
					LogError(ex);
			}
		}, token);
	}

	private static bool IsMatch(AlertSchema schema, Message message)
	{
		return schema.Rules.All(rule =>
		{
			var field = rule.Field;

			var value = field.Invoke(message);

			if (value == null)
				return false;

			int Compare() => field.ValueType.GetOperator().Compare(value, rule.Value);

			return rule.Operator switch
			{
				ComparisonOperator.Equal =>				rule.Value.Equals(value),
				ComparisonOperator.NotEqual =>			!rule.Value.Equals(value),

				ComparisonOperator.Greater =>			Compare() > 0,
				ComparisonOperator.GreaterOrEqual =>	Compare() >= 0,
				ComparisonOperator.Less =>				Compare() < 0,
				ComparisonOperator.LessOrEqual =>		Compare() <= 0,

				ComparisonOperator.Any => true,
				_ => throw new ArgumentOutOfRangeException(nameof(rule), rule.Operator.ToString()),
			};
		});
	}

	/// <inheritdoc />
	public event Action<AlertSchema> Registered;

	/// <inheritdoc />
	public event Action<AlertSchema> UnRegistered;

	/// <summary>
	/// Number of messages dropped because the processing queue was full.
	/// </summary>
	public long DroppedMessages => Interlocked.Read(ref _droppedMessages);

	/// <inheritdoc />
	public void Register(AlertSchema schema)
	{
		if (schema == null)
			throw new ArgumentNullException(nameof(schema));

		var schemas = _schemas.SafeAdd(schema.MessageType);

		if (schemas.TryAdd(schema))
			Registered?.Invoke(schema);
	}

	void IAlertProcessingService.UnRegister(AlertSchema schema)
	{
		if (schema == null)
			throw new ArgumentNullException(nameof(schema));

		if (_schemas.TryGetValue(schema.MessageType, out var schemas) && schemas.Remove(schema))
			UnRegistered?.Invoke(schema);

		_delivered.Remove(schema);
	}

	void IAlertProcessingService.Process(Message message)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		if (_channel.Writer.TryWrite(message) || _cts.IsCancellationRequested)
			return;

		if (Interlocked.Increment(ref _droppedMessages) == 1)
			LogWarning("Alert processing queue is full. Messages are being dropped.");
	}

	/// <inheritdoc />
	public IEnumerable<AlertSchema> Schemas
		=> _schemas.CachedValues.SelectMany(v => v.Cache);

	AlertSchema IAlertProcessingService.FindSchema(Guid id)
		=>	_schemas
			.CachedValues
			.SelectMany(v => v.Cache)
			.FirstOrDefault(v => v.Id == id);

	void IPersistable.Load(SettingsStorage storage)
	{
		_schemas.Clear();
		_delivered.Clear();

		foreach (var schemaSettings in storage.GetValue<IEnumerable<SettingsStorage>>(nameof(Schemas)))
			Register(schemaSettings.Load<AlertSchema>());
	}

	void IPersistable.Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(Schemas), Schemas
			.Select(s => s.Save())
			.ToArray());
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		_cts.Cancel();

		_channel.Writer.TryComplete();

		base.DisposeManaged();
	}
}

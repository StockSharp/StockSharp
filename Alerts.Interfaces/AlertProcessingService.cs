namespace StockSharp.Alerts;

using System.Threading.Channels;

/// <summary>
/// Alert processing service.
/// </summary>
public class AlertProcessingService : BaseLogReceiver, IAlertProcessingService
{
	private readonly CachedSynchronizedDictionary<Type, CachedSynchronizedSet<AlertSchema>> _schemas = [];
	private readonly Channel<Message> _channel;
	private readonly Channel<(AlertSchema schema, DateTime time)> _deliveries = Channel.CreateUnbounded<(AlertSchema, DateTime)>();
	private readonly CancellationTokenSource _cts = new();

	// schemas whose alert reached the user, and which therefore stop matching.
	private readonly SynchronizedSet<AlertSchema> _delivered = [];

	// schemas handed to delivery and not answered yet, so one match is not queued twice.
	private readonly SynchronizedSet<AlertSchema> _delivering = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="AlertProcessingService"/>.
	/// </summary>
	/// <param name="maxQueue">Max queue for process.</param>
	public AlertProcessingService(int maxQueue)
	{
		_channel = Channel.CreateBounded<Message>(maxQueue);

		var messages = _channel.Reader;
		var deliveries = _deliveries.Reader;
		var toDeliver = _deliveries.Writer;
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

						if (isMatch && _delivering.TryAdd(schema))
							await toDeliver.WriteAsync((schema, message.LocalTime), token);
					}
				}
			}
			catch (Exception ex)
			{
				if (!token.IsCancellationRequested)
					LogError(ex);
			}
		}, token);

		Task.Run(async () =>
		{
			try
			{
				await foreach (var (schema, time) in deliveries.ReadAllAsync(token))
				{
					try
					{
						await AlertServicesRegistry.NotificationService.NotifyAsync(schema.AlertType.Value, schema.ExternalId, schema.LogLevel, schema.Caption, schema.Message, time, token);

						_delivered.Add(schema);
					}
					catch (Exception ex)
					{
						if (token.IsCancellationRequested)
							break;

						// a refused delivery told the user nothing, so the schema stays owed and the
						// next message matching it carries the alert again.
						LogError(ex);
					}
					finally
					{
						_delivering.Remove(schema);
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
		_delivering.Remove(schema);
	}

	void IAlertProcessingService.Process(Message message)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		// the caller is told nothing back, so an accepted message is never dropped: a full queue
		// makes the message wait for room instead, in the order the messages arrived.
		if (!_channel.Writer.TryWrite(message))
			_ = EnqueueAsync(message);
	}

	private async Task EnqueueAsync(Message message)
	{
		try
		{
			await _channel.Writer.WriteAsync(message, _cts.Token);
		}
		catch (Exception ex)
		{
			if (!_cts.IsCancellationRequested)
				LogError(ex);
		}
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
		_delivering.Clear();

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

		// a producer waiting for room is released by the completion rather than by an exception.
		_channel.Writer.TryComplete();
		_deliveries.Writer.TryComplete();

		base.DisposeManaged();
	}
}

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
	private readonly SynchronizedSet<AlertSchema> _activated = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="AlertProcessingService"/>.
	/// </summary>
	/// <param name="maxQueue">Max queue for process.</param>
	public AlertProcessingService(int maxQueue)
	{
		_channel = Channel.CreateBounded<Message>(maxQueue);
		var reader = _channel.Reader;
		var token = _cts.Token;

		Task.Run(async () =>
		{
			try
			{
				const int maxErrors = 5;
				var errorCount = 0;

				await foreach (var message in reader.ReadAllAsync(token))
				{
					if (!_schemas.TryGetValue(message.GetType(), out var schemas))
						continue;

					foreach (var schema in schemas.Cache.Where(s => s.IsEnabled && !_activated.Contains(s)))
					{
						var type = schema.AlertType;

						if (type is null)
							continue;

						var canAlert = schema.Rules.All(rule =>
						{
							var field = rule.Field;

							var value = field.Invoke(message);

							if (value == null)
								return false;

							var valueType = field.ValueType;

							return rule.Operator switch
							{
								ComparisonOperator.Equal =>				rule.Value.Equals(value),
								ComparisonOperator.NotEqual =>			!rule.Value.Equals(value),

								ComparisonOperator.Greater =>			valueType.GetOperator().Compare(rule.Value, value) == 1,
								ComparisonOperator.GreaterOrEqual =>	valueType.GetOperator().Compare(rule.Value, value) <= 0,
								ComparisonOperator.Less =>				valueType.GetOperator().Compare(rule.Value, value) == -1,
								ComparisonOperator.LessOrEqual =>		valueType.GetOperator().Compare(rule.Value, value) >= 0,

								ComparisonOperator.Any => true,
								_ => throw new ArgumentOutOfRangeException(nameof(rule), rule.Operator.ToString()),
							};
						});

						if (canAlert && _activated.TryAdd(schema))
						{
							try
							{
								await AlertServicesRegistry.NotificationService.NotifyAsync(type.Value, schema.ExternalId, schema.LogLevel, schema.Caption, schema.Message, message.LocalTime, token);
								errorCount = 0;
							}
							catch (Exception ex)
							{
								if (token.IsCancellationRequested)
									break;

								LogError(ex);

								if (++errorCount >= maxErrors)
									break;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				if (!token.IsCancellationRequested)
					LogError(ex);
			}
		}, _cts.Token);
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

		_activated.Remove(schema);
	}

	void IAlertProcessingService.Process(Message message)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		_channel.Writer.TryWrite(message);
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
		base.DisposeManaged();
	}
}

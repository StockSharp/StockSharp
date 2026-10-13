namespace StockSharp.Alerts;

/// <summary>
/// Schema.
/// </summary>
public class AlertSchema : IAsyncPersistable
{
	/// <summary>
	/// Initializes a new instance of the <see cref="AlertSchema"/>.
	/// </summary>
	public AlertSchema()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AlertSchema"/>.
	/// </summary>
	/// <param name="messageType">Message type.</param>
	public AlertSchema(Type messageType)
	{
		MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
	}

	/// <summary>
	/// Identifier.
	/// </summary>
	public Guid Id { get; private set; } = Guid.NewGuid();

	/// <summary>
	/// Enabled.
	/// </summary>
	public bool IsEnabled { get; set; } = true;

	/// <summary>
	/// Message type.
	/// </summary>
	public Type MessageType { get; private set; }

	/// <summary>
	/// Rules.
	/// </summary>
	public IList<AlertRule> Rules { get; } = [];

	/// <summary>
	/// Alert type.
	/// </summary>
	public AlertNotifications? AlertType { get; set; }

	/// <summary>
	/// External ID.
	/// </summary>
	public long? ExternalId { get; set; }

	/// <summary>
	/// Signal header.
	/// </summary>
	public string Caption { get; set; }

	/// <summary>
	/// Alert text.
	/// </summary>
	public string Message { get; set; }

	/// <summary>
	/// <see cref="LogLevels"/>
	/// </summary>
	public LogLevels LogLevel { get; set; } = LogLevels.Info;

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="Task"/></returns>
	public async Task LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		Rules.Clear();

		foreach (var settings in storage.GetValue<SettingsStorage[]>(nameof(Rules)))
		{
			var rule = await settings.LoadAsync<AlertRule>(cancellationToken);

			if (rule.Value != null)
				Rules.Add(rule);
		}

		var alertType = storage.GetValue<string>(nameof(AlertType));

        if (alertType == "Sms" || alertType == "Email" || alertType == "Speech")
			alertType = string.Empty;

        AlertType = alertType.To<AlertNotifications?>();
		ExternalId = storage.GetValue<long?>(nameof(ExternalId));
		Caption = storage.GetValue<string>(nameof(Caption));
		Message = storage.GetValue<string>(nameof(Message));
		IsEnabled = storage.GetValue(nameof(IsEnabled), IsEnabled);
		Id = storage.GetValue<Guid>(nameof(Id));
		MessageType = storage.GetValue<string>(nameof(MessageType)).To<Type>();
		LogLevel = storage.GetValue(nameof(LogLevel), LogLevel);
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="Task"/></returns>
	public async Task SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		var rules = new List<SettingsStorage>();

		foreach (var rule in Rules)
			rules.Add(await rule.SaveAsync(cancellationToken));

		storage
			.Set(nameof(Rules), rules.ToArray())
			.Set(nameof(AlertType), AlertType.To<string>())
			.Set(nameof(ExternalId), ExternalId)
			.Set(nameof(Caption), Caption)
			.Set(nameof(Message), Message)
			.Set(nameof(IsEnabled), IsEnabled)
			.Set(nameof(Id), Id)
			.Set(nameof(MessageType), MessageType?.GetTypeName(false))
			.Set(nameof(LogLevel), LogLevel)
		;
	}
}
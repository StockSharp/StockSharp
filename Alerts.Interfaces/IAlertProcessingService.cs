namespace StockSharp.Alerts;

/// <summary>
/// Defines an alert processing service.
/// </summary>
public interface IAlertProcessingService : IPersistable, ILogSource
{
	/// <summary>
	/// All schemas.
	/// </summary>
	IEnumerable<AlertSchema> Schemas { get; }

	/// <summary>
	/// Schema registration event.
	/// </summary>
	event Action<AlertSchema> Registered;

	/// <summary>
	/// Schema unregistering event.
	/// </summary>
	event Action<AlertSchema> UnRegistered;

	/// <summary>
	/// Register schema.
	/// </summary>
	/// <param name="schema">Schema.</param>
	void Register(AlertSchema schema);

	/// <summary>
	/// Remove previously registered by <see cref="Register"/> schema.
	/// </summary>
	/// <param name="schema">Schema.</param>
	void UnRegister(AlertSchema schema);

	/// <summary>
	/// Check message on alert conditions.
	/// </summary>
	/// <param name="message">Message.</param>
	void Process(Message message);

	/// <summary>
	/// Find schema by the specified identifier.
	/// </summary>
	/// <param name="id">The identifier.</param>
	/// <returns>Found schema. <see langword="null"/> if schema with the specified identifier doesn't exist.</returns>
	AlertSchema FindSchema(Guid id);
}
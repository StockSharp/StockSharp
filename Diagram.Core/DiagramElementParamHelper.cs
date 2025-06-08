namespace StockSharp.Diagram;

/// <summary>
/// Extension class for <see cref="IDiagramElementParam"/>.
/// </summary>
public static class DiagramElementParamHelper
{
	/// <summary>
	/// To set the handler at the start of the value change for the diagram element parameter.
	/// </summary>
	/// <typeparam name="TValue">The diagram element parameter type.</typeparam>
	/// <param name="param">The diagram element parameter.</param>
	/// <param name="handler">The handler.</param>
	/// <returns>The diagram element parameter.</returns>
	public static DiagramElementParam<TValue> SetOnValueChangingHandler<TValue>(this DiagramElementParam<TValue> param, Action<TValue, TValue> handler)
	{
		if (param == null)
			throw new ArgumentNullException(nameof(param));

		if (handler == null)
			throw new ArgumentNullException(nameof(handler));

		param.ValueChanging += handler;

		return param;
	}

	/// <summary>
	/// To set the handler to the value change for the diagram element parameter.
	/// </summary>
	/// <typeparam name="TValue">The diagram element parameter type.</typeparam>
	/// <param name="param">The diagram element parameter.</param>
	/// <param name="handler">The handler of the diagram element value change.</param>
	/// <returns>The diagram element parameter.</returns>
	public static DiagramElementParam<TValue> SetOnValueChangedHandler<TValue>(this DiagramElementParam<TValue> param, Action<TValue> handler)
	{
		if (param == null)
			throw new ArgumentNullException(nameof(param));

		if (handler == null)
			throw new ArgumentNullException(nameof(handler));

		param.ValueChanged += handler;

		return param;
	}

	/// <summary>
	/// To set the handler of saving/loading for the diagram element parameter.
	/// </summary>
	/// <typeparam name="TValue">The diagram element parameter type.</typeparam>
	/// <param name="param">The diagram element parameter.</param>
	/// <param name="save">The handler for the parameter saving.</param>
	/// <param name="load">The handler for the parameter loading.</param>
	/// <returns>The diagram element parameter.</returns>
	public static DiagramElementParam<TValue> SetSaveLoadHandlers<TValue>(this DiagramElementParam<TValue> param, Func<TValue, SettingsStorage> save, Func<SettingsStorage, TValue> load)
	{
		if (param == null) 
			throw new ArgumentNullException(nameof(param));

		param.SaveHandler = save ?? throw new ArgumentNullException(nameof(save));
		param.LoadHandler = load ?? throw new ArgumentNullException(nameof(load));

		return param;
	}
}
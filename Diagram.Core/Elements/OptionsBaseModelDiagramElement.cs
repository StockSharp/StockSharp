namespace StockSharp.Diagram.Elements;

/// <summary>
/// Base class for options elements based on <see cref="IBlackScholes"/> model.
/// </summary>
/// <typeparam name="TModel"><see cref="IBlackScholes"/> implementation.</typeparam>
public abstract class OptionsBaseModelDiagramElement<TModel> : DiagramElement
	where TModel : class, IBlackScholes
{
	/// <summary>
	/// Initializes a new instance of the <see cref="OptionsBaseModelDiagramElement{TModel}"/>.
	/// </summary>
	protected OptionsBaseModelDiagramElement()
	{
		AddInput(StaticSocketIds.BlackScholes, LocalizedStrings.Model, DiagramSocketType.BlackScholes, ProcessModel);
	}

	/// <summary>
	/// <see cref="IBlackScholes"/>.
	/// </summary>
	protected TModel Model { get; private set; }

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		Model = null;
	}

	/// <summary>
	/// Process model socket.
	/// </summary>
	/// <param name="value"><see cref="DiagramSocketValue"/>.</param>
	protected virtual void ProcessModel(DiagramSocketValue value)
	{
		Model = (TModel)value.GetValue<IBlackScholes>();
	}

	/// <summary>
	/// <see cref="Model"/> changed.
	/// </summary>
	/// <param name="time">Time.</param>
	protected virtual void ProcessModel(DateTimeOffset time) { }
}
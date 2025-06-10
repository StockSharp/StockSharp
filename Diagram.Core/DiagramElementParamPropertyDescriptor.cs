namespace StockSharp.Diagram;

class DiagramElementParamPropertyDescriptor : NamedPropertyDescriptor
{
	private readonly IDiagramElementParam _param;

	public DiagramElementParamPropertyDescriptor(IDiagramElementParam param)
		: base(param.CheckOnNull(nameof(param)).Name, [.. param.Attributes])
	{
		_param = param;
	}

	public override bool CanResetValue(object component)
	{
		return false;
	}

	public override object GetValue(object component)
	{
		return _param.Value;
	}

	public override void ResetValue(object component)
	{
	}

	public override void SetValue(object component, object value)
	{
		_param.Value = value;
	}

	public override bool ShouldSerializeValue(object component)
	{
		return false;
	}

	public override Type ComponentType => _param.Type;

	public override bool IsReadOnly => _param.IsReadOnly();

	public override Type PropertyType => _param.Type;
}

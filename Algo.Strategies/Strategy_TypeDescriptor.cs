namespace StockSharp.Algo.Strategies;

partial class Strategy
{
	private class StrategyParamPropDescriptor(IStrategyParam param) : NamedPropertyDescriptor(param.Id, [.. param.Attributes])
	{
		public override Type ComponentType => typeof(Strategy);
		public override bool IsReadOnly => false;
		public override Type PropertyType => param.Type;

		public override object GetValue(object component) => param.Value;
		public override void SetValue(object component, object value) => param.Value = value;

		public override bool CanResetValue(object component) => false;
		public override void ResetValue(object component) => throw new NotSupportedException();
		public override bool ShouldSerializeValue(object component) => false;
	}

	/// <summary>
	/// Get parameters.
	/// </summary>
	/// <returns>Parameters.</returns>
	public virtual IStrategyParam[] GetParameters() => Parameters.CachedValues;

	AttributeCollection ICustomTypeDescriptor.GetAttributes() => TypeDescriptor.GetAttributes(this, true);

	string ICustomTypeDescriptor.GetClassName() => TypeDescriptor.GetClassName(this, true);
	string ICustomTypeDescriptor.GetComponentName() => TypeDescriptor.GetComponentName(this, true);
	TypeConverter ICustomTypeDescriptor.GetConverter() => TypeDescriptor.GetConverter(this, true);
	object ICustomTypeDescriptor.GetEditor(Type editorBaseType) => TypeDescriptor.GetEditor(this, editorBaseType, true);
	object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd) => this;

	EventDescriptor ICustomTypeDescriptor.GetDefaultEvent() => TypeDescriptor.GetDefaultEvent(this, true);
	EventDescriptorCollection ICustomTypeDescriptor.GetEvents() => TypeDescriptor.GetEvents(this, true);
	EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes) => TypeDescriptor.GetEvents(this, attributes, true);

	PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty() => ((ICustomTypeDescriptor)this).GetProperties().TryGetDefault(GetType());
	PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties() => new([.. GetParameters().Select(p => new StrategyParamPropDescriptor(p))]);
	PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes) => this.GetFilteredProperties(attributes);
}
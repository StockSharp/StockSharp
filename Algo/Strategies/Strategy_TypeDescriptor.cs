namespace StockSharp.Algo.Strategies;

partial class Strategy
{
	private class StrategyParamPropDescriptor(IStrategyParam param) : PropertyDescriptor(param.Id, [])
	{
		public override Type ComponentType => typeof(Strategy);
		public override bool IsReadOnly => false;
		public override Type PropertyType => param.Type;
		public override string DisplayName => param.Name;
		public override string Description => param.Description;
		public override string Category => param.Category;
		public override bool IsBrowsable => param.IsBrowsable;
		public override AttributeCollection Attributes { get; } = new(param.IsBrowsable ? [new BrowsableAttribute(true)] : []);

		public override object GetValue(object component) => param.Value;
		public override void SetValue(object component, object value) => param.Value = value;

		public override bool CanResetValue(object component) => false;
		public override void ResetValue(object component) => throw new NotSupportedException();
		public override bool ShouldSerializeValue(object component) => false;

		public override string ToString() => DisplayName;
	}

	AttributeCollection ICustomTypeDescriptor.GetAttributes() => TypeDescriptor.GetAttributes(this, true);

	string ICustomTypeDescriptor.GetClassName() => TypeDescriptor.GetClassName(this, true);
	string ICustomTypeDescriptor.GetComponentName() => TypeDescriptor.GetComponentName(this, true);
	TypeConverter ICustomTypeDescriptor.GetConverter() => TypeDescriptor.GetConverter(this, true);
	object ICustomTypeDescriptor.GetEditor(Type editorBaseType) => TypeDescriptor.GetEditor(this, editorBaseType, true);
	object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd) => this;

	EventDescriptor ICustomTypeDescriptor.GetDefaultEvent() => TypeDescriptor.GetDefaultEvent(this, true);
	EventDescriptorCollection ICustomTypeDescriptor.GetEvents() => new([]);
	EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes) => new([]);
	
	PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty() => ((ICustomTypeDescriptor)this).GetProperties().Typed().FirstOrDefault();
	PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties() => new(Parameters.CachedValues.Select(p => new StrategyParamPropDescriptor(p)).ToArray());
	PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
	{
		var allProperties = ((ICustomTypeDescriptor)this).GetProperties();

		if (attributes == null || attributes.Length == 0)
			return allProperties;

		return new(allProperties.Typed().Where(p => p.Attributes.Cast<Attribute>().Any(a => attributes.Any(attr => attr.Match(a)))).ToArray());
	}
}
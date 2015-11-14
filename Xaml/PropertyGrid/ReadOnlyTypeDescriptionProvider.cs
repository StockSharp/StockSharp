namespace StockSharp.Xaml.PropertyGrid
{
	using System;
	using System.ComponentModel;
	using System.Linq;

	class ReadOnlyTypeDescriptionProvider : TypeDescriptionProvider
	{
		sealed class ReadOnlyPropertyDescriptor : PropertyDescriptor
		{
			readonly PropertyDescriptor _propDesc;

			public ReadOnlyPropertyDescriptor(PropertyDescriptor propDesc)
				: base(propDesc)
			{
				_propDesc = propDesc;
			}

			public override string Category => _propDesc.Category;

			public override string DisplayName => _propDesc.DisplayName;

			public override Type ComponentType => _propDesc.ComponentType;

			public override bool IsReadOnly => true;

			public override Type PropertyType => _propDesc.PropertyType;

			public override AttributeCollection Attributes
			{
				get
				{
					var array = base
						.Attributes
						.Cast<Attribute>()
						.Where(a => !(a is EditorAttribute))
						.ToArray();

					return new AttributeCollection(array);
				}
			}

			public override bool CanResetValue(object component)
			{
				return _propDesc.CanResetValue(component);
			}

			public override object GetValue(object component)
			{
				return _propDesc.GetValue(component);
			}

			public override void ResetValue(object component)
			{
				_propDesc.ResetValue(component);
			}

			public override void SetValue(object component, object value)
			{
				_propDesc.SetValue(component, value);
			}

			public override bool ShouldSerializeValue(object component)
			{
				return _propDesc.ShouldSerializeValue(component);
			}
		}

		sealed class ReadOnlyTypeDescriptor : CustomTypeDescriptor
		{
			public ReadOnlyTypeDescriptor(ICustomTypeDescriptor parent)
				: base(parent)
			{
			}

			public override PropertyDescriptorCollection GetProperties()
			{
				var array = base
					.GetProperties()
					.Cast<PropertyDescriptor>()
					.Select(p => (PropertyDescriptor)new ReadOnlyPropertyDescriptor(p))
					.ToArray();

				return new PropertyDescriptorCollection(array);
			}
		}

		public ReadOnlyTypeDescriptionProvider()
			: base(TypeDescriptor.GetProvider(typeof(object)))
		{
		}

		public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
		{
			return new ReadOnlyTypeDescriptor(base.GetTypeDescriptor(objectType, instance));
		}
	}
}
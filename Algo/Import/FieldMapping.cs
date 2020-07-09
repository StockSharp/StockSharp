namespace StockSharp.Algo.Import
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	/// <summary>
	/// Importing field description.
	/// </summary>
	public abstract class FieldMapping : NotifiableObject, IPersistable, ICloneable
	{
		private FastDateTimeParser _dateParser;
		private FastTimeSpanParser _timeParser;

		private readonly HashSet<string> _enumNames = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

		/// <summary>
		/// Initializes a new instance of the <see cref="FieldMapping"/>.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="displayName">Display name.</param>
		/// <param name="description">Description.</param>
		/// <param name="type">Field type.</param>
		protected FieldMapping(string name, string displayName, string description, Type type)
		{
			//if (settings == null)
			//	throw new ArgumentNullException(nameof(settings));

			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));
				
			if (displayName.IsEmpty())
				throw new ArgumentNullException(nameof(displayName));

			if (description.IsEmpty())
				description = displayName;

			Type = type ?? throw new ArgumentNullException(nameof(type));
			Name = name;
			DisplayName = displayName;
			Description = description;
			IsEnabled = true;

			//Number = -1;

			if (Type.IsDateTime())
				Format = "yyyyMMdd";
			else if (Type == typeof(TimeSpan))
				Format = "hh:mm:ss";

			if (Type.IsEnum)
				_enumNames.AddRange(Type.GetNames());
		}

		/// <summary>
		/// Name.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Is field extended.
		/// </summary>
		public bool IsExtended { get; set; }
		
		/// <summary>
		/// Display name.
		/// </summary>
		public string DisplayName { get; }

		/// <summary>
		/// Description.
		/// </summary>
		public string Description { get; }

		/// <summary>
		/// Date format.
		/// </summary>
		public string Format { get; set; }

		/// <summary>
		/// Field type.
		/// </summary>
		public Type Type { get; }

		/// <summary>
		/// Is field required.
		/// </summary>
		public bool IsRequired { get; set; }

		/// <summary>
		/// Is field enabled.
		/// </summary>
		public bool IsEnabled
		{
			get => Order != null;
			set
			{
				if (IsEnabled == value)
					return;

				if (value)
				{
					if (Order == null)
						Order = 0;
				}
				else
					Order = null;
				
				NotifyChanged(nameof(IsEnabled));
				NotifyChanged(nameof(Order));
			}
		}

		private int? _order;

		/// <summary>
		/// Field order.
		/// </summary>
		public int? Order
		{
			get => _order;
			set
			{
				if (Order == value)
					return;

				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value));

				_order = value;

				IsEnabled = value != null;

				NotifyChanged(nameof(IsEnabled));
				NotifyChanged(nameof(Order));
			}
		}

		private IEnumerable<FieldMappingValue> _values = Enumerable.Empty<FieldMappingValue>();

		/// <summary>
		/// Mapping values.
		/// </summary>
		public IEnumerable<FieldMappingValue> Values
		{
			get => _values;
			set => _values = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Default value.
		/// </summary>
		public string DefaultValue { get; set; }

		/// <summary>
		/// Zero as <see langword="null"/>.
		/// </summary>
		public bool ZeroAsNull { get; set; }

		/// <summary>
		/// Multiple field's instancies allowed.
		/// </summary>
		public bool IsMultiple => IsAdapter;

		/// <summary>
		/// <see cref="AdapterType"/> required.
		/// </summary>
		public bool IsAdapter { get; set; }

		/// <summary>
		/// Adapter.
		/// </summary>
		public Type AdapterType { get; set; }

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			Name = storage.GetValue<string>(nameof(Name));
			IsExtended = storage.GetValue<bool>(nameof(IsExtended));
			Values = storage.GetValue<SettingsStorage[]>(nameof(Values)).Select(s => s.Load<FieldMappingValue>()).ToArray();
			DefaultValue = storage.GetValue<string>(nameof(DefaultValue));
			Format = storage.GetValue<string>(nameof(Format));
			ZeroAsNull = storage.GetValue<bool>(nameof(ZeroAsNull));

			//IsEnabled = storage.GetValue(nameof(IsEnabled), IsEnabled);

			if (storage.ContainsKey(nameof(IsEnabled)))
				IsEnabled = storage.GetValue<bool>(nameof(IsEnabled));
			else
				Order = storage.GetValue<int?>(nameof(Order));

			IsAdapter = storage.GetValue(nameof(IsAdapter), IsAdapter);
			AdapterType = storage.GetValue<string>(nameof(AdapterType)).To<Type>();
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Name), Name);
			storage.SetValue(nameof(IsExtended), IsExtended);
			storage.SetValue(nameof(Values), Values.Select(v => v.Save()).ToArray());
			storage.SetValue(nameof(DefaultValue), DefaultValue);
			storage.SetValue(nameof(Format), Format);
			//storage.SetValue(nameof(IsEnabled), IsEnabled);
			storage.SetValue(nameof(Order), Order);
			storage.SetValue(nameof(ZeroAsNull), ZeroAsNull);
			storage.SetValue(nameof(IsAdapter), IsAdapter);
			storage.SetValue(nameof(AdapterType), AdapterType.To<string>());
		}

		/// <summary>
		/// Apply value.
		/// </summary>
		/// <param name="instance">Instance.</param>
		/// <param name="value">Field value.</param>
		public void ApplyFileValue(object instance, string value)
		{
			if (value.IsEmpty())
			{
				ApplyDefaultValue(instance);
				return;
			}

			if (Values.Any())
			{
				var v = Values.FirstOrDefault(vl => vl.ValueFile.CompareIgnoreCase(value));

				if (v != null)
				{
					ApplyValue(instance, v.ValueStockSharp);
					return;
				}
			}
			
			if (_enumNames.Contains(value))
			{
				ApplyValue(instance, value.To(Type));
				return;
			}

			ApplyValue(instance, value);
		}

		/// <summary>
		/// Apply default value.
		/// </summary>
		/// <param name="instance">Instance.</param>
		public void ApplyDefaultValue(object instance)
		{
			ApplyValue(instance, DefaultValue);
		}

		private void ApplyValue(object instance, object value)
		{
			if (Type == typeof(decimal))
			{
				if (value is string str)
				{
					if (str.ContainsIgnoreCase("e")) // exponential notation
						value = str.To<double>();
					else
					{
						str = str.Replace(',', '.').RemoveSpaces().ReplaceWhiteSpaces().Trim();

						if (str.IsEmpty())
							return;

						value = str;
					}
				}
			}
			else if (Type.IsDateTime())
			{
				if (value is string str)
				{
					if (_dateParser == null)
						_dateParser = new FastDateTimeParser(Format);

					if (Type == typeof(DateTimeOffset))
					{
						var dto = _dateParser.ParseDto(str);

						if (dto.Offset.IsDefault())
						{
							var tz = Scope<TimeZoneInfo>.Current?.Value;

							if (tz != null)
								dto = dto.UtcDateTime.ApplyTimeZone(tz);
						}

						value = dto;
					}
					else
					{
						value = _dateParser.Parse(str);
					}
				}
			}
			else if (Type == typeof(TimeSpan))
			{
				if (value is string str)
				{
					if (_timeParser == null)
						_timeParser = new FastTimeSpanParser(Format);

					value = _timeParser.Parse(str);
				}
			}

			if (value != null)
			{
				value = value.To(Type);

				if (ZeroAsNull && Type.IsNumeric() && value.To<decimal>() == 0)
					return;

				OnApply(instance, value);
			}
		}

		/// <summary>
		/// Apply value.
		/// </summary>
		/// <param name="instance">Instance.</param>
		/// <param name="value">Field value.</param>
		protected abstract void OnApply(object instance, object value);

		/// <inheritdoc />
		public override string ToString() => Name;

		/// <inheritdoc />
		public abstract object Clone();

		/// <summary>
		/// Reset state.
		/// </summary>
		public void Reset()
		{
			_dateParser = null;
			_timeParser = null;
		}

		/// <summary>
		/// Get <see cref="FieldMapping"/> instance or clone dependent on <see cref="IsMultiple"/>.
		/// </summary>
		/// <returns>Field.</returns>
		public FieldMapping GetOrClone()
		{
			return IsMultiple ? (FieldMapping)Clone() : this;
		}
	}

	/// <summary>
	/// Importing field description.
	/// </summary>
	/// <typeparam name="TInstance">Type, containing the field.</typeparam>
	/// <typeparam name="TValue">Field value type.</typeparam>
	public class FieldMapping<TInstance, TValue> : FieldMapping
	{
		private readonly Action<TInstance, TValue> _apply;

		/// <summary>
		/// Initializes a new instance of the <see cref="FieldMapping{TInstance,TValue}"/>.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="displayName">Display name.</param>
		/// <param name="description">Description.</param>
		/// <param name="apply">Apply field value action.</param>
		public FieldMapping(string name, string displayName, string description, Action<TInstance, TValue> apply)
			: this(name, displayName, description, typeof(TValue), apply)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="FieldMapping{TInstance,TValue}"/>.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="displayName">Display name.</param>
		/// <param name="description">Description.</param>
		/// <param name="type">Field type.</param>
		/// <param name="apply">Apply field value action.</param>
		public FieldMapping(string name, string displayName, string description, Type type, Action<TInstance, TValue> apply)
			: base(name, displayName, description, type)
		{
			_apply = apply ?? throw new ArgumentNullException(nameof(apply));
		}

		/// <inheritdoc />
		protected override void OnApply(object instance, object value)
		{
			_apply((TInstance)instance, (TValue)value);
		}

		/// <inheritdoc />
		public override object Clone()
		{
			var clone = new FieldMapping<TInstance, TValue>(Name, DisplayName, Description, _apply);
			clone.Load(this.Save());
			return clone;
		}
	}
}
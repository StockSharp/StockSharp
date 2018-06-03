namespace StockSharp.Algo.Import
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	/// <summary>
	/// Importing field description.
	/// </summary>
	public abstract class FieldMapping : NotifiableObject, IPersistable
	{
		//private readonly Settings _settings;

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
		/// <param name="isExtended">Is field extended.</param>
		protected FieldMapping(string name, string displayName, string description, Type type, bool isExtended)
		{
			//if (settings == null)
			//	throw new ArgumentNullException(nameof(settings));

			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));
				
			if (displayName.IsEmpty())
				throw new ArgumentNullException(nameof(displayName));

			if (type == null)
				throw new ArgumentNullException(nameof(type));

			if (description.IsEmpty())
				description = displayName;

			//_settings = settings;
			Name = name;
			DisplayName = displayName;
			Description = description;
			Type = type;
			IsExtended = isExtended;
			IsEnabled = true;

			Values = new ObservableCollection<FieldMappingValue>();
			//Number = -1;

			if (Type == typeof(DateTimeOffset))
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
		public bool IsExtended { get; private set; }
		
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

		/// <summary>
		/// Mapping values.
		/// </summary>
		public ObservableCollection<FieldMappingValue> Values { get; }

		/// <summary>
		/// Default value.
		/// </summary>
		public string DefaultValue { get; set; }

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			Name = storage.GetValue<string>(nameof(Name));
			IsExtended = storage.GetValue<bool>(nameof(IsExtended));
			Values.AddRange(storage.GetValue<SettingsStorage[]>(nameof(Values)).Select(s => s.Load<FieldMappingValue>()));
			DefaultValue = storage.GetValue<string>(nameof(DefaultValue));
			Format = storage.GetValue<string>(nameof(Format));

			//IsEnabled = storage.GetValue(nameof(IsEnabled), IsEnabled);

			if (storage.ContainsKey(nameof(IsEnabled)))
				IsEnabled = storage.GetValue<bool>(nameof(IsEnabled));
			else
				Order = storage.GetValue<int?>(nameof(Order));
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

			if (Values.Count > 0)
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
					str = str.Replace(',', '.').RemoveSpaces().ReplaceWhiteSpaces().Trim();

					if (str.IsEmpty())
						return;

					value = str;
				}
			}
			else if (Type == typeof(DateTimeOffset))
			{
				if (value is string str)
				{
					if (_dateParser == null)
						_dateParser = new FastDateTimeParser(Format);

					var dto = _dateParser.ParseDto(str);

					if (dto.Offset.IsDefault())
					{
						var tz = Scope<TimeZoneInfo>.Current?.Value;

						if (tz != null)
							dto = dto.UtcDateTime.ApplyTimeZone(tz);
					}

					value = dto;
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
				OnApply(instance, value.To(Type));
		}

		/// <summary>
		/// Apply value.
		/// </summary>
		/// <param name="instance">Instance.</param>
		/// <param name="value">Field value.</param>
		protected abstract void OnApply(object instance, object value);

		/// <inheritdoc />
		public override string ToString()
		{
			return Name;
		}

		/// <summary>
		/// Reset state.
		/// </summary>
		public void Reset()
		{
			_dateParser = null;
			_timeParser = null;
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
		/// <param name="isExtended">Is field extended.</param>
		public FieldMapping(string name, string displayName, string description, Action<TInstance, TValue> apply, bool isExtended = false)
			: base(name, displayName, description, typeof(TValue), isExtended)
		{
			_apply = apply ?? throw new ArgumentNullException(nameof(apply));
		}

		/// <inheritdoc />
		protected override void OnApply(object instance, object value)
		{
			_apply((TInstance)instance, (TValue)value);
		}
	}
}
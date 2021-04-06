namespace StockSharp.Algo.Indicators
{
	using System;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	/// <summary>
	/// The indicator type description.
	/// </summary>
	public class IndicatorType : Equatable<IndicatorType>, IPersistable
	{
		private Type _indicator;

		/// <summary>
		/// Indicator name.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// The indicator description.
		/// </summary>
		public string Description { get; private set; }

		/// <summary>
		/// Indicator type.
		/// </summary>
		public Type Indicator
		{
			get => _indicator;
			private set
			{
				_indicator = value;

				Name = _indicator != null ? _indicator.GetDisplayName() : string.Empty;
				Description = _indicator != null ? _indicator.GetDescription() : string.Empty;

				InputValue = _indicator?.GetValueType(true);
				OutputValue = _indicator?.GetValueType(false);
			}
		}

		/// <summary>
		/// The renderer type for indicator extended drawing.
		/// </summary>
		public Type Painter { get; private set; }

		/// <summary>
		/// Input values type.
		/// </summary>
		public Type InputValue { get; private set; }

		/// <summary>
		/// Result values type.
		/// </summary>
		public Type OutputValue { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="IndicatorType"/>.
		/// </summary>
		public IndicatorType()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="IndicatorType"/>.
		/// </summary>
		/// <param name="indicator">Indicator type.</param>
		/// <param name="painter">The renderer type for indicator extended drawing.</param>
		public IndicatorType(Type indicator, Type painter)
		{
			if (indicator == null)
				throw new ArgumentNullException(nameof(indicator));

			Indicator = indicator;
			Painter = painter;
		}

		/// <summary>
		/// Create a copy of <see cref="IndicatorType"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IndicatorType Clone()
		{
			var clone = GetType().CreateInstance<IndicatorType>();
			clone.Load(this.Save());
			return clone;
		}

		/// <summary>
		/// Compare <see cref="IndicatorType"/> on the equivalence.
		/// </summary>
		/// <param name="other">Another value with which to compare.</param>
		/// <returns><see langword="true" />, if the specified object is equal to the current object, otherwise, <see langword="false" />.</returns>
		protected override bool OnEquals(IndicatorType other)
		{
			return Indicator == other.Indicator;
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			if (storage.ContainsKey(nameof(Indicator)))
				Indicator = storage.GetValue<string>(nameof(Indicator)).To<Type>();

			if (storage.ContainsKey(nameof(Painter)))
				Painter = storage.GetValue<string>(nameof(Painter)).To<Type>();

			if (storage.ContainsKey(nameof(InputValue)))
				InputValue = storage.GetValue<string>(nameof(InputValue)).To<Type>();

			if (storage.ContainsKey(nameof(OutputValue)))
				OutputValue = storage.GetValue<string>(nameof(OutputValue)).To<Type>();
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			if (Indicator != null)
				storage.SetValue(nameof(Indicator), Indicator.GetTypeName(false));

			if (Painter != null)
				storage.SetValue(nameof(Painter), Painter.GetTypeName(false));

			if (InputValue != null)
				storage.SetValue(nameof(InputValue), InputValue.GetTypeName(false));

			if (OutputValue != null)
				storage.SetValue(nameof(OutputValue), OutputValue.GetTypeName(false));
		}
	}
}
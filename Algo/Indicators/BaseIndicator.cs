#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: BaseIndicator.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// The base Indicator.
	/// </summary>
	public abstract class BaseIndicator : Cloneable<IIndicator>, IIndicator
	{
		/// <summary>
		/// Initialize <see cref="BaseIndicator"/>.
		/// </summary>
		protected BaseIndicator()
		{
			var type = GetType();

			_name = type.GetDisplayName();
			InputType = type.GetValueType(true);
			ResultType = type.GetValueType(false);
		}

		/// <summary>
		/// Unique ID.
		/// </summary>
		[Browsable(false)]
		public Guid Id { get; private set; } = Guid.NewGuid();

		private string _name;

		/// <summary>
		/// Indicator name.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.NameKey)]
		[DescriptionLoc(LocalizedStrings.Str908Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public virtual string Name
		{
			get => _name;
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException(nameof(value));

				_name = value;
			}
		}

		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		public virtual void Reset()
		{
			IsFormed = false;
			Container.ClearValues();
			Reseted?.Invoke();
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public virtual void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Id), Id);
			storage.SetValue(nameof(Name), Name);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public virtual void Load(SettingsStorage storage)
		{
			Id = storage.GetValue<Guid>(nameof(Id));
			Name = storage.GetValue<string>(nameof(Name));
		}

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		[Browsable(false)]
		public virtual bool IsFormed { get; protected set; }

		/// <summary>
		/// The container storing indicator data.
		/// </summary>
		[Browsable(false)]
		public IIndicatorContainer Container { get; } = new IndicatorContainer();

		/// <summary>
		/// Input values type.
		/// </summary>
		[Browsable(false)]
		public virtual Type InputType { get; }

		/// <summary>
		/// Result values type.
		/// </summary>
		[Browsable(false)]
		public virtual Type ResultType { get; }

		/// <summary>
		/// The indicator change event (for example, a new value is added).
		/// </summary>
		public event Action<IIndicatorValue, IIndicatorValue> Changed;

		/// <summary>
		/// The event of resetting the indicator status to initial. The event is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		public event Action Reseted;

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		public virtual IIndicatorValue Process(IIndicatorValue input)
		{
			var result = OnProcess(input);

			result.InputValue = input;
			//var result = value as IIndicatorValue ?? input.SetValue(value);

			if (input.IsFinal)
			{
				result.IsFinal = input.IsFinal;
				Container.AddValue(input, result);
			}

			if (IsFormed && !result.IsEmpty)
				RaiseChangedEvent(input, result);

			return result;
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected abstract IIndicatorValue OnProcess(IIndicatorValue input);

		/// <summary>
		/// To call the event <see cref="BaseIndicator.Changed"/>.
		/// </summary>
		/// <param name="input">The input value of the indicator.</param>
		/// <param name="result">The resulting value of the indicator.</param>
		protected void RaiseChangedEvent(IIndicatorValue input, IIndicatorValue result)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));

			if (result == null)
				throw new ArgumentNullException(nameof(result));

			Changed?.Invoke(input, result);
		}

		/// <summary>
		/// Create a copy of <see cref="IIndicator"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IIndicator Clone()
		{
			return PersistableHelper.Clone(this);
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return Name;
		}
	}
}

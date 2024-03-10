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
	using System.ComponentModel.DataAnnotations;

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

		/// <inheritdoc />
		[Browsable(false)]
		public Guid Id { get; private set; } = Guid.NewGuid();

		private string _name;

		/// <inheritdoc />
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.NameKey,
			Description = LocalizedStrings.IndicatorNameKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public string Name
		{
			get => _name;
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException(nameof(value));

				_name = value;
			}
		}

		/// <inheritdoc />
		[Browsable(false)]
		public virtual int NumValuesToInitialize => 1;

		/// <inheritdoc />
		public virtual void Reset()
		{
			_isFormed = false;
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

		/// <inheritdoc />
		[Browsable(false)]
		public virtual IndicatorMeasures Measure { get; } = IndicatorMeasures.Price;

		private bool _isFormed;

		/// <inheritdoc />
		[Browsable(false)]
		public bool IsFormed
		{
			get
			{
				if (_isFormed)
					return true;

				return _isFormed = CalcIsFormed();
			}
			protected set => _isFormed = value;
		}

		/// <summary>
		/// Calc <see cref="IsFormed"/>.
		/// </summary>
		/// <returns><see cref="IsFormed"/></returns>
		protected virtual bool CalcIsFormed() => false;

		/// <inheritdoc />
		[Browsable(false)]
		public IIndicatorContainer Container { get; } = new IndicatorContainer();

		/// <inheritdoc />
		[Browsable(false)]
		public virtual Type InputType { get; }

		/// <inheritdoc />
		[Browsable(false)]
		public virtual Type ResultType { get; }

		/// <inheritdoc />
		public event Action<IIndicatorValue, IIndicatorValue> Changed;

		/// <inheritdoc />
		public event Action Reseted;

		/// <inheritdoc />
		public virtual IIndicatorValue Process(IIndicatorValue input)
		{
			var result = OnProcess(input);

			if(result.Indicator != this)
				throw new InvalidOperationException($"invalid indicator value. expected {GetType().Name} got {result.Indicator?.GetType()}");

			//var result = value as IIndicatorValue ?? input.SetValue(value);

			if (input.IsFinal)
			{
				result.IsFinal = input.IsFinal;
				Container.AddValue(input, result);
			}

			if (!result.IsEmpty)
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
		/// To call the event <see cref="Changed"/>.
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
			=> PersistableHelper.Clone(this);

		/// <inheritdoc />
		public override string ToString() => Name;

		/// <inheritdoc/>
		public virtual IIndicatorValue CreateValue(object[] values)
		{
			var value = GetType().GetValueType(false).CreateInstance<IIndicatorValue>(this);
			value.FromValues(values);
			return value;
		}
	}
}

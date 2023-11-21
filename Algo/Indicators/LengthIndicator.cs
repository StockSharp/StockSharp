#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: LengthIndicator.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// The interface for indicators with one resulting value and based on the period.
	/// </summary>
	public interface ILengthIndicator
	{
		/// <summary>
		/// Period length. By default equal to 1.
		/// </summary>
		int Length { get; }
	}

	/// <summary>
	/// The base class for indicators with one resulting value and based on the period.
	/// </summary>
	/// <typeparam name="TResult">Result values type.</typeparam>
	public abstract class LengthIndicator<TResult> : BaseIndicator, ILengthIndicator
	{
		/// <summary>
		/// Buffer.
		/// </summary>
		protected class LengthIndicatorBuffer : CircularBuffer<TResult>
		{
			private readonly LengthIndicator<TResult> _parent;

			internal LengthIndicatorBuffer(LengthIndicator<TResult> parent)
				: base(parent.Length)
			{
				_parent = parent ?? throw new ArgumentNullException(nameof(parent));
				Reset();
			}

			/// <summary>
			/// Calc <see cref="Sum"/>.
			/// </summary>
			public IOperator<TResult> Operator { get; set; }

			/// <summary>
			/// Calc <see cref="Max"/>.
			/// </summary>
			public IComparer<TResult> MaxComparer { get; set; }

			/// <summary>
			/// Calc <see cref="Min"/>.
			/// </summary>
			public IComparer<TResult> MinComparer { get; set; }

			/// <summary>
			/// Max value.
			/// </summary>
			public NullableEx<TResult> Max { get; private set; } = new();

			/// <summary>
			/// Min value.
			/// </summary>
			public NullableEx<TResult> Min { get; private set; } = new();

			/// <summary>
			/// Sum of all elements in buffer.
			/// </summary>
			public TResult Sum { get; private set; }

			/// <summary>
			/// Sum of all elements in buffer without the first element.
			/// </summary>
			public TResult SumNoFirst => Count == 0 ? default : Operator.Subtract(Sum, this[0]);

			/// <summary>
			/// Add with <see cref="Length"/> auto adjust.
			/// </summary>
			/// <param name="result">Value.</param>
			public void AddEx(TResult result)
			{
				var op = Operator;
				var maxComparer = MaxComparer;
				var minComparer = MinComparer;

				var recalcMax = false;
				var recalcMin = false;

				if (Count == _parent.Length)
				{
					if (op is not null)
						Sum = op.Subtract(Sum, this[0]);

					if (maxComparer?.Compare(Max.Value, this[0]) == 0)
						recalcMax = true;

					if (minComparer?.Compare(Min.Value, this[0]) == 0)
						recalcMin = true;
				}

				PushBack(result);

				if (op is not null)
					Sum = op.Add(Sum, result);

				if (maxComparer is not null)
				{
					if (recalcMax)
						Max.Value = this.Max(maxComparer);
					else if (!Max.HasValue || maxComparer?.Compare(Max.Value, result) < 0)
						Max.Value = result;
				}

				if (minComparer is not null)
				{
					if (recalcMin)
						Min.Value = this.Min(minComparer);
					else if (!Min.HasValue || minComparer?.Compare(Min.Value, result) > 0)
						Min.Value = result;
				}
			}

			/// <summary>
			/// Reset.
			/// </summary>
			public void Reset()
			{
				Clear();
				Capacity = _parent.Length;
				Sum = default;
				Max = new();
				Min = new();
			}
		}

		/// <summary>
		/// Initialize <see cref="LengthIndicator{T}"/>.
		/// </summary>
		protected LengthIndicator()
		{
			Buffer = new(this);
		}

		/// <inheritdoc />
		public override void Reset()
		{
			Buffer.Reset();
			base.Reset();
		}

		/// <inheritdoc />
		public override int NumValuesToInitialize => Length;

		private int _length = 1;

		/// <inheritdoc />
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.PeriodKey,
			Description = LocalizedStrings.PeriodLengthKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public virtual int Length
		{
			get => _length;
			set
			{
				if (value < 1)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

				_length = value;

				Reset();
			}
		}

		/// <inheritdoc />
		protected override bool CalcIsFormed() => Buffer.Count >= Length;

		/// <summary>
		/// The buffer for data storage.
		/// </summary>
		[Browsable(false)]
		protected LengthIndicatorBuffer Buffer { get; }

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);
			Length = storage.GetValue<int>(nameof(Length));
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);
			storage.SetValue(nameof(Length), Length);
		}

		/// <inheritdoc />
		public override string ToString() => base.ToString() + " " + Length;
	}
}

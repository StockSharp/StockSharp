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

	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// The base class for indicators with one resulting value and based on the period.
	/// </summary>
	/// <typeparam name="TResult">Result values type.</typeparam>
	public abstract class LengthIndicator<TResult> : BaseIndicator
	{
		/// <summary>
		/// Initialize <see cref="LengthIndicator{T}"/>.
		/// </summary>
		protected LengthIndicator()
		{
			Buffer = new List<TResult>();
		}

		/// <inheritdoc />
		public override void Reset()
		{
			Buffer.Clear();
			base.Reset();
		}

		private int _length = 1;

		/// <summary>
		/// Period length. By default equal to 1.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str736Key)]
		[DescriptionLoc(LocalizedStrings.Str778Key, true)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public int Length
		{
			get => _length;
			set
			{
				if (value < 1)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str916);

				_length = value;

				Reset();
			}
		}

		/// <inheritdoc />
		public override bool IsFormed => Buffer.Count >= Length;

		/// <summary>
		/// The buffer for data storage.
		/// </summary>
		[Browsable(false)]
		protected IList<TResult> Buffer { get; }

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

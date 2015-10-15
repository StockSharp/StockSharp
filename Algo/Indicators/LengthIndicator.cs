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
	public abstract class LengthIndicator<TResult> : BaseIndicator
	{
		/// <summary>
		/// Initialize <see cref="LengthIndicator{T}"/>.
		/// </summary>
		protected LengthIndicator()
		{
			Buffer = new List<TResult>();
		}

		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
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
			get { return _length; }
			set
			{
				if (value < 1)
					throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.Str916);

				_length = value;

				Reset();
			}
		}

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		public override bool IsFormed
		{
			get { return Buffer.Count >= Length; }
		}

		/// <summary>
		/// The buffer for data storage.
		/// </summary>
		[Browsable(false)]
		protected IList<TResult> Buffer { get; private set; }

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);
			Length = settings.GetValue<int>("Length");
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);
			settings.SetValue("Length", Length);
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return base.ToString() + " " + Length;
		}
	}
}

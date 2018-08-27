#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Testing.Algo
File: MarketDataGenerator.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Testing
{
	using System;

	using Ecng.Common;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The market data generator.
	/// </summary>
	public abstract class MarketDataGenerator : Cloneable<MarketDataGenerator>
	{
		/// <summary>
		/// Initialize <see cref="MarketDataGenerator"/>.
		/// </summary>
		/// <param name="securityId">The identifier of the instrument, for which data shall be generated.</param>
		protected MarketDataGenerator(SecurityId securityId)
		{
			SecurityId = securityId;

			MaxVolume = 20;
			MinVolume = 1;
			MaxPriceStepCount = 10;
			RandomArrayLength = 100;

			Interval = TimeSpan.FromMilliseconds(50);
		}

		/// <summary>
		/// Market data type.
		/// </summary>
		public abstract MarketDataTypes DataType { get; }

		/// <summary>
		/// The length of massive of preliminarily generated random numbers. The default is 100.
		/// </summary>
		public int RandomArrayLength { get; set; }

		/// <summary>
		/// To initialize the generator state.
		/// </summary>
		public virtual void Init()
		{
			LastGenerationTime = DateTimeOffset.MinValue;

			Volumes = new RandomArray<int>(MinVolume, MaxVolume, RandomArrayLength);
			Steps = new RandomArray<int>(1, MaxPriceStepCount, RandomArrayLength);

			SecurityDefinition = null;
		}

		/// <summary>
		/// The identifier of the instrument, for which data shall be generated.
		/// </summary>
		public SecurityId SecurityId { get; }

		/// <summary>
		/// Information about the trading instrument.
		/// </summary>
		protected SecurityMessage SecurityDefinition { get; private set; }

		/// <summary>
		/// The time of last data generation.
		/// </summary>
		protected DateTimeOffset LastGenerationTime { get; set; }

		/// <summary>
		/// The data generation interval.
		/// </summary>
		public TimeSpan Interval { get; set; }

		private int _maxVolume;

		/// <summary>
		/// The maximal volume. The volume will be selected randomly from <see cref="MinVolume"/> to <see cref="MaxVolume"/>.
		/// </summary>
		/// <remarks>
		/// The default value equals 20.
		/// </remarks>
		public int MaxVolume
		{
			get => _maxVolume;
			set
			{
				if (value < 1)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1133);

				_maxVolume = value;
			}
		}

		private int _minVolume;

		/// <summary>
		/// The maximal volume. The volume will be selected randomly from <see cref="MinVolume"/> to <see cref="MaxVolume"/>.
		/// </summary>
		/// <remarks>
		/// The default value is 1.
		/// </remarks>
		public int MinVolume
		{
			get => _minVolume;
			set
			{
				if (value < 1)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1134);

				_minVolume = value;
			}
		}

		private int _maxPriceStepCount;

		/// <summary>
		/// The maximal number of price increments <see cref="BusinessEntities.Security.PriceStep"/> to be returned through massive <see cref="Steps"/>.
		/// </summary>
		/// <remarks>
		/// The default value is 10.
		/// </remarks>
		public int MaxPriceStepCount
		{
			get => _maxPriceStepCount;
			set
			{
				if (value < 1)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1135);

				_maxPriceStepCount = value;
			}
		}

		/// <summary>
		/// Process message.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <returns>The result of processing. If <see langword="null" /> is returned, then generator has no sufficient data to generate new message.</returns>
		public virtual Message Process(Message message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (message.Type == MessageTypes.Security)
				SecurityDefinition = (SecurityMessage)message.Clone();
			else if (SecurityDefinition != null)
				return OnProcess(message);

			return null;
		}

		/// <summary>
		/// Process message.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <returns>The result of processing. If <see langword="null" /> is returned, then generator has no sufficient data to generate new message.</returns>
		protected abstract Message OnProcess(Message message);

		/// <summary>
		/// Is new data generation required.
		/// </summary>
		/// <param name="time">The current time.</param>
		/// <returns><see langword="true" />, if data shall be generated, Otherwise, <see langword="false" />.</returns>
		protected bool IsTimeToGenerate(DateTimeOffset time)
		{
			return time >= LastGenerationTime + Interval;
		}

		private RandomArray<int> _volumes;

		/// <summary>
		/// The massive of random volumes in the range from <see cref="MarketDataGenerator.MinVolume"/> to <see cref="MarketDataGenerator.MaxVolume"/>.
		/// </summary>
		public RandomArray<int> Volumes
		{
			get
			{
				if (_volumes == null)
					throw new InvalidOperationException(LocalizedStrings.Str1136);

				return _volumes;
			}
			protected set => _volumes = value ?? throw new ArgumentNullException(nameof(value));
		}

		private RandomArray<int> _steps;

		/// <summary>
		/// The massive of random price increments in the range from 1 to <see cref="MarketDataGenerator.MaxPriceStepCount"/>.
		/// </summary>
		public RandomArray<int> Steps
		{
			get
			{
				if (_steps == null)
					throw new InvalidOperationException(LocalizedStrings.Str1136);

				return _steps;
			}
			protected set => _steps = value ?? throw new ArgumentNullException(nameof(value));
		}
	}
}
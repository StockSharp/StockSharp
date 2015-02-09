namespace StockSharp.Algo.Testing
{
	using System;

	using Ecng.Common;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Генератор маркет-данных.
	/// </summary>
	public abstract class MarketDataGenerator : Cloneable<MarketDataGenerator>
	{
		/// <summary>
		/// Инициализировать <see cref="MarketDataGenerator"/>.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента, для которого необходимо генерировать данные.</param>
		protected MarketDataGenerator(SecurityId securityId)
		{
			SecurityId = securityId;

			MaxVolume = 20;
			MinVolume = 1;
			MaxPriceStepCount = 10;
			RandomArrayLength = 100;
		}

		/// <summary>
		/// Длина массива предварительно сгенерированных случайных чисел. По умолчанию 100.
		/// </summary>
		public int RandomArrayLength { get; set; }

		/// <summary>
		/// Инициализировать состояние генератора.
		/// </summary>
		public virtual void Init()
		{
			LastGenerationTime = DateTimeOffset.MinValue;

			Volumes = new RandomArray<int>(MinVolume, MaxVolume, RandomArrayLength);
			Steps = new RandomArray<int>(1, MaxPriceStepCount, RandomArrayLength);

			SecurityDefinition = null;
		}

		/// <summary>
		/// Идентификатор инструмента, для которого необходимо генерировать данные.
		/// </summary>
		public SecurityId SecurityId { get; private set; }

		/// <summary>
		/// Информация о торговом инструменте.
		/// </summary>
		protected SecurityMessage SecurityDefinition { get; private set; }

		/// <summary>
		/// Время последней генерации данных.
		/// </summary>
		protected DateTimeOffset LastGenerationTime { get; set; }

		/// <summary>
		/// Интервал генерации данных.
		/// </summary>
		public TimeSpan Interval { get; set; }

		private int _maxVolume;

		/// <summary>
		/// Максимальный объем. Объем будет выбран случайно от <see cref="MinVolume"/> до <see cref="MaxVolume"/>.
		/// </summary>
		/// <remarks>
		/// Значение по умолчанию равно 20.
		/// </remarks>
		public int MaxVolume
		{
			get { return _maxVolume; }
			set
			{
				if (value < 1)
					throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.Str1133);

				_maxVolume = value;
			}
		}

		private int _minVolume;

		/// <summary>
		/// Максимальный объем. Объем будет выбран случайно от <see cref="MinVolume"/> до <see cref="MaxVolume"/>.
		/// </summary>
		/// <remarks>
		/// Значение по умолчанию равно 1.
		/// </remarks>
		public int MinVolume
		{
			get { return _minVolume; }
			set
			{
				if (value < 1)
					throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.Str1134);

				_minVolume = value;
			}
		}

		private int _maxPriceStepCount;

		/// <summary>
		/// Максимальное количество шагов цены <see cref="BusinessEntities.Security.PriceStep"/>, которое будет возвращатся через массив <see cref="Steps"/>.
		/// </summary>
		/// <remarks>
		/// Значение по умолчанию равно 10.
		/// </remarks>
		public int MaxPriceStepCount
		{
			get { return _maxPriceStepCount; }
			set
			{
				if (value < 1)
					throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.Str1135);

				_maxPriceStepCount = value;
			}
		}

		/// <summary>
		/// Обработать сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		/// <returns>Результат обработки. Если будет возрвщено <see langword="null"/>,
		/// то генератору пока недостаточно данных для генерации нового сообщения.</returns>
		public virtual Message Process(Message message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			if (message.Type == MessageTypes.Security)
				SecurityDefinition = (SecurityMessage)message.Clone();
			else if (SecurityDefinition != null)
				return OnProcess(message);

			return null;
		}

		/// <summary>
		/// Обработать сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		/// <returns>Результат обработки. Если будет возрвщено <see langword="null"/>,
		/// то генератору пока недостаточно данных для генерации нового сообщения.</returns>
		protected abstract Message OnProcess(Message message);

		/// <summary>
		/// Требуется ли генерация новых данных.
		/// </summary>
		/// <param name="time">Текущее время.</param>
		/// <returns><see langword="true"/>, если надо сгенерировать данные. Иначе, <see langword="false"/>.</returns>
		protected bool IsTimeToGenerate(DateTimeOffset time)
		{
			return time >= LastGenerationTime + Interval;
		}

		private RandomArray<int> _volumes;

		/// <summary>
		/// Массив случайных объемов в диапазоне от <see cref="MinVolume"/> до <see cref="MaxVolume"/>.
		/// </summary>
		public RandomArray<int> Volumes
		{
			get
			{
				if (_volumes == null)
					throw new InvalidOperationException(LocalizedStrings.Str1136);

				return _volumes;
			}
			protected set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_volumes = value;
			}
		}

		private RandomArray<int> _steps;

		/// <summary>
		/// Массив случайных количеств шагов цены в диапазоне от 1 до <see cref="MaxPriceStepCount"/>.
		/// </summary>
		public RandomArray<int> Steps
		{
			get
			{
				if (_steps == null)
					throw new InvalidOperationException(LocalizedStrings.Str1136);

				return _steps;
			}
			protected set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_steps = value;
			}
		}
	}
}
namespace StockSharp.Algo.Indicators
{
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Реализация одной из линий индикатора Alligator (Jaw, Teeth, Lips).
	/// </summary>
	public class AlligatorLine : LengthIndicator<decimal>
	{
		private readonly MedianPrice _medianPrice;

		private readonly SmoothedMovingAverage _sma;
		//private readonly SimpleMovingAverage _sma;

		/// <summary>
		/// Создать <see cref="AlligatorLine"/>.
		/// </summary>
		public AlligatorLine()
		{
			_medianPrice = new MedianPrice();
			_sma = new SmoothedMovingAverage();
			//_sma = new SimpleMovingAverage();
		}

		private int _shift;

		/// <summary>
		/// Сдвиг в будущее.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str841Key)]
		[DescriptionLoc(LocalizedStrings.Str842Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public int Shift
		{
			get { return _shift; }
			set
			{
				_shift = value;
				Reset();
			}
		}

		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		public override void Reset()
		{
			_sma.Length = Length;
			_medianPrice.Reset();

			base.Reset();
		}

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		public override bool IsFormed { get { return Buffer.Count > Shift; } }

		/// <summary>
		/// Возможно ли обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns><see langword="true"/>, если возможно, иначе, <see langword="false"/>.</returns>
		public override bool CanProcess(IIndicatorValue input)
		{
			return _medianPrice.CanProcess(input);
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			//если кол-во в буфере больше Shift, то первое значение отдали в прошлый раз, удалим его.
			if (Buffer.Count > Shift)
				Buffer.RemoveAt(0);

			var smaResult = _sma.Process(_medianPrice.Process(input));
			if (_sma.IsFormed & input.IsFinal)
				Buffer.Add(smaResult.GetValue<decimal>());

			return Buffer.Count > Shift
				? new DecimalIndicatorValue(this, Buffer[0])
				: new DecimalIndicatorValue(this);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);
			Shift = settings.GetValue<int>("Shift");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);
			settings.SetValue("Shift", Shift);
		}
	}
}
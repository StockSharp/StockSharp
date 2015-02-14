namespace StockSharp.Algo.Indicators
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;

	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Базовый класс для индикаторов с одним результирующим значением и основанных на периоде.
	/// </summary>
	/// <typeparam name="TResult">Тип результирующего значение, которое возвращается через метод <see cref="BaseIndicator{TResult}.OnProcess"/>.</typeparam>
	public abstract class LengthIndicator<TResult> : BaseIndicator<TResult>
	{
		/// <summary>
		/// Инициализировать <see cref="LengthIndicator{TResult}"/>.
		/// </summary>
		protected LengthIndicator()
		{
			Buffer = new List<TResult>();
		}

		/// <summary>
		/// Инициализировать <see cref="LengthIndicator{TResult}"/>.
		/// </summary>
		/// <param name="valueType">Тип данных, которым оперирует индикатор.</param>
		protected LengthIndicator(Type valueType)
			: base(valueType)
		{
			Buffer = new List<TResult>();
		}

		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		public override void Reset()
		{
			Buffer.Clear();
			base.Reset();
		}

		private int _length = 1;

		/// <summary>
		/// Длина периода. По-умолчанию длина равна 1.
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
		/// Сформирован ли индикатор.
		/// </summary>
		public override bool IsFormed
		{
			get { return Buffer.Count >= Length; }
		}

		/// <summary>
		/// Буфер для хранения данных.
		/// </summary>
		[Browsable(false)]
		protected IList<TResult> Buffer { get; private set; }

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);
			Length = settings.GetValue<int>("Length");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);
			settings.SetValue("Length", Length);
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return base.ToString() + " " + Length;
		}
	}
}

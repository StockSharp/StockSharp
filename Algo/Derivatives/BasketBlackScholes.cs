namespace StockSharp.Algo.Derivatives
{
	using System;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Algo.Positions;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Портфельная модель расчета значений "греков" по формуле Блэка-Шоулза.
	/// </summary>
	public class BasketBlackScholes : BlackScholes
	{
		/// <summary>
		/// Модель расчета значений "греков" по формуле Блэка-Шоулза с учетом позиции.
		/// </summary>
		public class InnerModel
		{
			/// <summary>
			/// Создать <see cref="InnerModel"/>.
			/// </summary>
			/// <param name="model">Модель расчета значений "греков" по формуле Блэка-Шоулза.</param>
			/// <param name="positionManager">Менеджер позиции.</param>
			public InnerModel(BlackScholes model, IPositionManager positionManager)
			{
				if (model == null)
					throw new ArgumentNullException("model");

				if (positionManager == null)
					throw new ArgumentNullException("positionManager");

				Model = model;
				PositionManager = positionManager;
			}

			/// <summary>
			/// Модель расчета значений "греков" по формуле Блэка-Шоулза.
			/// </summary>
			public BlackScholes Model { get; private set; }

			/// <summary>
			/// Менеджер позиции.
			/// </summary>
			public IPositionManager PositionManager { get; private set; }
		}

		/// <summary>
		/// Интерфейс, описывающий коллекцию внутренних моделей <see cref="BasketBlackScholes.InnerModels"/>.
		/// </summary>
		public interface IInnerModelList : ISynchronizedCollection<InnerModel>
		{
			/// <summary>
			/// Получить модель расчета значений "греков" по формуле Блэка-Шоулза для конкретного опциона.
			/// </summary>
			/// <param name="option">Опцион.</param>
			/// <returns>Модель. Если опцион не зарегистрирован, то будет возвращено null.</returns>
			InnerModel this[Security option] { get; }
		}

		private sealed class InnerModelList : CachedSynchronizedList<InnerModel>, IInnerModelList
		{
			private readonly BasketBlackScholes _parent;

			public InnerModelList(BasketBlackScholes parent)
			{
				if (parent == null)
					throw new ArgumentNullException("parent");

				_parent = parent;
			}

			InnerModel IInnerModelList.this[Security option]
			{
				get
				{
					if (option == null)
						throw new ArgumentNullException("option");

					return this.SyncGet(c => c.FirstOrDefault(i => i.Model.Option == option));
				}
			}

			protected override bool OnAdding(InnerModel item)
			{
				item.Model.RoundDecimals = _parent.RoundDecimals;
				return base.OnAdding(item);
			}

			protected override bool OnInserting(int index, InnerModel item)
			{
				item.Model.RoundDecimals = _parent.RoundDecimals;
				return base.OnInserting(index, item);
			}
		}

		/// <summary>
		/// Создать <see cref="BasketBlackScholes"/>.
		/// </summary>
		/// <param name="securityProvider">Поставщик информации об инструментах.</param>
		/// <param name="dataProvider">Поставщик маркет-данных.</param>
		public BasketBlackScholes(ISecurityProvider securityProvider, IMarketDataProvider dataProvider)
			: base(securityProvider, dataProvider)
		{
			_innerModels = new InnerModelList(this);
		}

		private readonly InnerModelList _innerModels;

		/// <summary>
		/// Информация по опционам.
		/// </summary>
		public IInnerModelList InnerModels
		{
			get { return _innerModels; }
		}

		/// <summary>
		/// Позиция по базовому активу.
		/// </summary>
		public IPositionManager UnderlyingAssetPosition { get; set; }

		/// <summary>
		/// Опцион.
		/// </summary>
		public override Security Option
		{
			get { throw new NotSupportedException(); }
		}

		private Security _underlyingAsset;

		/// <summary>
		/// Базовый актив.
		/// </summary>
		public override Security UnderlyingAsset
		{
			get
			{
				if (_underlyingAsset == null)
				{
					var info = _innerModels.SyncGet(c => c.FirstOrDefault());

					if (info == null)
						throw new InvalidOperationException(LocalizedStrings.Str700);

					_underlyingAsset = info.Model.Option.GetAsset(SecurityProvider);
				}

				return _underlyingAsset;
			}
		}

		/// <summary>
		/// Количество знаков после запятой у вычисляемых значений. По-умолчанию равно -1, что означает не округлять значения.
		/// </summary>
		public override int RoundDecimals
		{
			set
			{
				base.RoundDecimals = value;

				lock (_innerModels.SyncRoot)
				{
					_innerModels.ForEach(m => m.Model.RoundDecimals = value);
				}
			}
		}

		private decimal GetAssetPosition()
		{
			return (UnderlyingAssetPosition != null ? UnderlyingAssetPosition.Position : 0);
		}

		/// <summary>
		/// Рассчитать дельту опциона.
		/// </summary>
		/// <param name="currentTime">Текущее время.</param>
		/// <param name="deviation">Стандартное отклонение. Если оно не указано, то используется <see cref="BlackScholes.DefaultDeviation"/>.</param>
		/// <param name="assetPrice">Цена базового актива. Если цена не указана, то получается цена последней сделки из <see cref="BlackScholes.UnderlyingAsset"/>.</param>
		/// <returns>Дельта опциона.</returns>
		public override decimal Delta(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			return ProcessOptions(bs => bs.Delta(currentTime, deviation, assetPrice)) + GetAssetPosition();
		}


		/// <summary>
		/// Рассчитать гамму опциона.
		/// </summary>
		/// <param name="currentTime">Текущее время.</param>
		/// <param name="deviation">Стандартное отклонение. Если оно не указано, то используется <see cref="BlackScholes.DefaultDeviation"/>.</param>
		/// <param name="assetPrice">Цена базового актива. Если цена не указана, то получается цена последней сделки из <see cref="BlackScholes.UnderlyingAsset"/>.</param>
		/// <returns>Гамма опциона.</returns>
		public override decimal Gamma(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			return ProcessOptions(bs => bs.Gamma(currentTime, deviation, assetPrice));
		}


		/// <summary>
		/// Рассчитать вегу опциона.
		/// </summary>
		/// <param name="currentTime">Текущее время.</param>
		/// <param name="deviation">Стандартное отклонение. Если оно не указано, то используется <see cref="BlackScholes.DefaultDeviation"/>.</param>
		/// <param name="assetPrice">Цена базового актива. Если цена не указана, то получается цена последней сделки из <see cref="BlackScholes.UnderlyingAsset"/>.</param>
		/// <returns>Вега опциона.</returns>
		public override decimal Vega(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			return ProcessOptions(bs => bs.Vega(currentTime, deviation, assetPrice));
		}

		/// <summary>
		/// Рассчитать тету опциона.
		/// </summary>
		/// <param name="currentTime">Текущее время.</param>
		/// <param name="deviation">Стандартное отклонение. Если оно не указано, то используется <see cref="BlackScholes.DefaultDeviation"/>.</param>
		/// <param name="assetPrice">Цена базового актива. Если цена не указана, то получается цена последней сделки из <see cref="BlackScholes.UnderlyingAsset"/>.</param>
		/// <returns>Тета опциона.</returns>
		public override decimal Theta(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			return ProcessOptions(bs => bs.Theta(currentTime, deviation, assetPrice));
		}

		/// <summary>
		/// Рассчитать ро опциона.
		/// </summary>
		/// <param name="currentTime">Текущее время.</param>
		/// <param name="deviation">Стандартное отклонение. Если оно не указано, то используется <see cref="BlackScholes.DefaultDeviation"/>.</param>
		/// <param name="assetPrice">Цена базового актива. Если цена не указана, то получается цена последней сделки из <see cref="BlackScholes.UnderlyingAsset"/>.</param>
		/// <returns>Ро опциона.</returns>
		public override decimal Rho(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			return ProcessOptions(bs => bs.Rho(currentTime, deviation, assetPrice));
		}

		/// <summary>
		/// Рассчитать премию опциона.
		/// </summary>
		/// <param name="currentTime">Текущее время.</param>
		/// <param name="deviation">Стандартное отклонение. Если оно не указано, то используется <see cref="BlackScholes.DefaultDeviation"/>.</param>
		/// <param name="assetPrice">Цена базового актива. Если цена не указана, то получается цена последней сделки из <see cref="BlackScholes.UnderlyingAsset"/>.</param>
		/// <returns>Премия опциона.</returns>
		public override decimal Premium(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			return ProcessOptions(bs => bs.Premium(currentTime, deviation, assetPrice));
		}

		/// <summary>
		/// Рассчитать подразумеваемую волатильность.
		/// </summary>
		/// <param name="currentTime">Текущее время.</param>
		/// <param name="premium">Премия по опциону.</param>
		/// <returns>Подразумеваевая волатильность.</returns>
		public override decimal ImpliedVolatility(DateTimeOffset currentTime, decimal premium)
		{
			return ProcessOptions(bs => bs.ImpliedVolatility(currentTime, premium), false);
		}

		/// <summary>
		/// Создать стакан волатильности.
		/// </summary>
		/// <param name="currentTime">Текущее время.</param>
		/// <returns>Стакан волатильности.</returns>
		public override MarketDepth ImpliedVolatility(DateTimeOffset currentTime)
		{
			throw new NotSupportedException();
			//return UnderlyingAsset.GetMarketDepth().ImpliedVolatility(this);
		}

		private decimal ProcessOptions(Func<BlackScholes, decimal> func, bool usePos = true)
		{
			return _innerModels.Cache.Sum(m =>
			{
				var iv = (decimal?)DataProvider.GetSecurityValue(m.Model.Option, Level1Fields.ImpliedVolatility);
				return iv == null ? 0 : func(m.Model) * (usePos ? m.PositionManager.Position : 1);
			});
		}
	}
}
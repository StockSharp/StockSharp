namespace StockSharp.Algo.Storages
{
	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Класс для представления в виде списка собственных сделок, хранящихся во внешнем хранилище.
	/// </summary>
	public class MyTradeList : BaseStorageEntityList<MyTrade>
	{
		/// <summary>
		/// Создать <see cref="MyTradeList"/>.
		/// </summary>
		/// <param name="storage">Специальный интерфейс для прямого доступа к хранилищу.</param>
		public MyTradeList(IStorage storage)
			: base(storage)
		{
			OverrideCreateDelete = true;
		}

		/// <summary>
		/// Получить данных из сущности для создания.
		/// </summary>
		/// <param name="entity">Сущность.</param>
		/// <returns>Данные для создания.</returns>
		protected override SerializationItemCollection GetOverridedAddSource(MyTrade entity)
		{
			var source = CreateSource(entity);

			if (entity.Commission != null)
				source.Add(new SerializationItem<decimal>(new VoidField<decimal>("Commission"), entity.Commission.Value));

			return source;
		}

		/// <summary>
		/// Получить данных из сущности для удаления.
		/// </summary>
		/// <param name="entity">Сущность.</param>
		/// <returns>Данные для удаления.</returns>
		protected override SerializationItemCollection GetOverridedRemoveSource(MyTrade entity)
		{
			return CreateSource(entity);
		}

		/// <summary>
		/// Загрузить собственную сделку.
		/// </summary>
		/// <param name="order">Заявка.</param>
		/// <param name="trade">Тиковая сделка.</param>
		/// <returns>Собственная сделка.</returns>
		public MyTrade ReadByOrderAndTrade(Order order, Trade trade)
		{
			return Read(CreateSource(order, trade));
		}

		/// <summary>
		/// Сохранить торговый объект.
		/// </summary>
		/// <param name="entity">Торговый объект.</param>
		public override void Save(MyTrade entity)
		{
			if (ReadByOrderAndTrade(entity.Order, entity.Trade) == null)
				Add(entity);
			//else
			//	Update(entity);
		}

		private static SerializationItemCollection CreateSource(MyTrade trade)
		{
			return CreateSource(trade.Order, trade.Trade);
		}

		private static SerializationItemCollection CreateSource(Order order, Trade trade)
		{
			return new SerializationItemCollection
			{
				new SerializationItem<long>(new VoidField<long>("Order"), order.TransactionId),
				new SerializationItem<string>(new VoidField<string>("Trade"), trade.Id == 0 ? trade.StringId : trade.Id.To<string>())
			};
		}
	}

	class SecurityMyTradeList : MyTradeList
	{
		public SecurityMyTradeList(IStorage storage, Security security)
			: base(storage)
		{
			AddFilter(Schema.Fields["Security"], security, () => security.Id);
		}
	}

	class OrderMyTradeList : MyTradeList
	{
		public OrderMyTradeList(IStorage storage, Order order)
			: base(storage)
		{
			AddFilter(Schema.Fields["Order"], order, () => order.Id);
		}
	}
}
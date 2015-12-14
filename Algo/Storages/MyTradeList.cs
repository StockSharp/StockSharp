#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Algo
File: MyTradeList.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages
{
	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The class for representation in the form of list of own trades, stored in external storage.
	/// </summary>
	public class MyTradeList : BaseStorageEntityList<MyTrade>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MyTradeList"/>.
		/// </summary>
		/// <param name="storage">The special interface for direct access to the storage.</param>
		public MyTradeList(IStorage storage)
			: base(storage)
		{
			OverrideCreateDelete = true;
		}

		/// <summary>
		/// To get data from essence for creation.
		/// </summary>
		/// <param name="entity">Entity.</param>
		/// <returns>Data for creation.</returns>
		protected override SerializationItemCollection GetOverridedAddSource(MyTrade entity)
		{
			var source = CreateSource(entity);

			if (entity.Commission != null)
				source.Add(new SerializationItem<decimal>(new VoidField<decimal>("Commission"), entity.Commission.Value));

			return source;
		}

		/// <summary>
		/// To get data from essence for deletion.
		/// </summary>
		/// <param name="entity">Entity.</param>
		/// <returns>Data for deletion.</returns>
		protected override SerializationItemCollection GetOverridedRemoveSource(MyTrade entity)
		{
			return CreateSource(entity);
		}

		/// <summary>
		/// To load own trade.
		/// </summary>
		/// <param name="order">Order.</param>
		/// <param name="trade">Tick trade.</param>
		/// <returns>Own trade.</returns>
		public MyTrade ReadByOrderAndTrade(Order order, Trade trade)
		{
			return Read(CreateSource(order, trade));
		}

		/// <summary>
		/// To save the trading object.
		/// </summary>
		/// <param name="entity">The trading object.</param>
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
#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Algo
File: PositionList.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages
{
	using System.Linq;

	using Ecng.Serialization;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The class for representation in the form of list of positions, stored in external storage.
	/// </summary>
	public class PositionList : BaseStorageEntityList<Position>, IStoragePositionList
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PositionList"/>.
		/// </summary>
		/// <param name="storage">The special interface for direct access to the storage.</param>
		public PositionList(IStorage storage)
			: base(storage)
		{
		}

		DelayAction IStorageEntityList<Position>.DelayAction => DelayAction;

		/// <summary>
		/// To get data from essence for creation.
		/// </summary>
		/// <param name="entity">Entity.</param>
		/// <returns>Data for creation.</returns>
		protected override SerializationItemCollection GetOverridedAddSource(Position entity)
		{
			return CreateSource(entity);
		}

		/// <summary>
		/// To get data from essence for deletion.
		/// </summary>
		/// <param name="entity">Entity.</param>
		/// <returns>Data for deletion.</returns>
		protected override SerializationItemCollection GetOverridedRemoveSource(Position entity)
		{
			return CreateSource(entity);
		}

		/// <inheritdoc />
		public Position GetPosition(Portfolio portfolio, Security security, string clientCode = "", string depoName = "")
		{
			return Read(CreateSource(security, portfolio));
		}

		/// <inheritdoc />
		public override void Save(Position entity)
		{
			if (GetPosition(entity.Portfolio, entity.Security) == null)
				Add(entity);
			else
				UpdateByKey(entity);
		}

		private void UpdateByKey(Position position)
		{
			var keyFields = new[]
			{
				Schema.Fields["Portfolio"],
				Schema.Fields["Security"]
			};
			var fields = Schema.Fields.Where(f => !keyFields.Contains(f)).ToArray();

			Database.Update(position, new FieldList(keyFields), new FieldList(fields));
		}

		private static SerializationItemCollection CreateSource(Position position)
		{
			return CreateSource(position.Security, position.Portfolio);
		}

		private static SerializationItemCollection CreateSource(Security security, Portfolio portfolio)
		{
			return new SerializationItemCollection
			{
				new SerializationItem<string>(new VoidField<string>("Security"), security.Id),
				new SerializationItem<string>(new VoidField<string>("Portfolio"), portfolio.Name)
			};
		}
	}
}
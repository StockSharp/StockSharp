namespace StockSharp.Algo.Export.Database
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Database provider.
	/// </summary>
	public interface IDbProvider : IDisposable
	{
		/// <summary>
		/// To check uniqueness of data in the database. It effects performance.
		/// </summary>
		bool CheckUnique { get; set; }

		/// <summary>
		/// Create table if not exist.
		/// </summary>
		/// <param name="table">Table.</param>
		void CreateIfNotExists(Table table);

		/// <summary>
		/// Insert new values.
		/// </summary>
		/// <param name="table">Table.</param>
		/// <param name="parameters">Parameters.</param>
		void InsertBatch(Table table, IEnumerable<IDictionary<string, object>> parameters);
	}
}
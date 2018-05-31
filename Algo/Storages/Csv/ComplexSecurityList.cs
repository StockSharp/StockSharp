namespace StockSharp.Algo.Storages.Csv
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;

	/// <summary>
	/// CSV storage of complex securities.
	/// </summary>
	/// <typeparam name="TSecurity">Type of security.</typeparam>
	public abstract class ComplexCsvSecurityList<TSecurity> : CsvEntityList<Security>, ISecurityStorage
		where TSecurity : Security
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ComplexCsvSecurityList{TSecurity}"/>.
		/// </summary>
		/// <param name="registry">The CSV storage of trading objects.</param>
		/// <param name="fileName">CSV file name.</param>
		protected ComplexCsvSecurityList(CsvEntityRegistry registry, string fileName)
			: base(registry, fileName)
		{
			((ICollectionEx<Security>)this).AddedRange += s => _added?.Invoke(s);
			((ICollectionEx<Security>)this).RemovedRange += s => _removed?.Invoke(s);
		}

		/// <inheritdoc />
		protected override object GetKey(Security item)
		{
			return item.Id;
		}

		/// <inheritdoc />
		protected override void Write(CsvFileWriter writer, Security data)
		{
			writer.WriteRow(new[]
			{
				data.Id,
				CreateText((TSecurity)data),
				data.Decimals.To<string>(),
				data.PriceStep.To<string>(),
				data.VolumeStep.To<string>()
			});
		}

		/// <inheritdoc />
		protected override Security Read(FastCsvReader reader)
		{
			var id = reader.ReadString();
			var security = CreateSecurity(reader.ReadString());

			var secId = id.ToSecurityId();

			security.Id = id;
			security.Code = secId.SecurityCode;
			security.Board = Registry.GetBoard(secId.BoardCode);
			security.Decimals = reader.ReadNullableInt();
			security.PriceStep = reader.ReadNullableDecimal();
			security.VolumeStep = reader.ReadNullableDecimal();

			return security;
		}

		/// <summary>
		/// Convert text to <typeparamref name="TSecurity" /> instance.
		/// </summary>
		/// <param name="text">Text.</param>
		/// <returns><typeparamref name="TSecurity" /> instance.</returns>
		protected abstract TSecurity CreateSecurity(string text);

		/// <summary>
		/// Convert <typeparamref name="TSecurity" /> instance to text.
		/// </summary>
		/// <param name="security"><typeparamref name="TSecurity" /> instance.</param>
		/// <returns>Text.</returns>
		protected abstract string CreateText(TSecurity security);

		void IDisposable.Dispose()
		{
		}

		private Action<IEnumerable<Security>> _added;

		event Action<IEnumerable<Security>> ISecurityProvider.Added
		{
			add => _added += value;
			remove => _added -= value;
		}

		private Action<IEnumerable<Security>> _removed;

		event Action<IEnumerable<Security>> ISecurityProvider.Removed
		{
			add => _removed += value;
			remove => _removed -= value;
		}

		IEnumerable<Security> ISecurityProvider.Lookup(Security criteria) => this.Filter(criteria);

		void ISecurityStorage.Delete(Security security) => Remove(security);

		void ISecurityStorage.DeleteBy(Security criteria) => this.Filter(criteria).ForEach(s => Remove(s));
	}

	/// <summary>
	/// CSV storage of continuous securities.
	/// </summary>
	public class ContinuousCsvSecurityList : ComplexCsvSecurityList<ContinuousSecurity>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ContinuousCsvSecurityList"/>.
		/// </summary>
		/// <param name="registry">The CSV storage of trading objects.</param>
		public ContinuousCsvSecurityList(CsvEntityRegistry registry)
			: base(registry, "security_continuous.csv")
		{
		}

		/// <inheritdoc />
		protected override ContinuousSecurity CreateSecurity(string text)
		{
			var security = new ContinuousSecurity();
			security.FromSerializedString(text);
			return security;
		}

		/// <inheritdoc />
		protected override string CreateText(ContinuousSecurity security)
		{
			return security.ToSerializedString();
		}
	}
}
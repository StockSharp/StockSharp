namespace StockSharp.Algo.Storages.Backup
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;

	using Amazon;

	using Ecng.Common;

	/// <summary>
	/// Вспомогательный класс для AWS.
	/// </summary>
	public static class AmazonExtensions
	{
		private static RegionEndpoint[] _endpoints;

		/// <summary>
		/// Все регионы.
		/// </summary>
		public static IEnumerable<RegionEndpoint> Endpoints
		{
			get
			{
				lock (typeof(AmazonExtensions))
				{
					if (_endpoints == null)
					{
						_endpoints = typeof(RegionEndpoint)
							.GetFields(BindingFlags.Static | BindingFlags.Public)
							.Where(f => f.FieldType == typeof(RegionEndpoint))
							.Select(f => (RegionEndpoint)f.GetValue(null))
							.ToArray();
					}
				}

				return _endpoints;
			}
		}

		/// <summary>
		/// Получить регион по имени.
		/// </summary>
		/// <param name="name">Имя региона.</param>
		/// <returns>Регион.</returns>
		public static RegionEndpoint GetEndpoint(string name)
		{
			return Endpoints.First(e =>
				e.SystemName.CompareIgnoreCase(name) ||
				e.SystemName.Replace("-", string.Empty).CompareIgnoreCase(name) ||
				e.DisplayName.CompareIgnoreCase(name));
		}
	}
}
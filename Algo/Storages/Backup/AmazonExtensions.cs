#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Backup.Algo
File: AmazonExtensions.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages.Backup
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;

	using Amazon;

	using Ecng.Common;

	/// <summary>
	/// Extension class for AWS.
	/// </summary>
	public static class AmazonExtensions
	{
		private static RegionEndpoint[] _endpoints;

		/// <summary>
		/// All regions.
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
		/// Get region by name.
		/// </summary>
		/// <param name="name">Region name.</param>
		/// <returns>Region.</returns>
		public static RegionEndpoint GetEndpoint(string name)
		{
			return Endpoints.First(e =>
				e.SystemName.CompareIgnoreCase(name) ||
				e.SystemName.Replace("-", string.Empty).CompareIgnoreCase(name) ||
				e.DisplayName.CompareIgnoreCase(name));
		}
	}
}
#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: IExtendableEntity.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;

	/// <summary>
	/// The interface for all trading types that have the property <see cref="IExtendableEntity.ExtensionInfo"/> for keeping extended information.
	/// </summary>
	public interface IExtendableEntity
	{
		/// <summary>
		/// Extended information.
		/// </summary>
		/// <remarks>
		/// Required when extra information is stored in the program.
		/// </remarks>
		IDictionary<string, object> ExtensionInfo { get; set; }
	}

	/// <summary>
	/// Extension class for <see cref="IExtendableEntity.ExtensionInfo"/>.
	/// </summary>
	public static class ExtandableEntityHelper
	{
		/// <summary>
		/// Add value into <see cref="IExtendableEntity.ExtensionInfo"/>.
		/// </summary>
		/// <param name="entity">Entity.</param>
		/// <param name="key">Key.</param>
		/// <param name="value">Value.</param>
		public static void AddValue(this IExtendableEntity entity, string key, object value)
		{
			entity.GetExtInfo(true)[key] = value;
		}

		/// <summary>
		/// Get value from <see cref="IExtendableEntity.ExtensionInfo"/>.
		/// </summary>
		/// <typeparam name="T">Value type.</typeparam>
		/// <param name="entity">Entity.</param>
		/// <param name="key">Key.</param>
		/// <returns>Value.</returns>
		public static T GetValue<T>(this IExtendableEntity entity, string key)
		{
			var info = entity.GetExtInfo(false);

			if (info == null)
				return default(T);

			return (T)(info.TryGetValue(key) ?? default(T));
		}

		private static IDictionary<string, object> GetExtInfo(this IExtendableEntity entity, bool createIfNotExist)
		{
			if (entity == null)
				throw new ArgumentNullException(nameof(entity));

			var info = entity.ExtensionInfo;

			if (info == null && createIfNotExist)
			{
				info = new SynchronizedDictionary<string, object>();
				entity.ExtensionInfo = info;
			}

			return info;
		}
	}
}
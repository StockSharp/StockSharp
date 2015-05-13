namespace StockSharp.IQFeed.Metadata
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// 
	/// </summary>
	internal class IQFeedLevel1FundamentalFields
	{
		private static readonly IList<IQFeedMessageField> _allFields = new List<IQFeedMessageField>();

		private static IQFeedMessageField CreateField(IQFeedMessageType messageType, string name, Type type)
		{
			var field = new IQFeedMessageField(messageType, name, type);

			_allFields.Add(field);

			return field;
		}

		/// <summary>
		/// 
		/// </summary>
		public static IEnumerable<IQFeedMessageField> AllFields
		{
			get { return _allFields; }
		}

		/// <summary>
		/// 
		/// </summary>
		public static readonly IQFeedMessageField Symbol = CreateField(IQFeedMessageType.SecurityFundamental, "Symbol", typeof (string));
	}
}
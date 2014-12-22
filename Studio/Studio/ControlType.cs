namespace StockSharp.Studio
{
	using System;

	public class ControlType : Tuple<Type, string, string, Uri>
	{
		public ControlType(Type item1, string item2, string item3, Uri item4)
			: base(item1, item2, item3, item4)
		{
		}
	}
}
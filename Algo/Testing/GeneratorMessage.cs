#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Testing.Algo
File: GeneratorMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Testing
{
	using StockSharp.Messages;

	/// <summary>
	/// The message about creation or deletion of the market data generator.
	/// </summary>
	public class GeneratorMessage : MarketDataMessage
	{
		/// <summary>
		/// The market data generator.
		/// </summary>
		public MarketDataGenerator Generator { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="GeneratorMessage"/>.
		/// </summary>
		public GeneratorMessage()
			: base(ExtendedMessageTypes.Generator)
		{
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		public void CopyTo(GeneratorMessage destination)
		{
			base.CopyTo(destination);

			destination.Generator = Generator?.Clone();
		}

		/// <summary>
		/// Create a copy of <see cref="GeneratorMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new GeneratorMessage();
			CopyTo(clone);
			return clone;
		}
	}
}
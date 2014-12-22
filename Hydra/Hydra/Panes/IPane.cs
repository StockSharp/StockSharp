namespace StockSharp.Hydra.Panes
{
	using System;

	using Ecng.Serialization;

	public interface IPane : IPersistable, IDisposable
	{
		string Title { get; }

		Uri Icon { get; }

		bool IsValid { get; }
	}
}
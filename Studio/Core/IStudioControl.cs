namespace StockSharp.Studio.Core
{
	using System;

	using Ecng.Serialization;

	public interface IStudioControl : IPersistable, IDisposable
	{
		string Title { get; }
		Uri Icon { get; }
	}
}
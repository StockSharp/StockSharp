namespace StockSharp.Algo.Storages;

/// <summary>
/// Meta-information on data for one day.
/// </summary>
public interface IMarketDataMetaInfo
{
	/// <summary>
	/// Date of day.
	/// </summary>
	DateTime Date { get; }

	/// <summary>
	/// Number of data.
	/// </summary>
	int Count { get; set; }

	/// <summary>
	/// Value <see cref="Security.PriceStep"/> at day <see cref="Date"/>.
	/// </summary>
	decimal PriceStep { get; set; }

	/// <summary>
	/// Value <see cref="Security.VolumeStep"/> at day <see cref="Date"/>.
	/// </summary>
	decimal VolumeStep { get; set; }

	/// <summary>
	/// First record time.
	/// </summary>
	DateTime FirstTime { get; set; }

	/// <summary>
	/// Last record time.
	/// </summary>
	DateTime LastTime { get; set; }

	/// <summary>
	/// Last record identifier.
	/// </summary>
	object LastId { get; set; }

	/// <summary>
	/// To save meta-information parameters to stream.
	/// </summary>
	/// <param name="stream">Data stream.</param>
	void Write(Stream stream);

	/// <summary>
	/// To load meta-information parameters from the stream.
	/// </summary>
	/// <param name="stream">Data stream.</param>
	void Read(Stream stream);

	/// <summary>
	/// Is override all data.
	/// </summary>
	bool IsOverride { get; }
}

abstract class MetaInfo(DateTime date) : IMarketDataMetaInfo
{
	public DateTime Date { get; } = date;
	public int Count { get; set; }

	public decimal PriceStep { get; set; } = 0.01m;
	public decimal VolumeStep { get; set; } = 1m;

	//public decimal FirstPriceStep { get; set; }
	public decimal LastPriceStep { get; set; }

	public DateTime FirstTime { get; set; }
	public DateTime LastTime { get; set; }

	public abstract object LastId { get; set; }

	/// <inheritdoc />
	public abstract void Write(Stream stream);

	/// <inheritdoc />
	public abstract void Read(Stream stream);

	/// <inheritdoc />
	public virtual bool IsOverride => false;
}
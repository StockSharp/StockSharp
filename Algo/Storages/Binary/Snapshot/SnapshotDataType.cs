namespace StockSharp.Algo.Storages.Binary.Snapshot;

using System.Runtime.InteropServices;

using Ecng.Interop;

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
struct SnapshotDataType
{
	public int MessageType;
	public long Arg1;
	public BlittableDecimal Arg2;
	public int Arg3;

	public static explicit operator SnapshotDataType(DataType dt)
	{
		if (dt is null)
			throw new ArgumentNullException(nameof(dt));

		var (messageType, arg1, arg2, arg3) = dt.Extract();

		return new SnapshotDataType
		{
			MessageType = messageType,
			Arg1 = arg1,
			Arg2 = (BlittableDecimal)arg2,
			Arg3 = arg3,
		};
	}

	public static implicit operator DataType(SnapshotDataType dt)
	{
		return dt.MessageType.ToDataType(dt.Arg1, dt.Arg2, dt.Arg3);
	}

	public override string ToString() => ((DataType)this).ToString();
}
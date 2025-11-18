namespace StockSharp.Algo.Storages.Binary.Snapshot;

struct SnapshotDataType
{
	public int MessageType;
	public long Arg1;
	public decimal Arg2;
	public int Arg3;

	public const int Size = sizeof(int) + sizeof(long) + sizeof(decimal) + sizeof(int);

	public static explicit operator SnapshotDataType(DataType dt)
	{
		if (dt is null)
			throw new ArgumentNullException(nameof(dt));

		var (messageType, arg1, arg2, arg3) = dt.Extract();

		return new()
		{
			MessageType = messageType,
			Arg1 = arg1,
			Arg2 = arg2,
			Arg3 = arg3,
		};
	}

	public static implicit operator DataType(SnapshotDataType dt)
	{
		return dt.MessageType.ToDataType(dt.Arg1, dt.Arg2, dt.Arg3);
	}

	public override readonly string ToString() => ((DataType)this).ToString();

	public readonly void Write(ref SpanWriter writer)
	{
		writer.WriteInt32(MessageType);
		writer.WriteInt64(Arg1);
		writer.WriteDecimal(Arg2);
		writer.WriteInt32(Arg3);
	}

	public static SnapshotDataType Read(ref SpanReader reader)
	{
		return new()
		{
			MessageType = reader.ReadInt32(),
			Arg1 = reader.ReadInt64(),
			Arg2 = reader.ReadDecimal(),
			Arg3 = reader.ReadInt32(),
		};
	}
}
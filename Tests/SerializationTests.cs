namespace StockSharp.Tests;

using System.Collections;

using Ecng.Reflection;

using StockSharp.Algo.Storages.Binary.Snapshot;

[TestClass]
public class SerializationTests
{
	[TestMethod]
	public void SerializeBoard()
	{
		var boards = typeof(ExchangeBoard)
			.GetProperties(BindingFlags.Static | BindingFlags.Public)
			.Where(p => p.PropertyType == typeof(ExchangeBoard))
			.Select(p => (ExchangeBoard)p.GetValue(null))
			.ToArray();

		foreach (var board in boards)
			SerializeEntity(board);
	}

	[TestMethod]
	public void SerializeExchange()
	{
		var exchanges = typeof(Exchange)
			.GetProperties(BindingFlags.Static | BindingFlags.Public)
			.Where(p => p.PropertyType == typeof(Exchange))
			.Select(p => (Exchange)p.GetValue(null))
			.ToArray();

		foreach (var exchange in exchanges)
		{
			SerializeEntity(exchange);
		}
	}

	[TestMethod]
	public void SerializePersistables()
	{
		var assemblies = new[]
		{
			typeof(Message).Assembly,
			typeof(Order).Assembly,
			typeof(Connector).Assembly
		};

		var objects = assemblies
			.SelectMany(a => a.FindImplementations<IPersistable>(false, false, extraFilter: t => t.GetConstructor(Type.EmptyTypes) != null))
			.Select(t => t.CreateInstance<IPersistable>())
			.ToArray();

		var genMethod = GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Static).First(m => m.Name == nameof(SerializeEntity) && m.GetParameters().Length == 1);

		for (var i=0; i<objects.Length; ++i)
		{
			var o = objects[i];

			if(o is IIndicator ind)
				ind.Reset();

			genMethod.Make(o.GetType()).Invoke(null, [o]);
		}
	}

	private static void SerializeEntity<T>(T entity)
		where T : class, IPersistable
	{
		ArgumentNullException.ThrowIfNull(entity);

		var ser = Paths.CreateSerializer<T>();

		Helper.CheckEqual(entity.Save(), ser.Deserialize(ser.Serialize(entity)).Save());
	}

	[TestMethod]
	public void TransactionsSnapshot()
	{
		ISnapshotSerializer<string, ExecutionMessage> serializer = new TransactionBinarySnapshotSerializer();

		var idGen = new IncrementalIdGenerator();

		foreach (var adapter in new InMemoryMessageAdapterProvider([]).PossibleAdapters)
		{
			var condition = adapter.CreateOrderCondition();

			if (condition == null)
				continue;

			static void FillProps(object s)
			{
				var props = s.GetType().GetMembers<PropertyInfo>();

				foreach (var prop in props)
				{
					if (prop.Name == "Parameters" || prop.ReflectedType != s.GetType())
						continue;

					if (prop.SetMethod == null)
					{
						FillProps(prop.GetValue(s));
						continue;
					}

					object value;

					if (prop.PropertyType == typeof(string))
						value = Guid.NewGuid().ToString();
					else if (prop.PropertyType.GetGenericType(typeof(IDictionary<,>)) != null)
					{
						var args = prop.PropertyType.GetGenericArguments();
						var dict = (IDictionary)typeof(Dictionary<,>).Make(args).CreateInstance();
						//list.Add(itemType.CreateInstance());
						//dict.Add(args[0].CreateInstance(), args[1].CreateInstance());
						value = dict;
					}
					else if (prop.PropertyType.GetGenericType(typeof(IEnumerable<>)) != null)
					{
						var itemType = prop.PropertyType.GetGenericTypeArg(typeof(IEnumerable<>), 0);
						var list = (IList)typeof(List<>).Make(itemType).CreateInstance();
						//list.Add(itemType.CreateInstance());
						value = list;
					}
					else
						value = prop.PropertyType.CreateInstance();

					prop.SetValue(s, value);
				}
			}

			FillProps(condition);

			var origin = new ExecutionMessage
			{
				SecurityId = new SecurityId
				{
					SecurityCode = "AAPL",
					BoardCode = BoardCodes.Nasdaq
				},
				DataTypeEx = DataType.Transactions,
				TransactionId = idGen.GetNextId(),
				Condition = condition
			};

			var bytes = serializer.Serialize(serializer.Version, origin);
			var loaded = serializer.Deserialize(serializer.Version, bytes);
		}
	}
}
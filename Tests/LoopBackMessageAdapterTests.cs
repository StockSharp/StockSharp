namespace StockSharp.Tests;

/// <summary>
/// Some messages a pipeline emits are not answers to the caller, they are instructions the pipeline
/// addressed to itself: a heartbeat that is due, a reconnect that has to be attempted. They leave
/// through the out side and have to come back in through the in side, or they are simply lost. The
/// wrapper here is the only thing that turns them around for a consumer that subscribed to the
/// pipeline directly, so what it owes is exactly that: a loopback goes back in, and never reaches
/// the consumer as though it were data.
/// </summary>
[TestClass]
public class LoopBackMessageAdapterTests : BaseTestClass
{
	/// <summary>The pipeline underneath: it records what it is sent and emits only when told to.</summary>
	private sealed class RecordingAdapter : MessageAdapter
	{
		public RecordingAdapter()
			: base(new IncrementalIdGenerator())
		{
		}

		/// <summary>Everything that came back down the in side, in arrival order.</summary>
		public List<Message> InMessages { get; } = [];

		protected override ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
		{
			InMessages.Add(message);
			return default;
		}

		/// <summary>Pushes a message up the out side, as the pipeline does when it has something to say.</summary>
		public ValueTask EmitAsync(Message message, CancellationToken cancellationToken)
			=> SendOutMessageAsync(message, cancellationToken);

		public override ValueTask<IMessageAdapter> CloneAsync(CancellationToken cancellationToken)
			=> new(new RecordingAdapter());
	}

	private static (LoopBackMessageAdapter adapter, RecordingAdapter inner, List<Message> output) CreateSut()
	{
		var inner = new RecordingAdapter();
		var adapter = new LoopBackMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, _) => { output.Add(m); return default; };

		return (adapter, inner, output);
	}

	/// <summary>
	/// A heartbeat is a message the pipeline sends to itself to keep the connection proved alive. It
	/// surfaces on the out side marked as loopback, and unless something puts it back in, nothing ever
	/// acts on it: the heartbeat never goes to the venue, the silence is never noticed, and a
	/// connection that has quietly died goes on looking healthy to everyone above it.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task LoopbackMessage_IsPutBackIntoThePipeline()
	{
		var (adapter, inner, _) = CreateSut();

		var heartbeat = new TimeMessage().LoopBack(adapter);

		await inner.EmitAsync(heartbeat, CancellationToken);

		IsTrue(inner.InMessages.Contains(heartbeat),
			"a loopback message the pipeline emitted was never put back into it, so nothing will ever act on it");
	}

	/// <summary>
	/// The other half of the same promise, and the half a consumer feels. A loopback is an instruction
	/// the pipeline addressed to itself - here the mass cancel it raises to flatten what is working -
	/// not an answer to anything the consumer asked for. Handed out, it reaches code that reads out
	/// messages as market data and transaction results, where a bare cancel request is at best
	/// ignored and at worst read as a report that something was cancelled.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task LoopbackMessage_IsNotHandedToTheConsumer()
	{
		var (adapter, inner, output) = CreateSut();

		var massCancel = new OrderGroupCancelMessage { TransactionId = 4711 }.LoopBack(adapter);

		await inner.EmitAsync(massCancel, CancellationToken);

		IsFalse(output.Contains(massCancel),
			"a loopback message is an instruction the pipeline addressed to itself and must not be delivered as an out message");
		IsTrue(inner.InMessages.Contains(massCancel),
			"and it must have gone back into the pipeline instead, or the mass cancel is simply lost");
	}

	/// <summary>
	/// Turning loopbacks around is the whole job, and it must not cost the consumer its ordinary
	/// traffic. A wrapper that swallowed or altered plain out messages would silence the pipeline it
	/// was added to protect.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task OrdinaryOutMessage_StillReachesTheConsumer()
	{
		var (adapter, inner, output) = CreateSut();

		var connected = new ConnectMessage();

		await inner.EmitAsync(connected, CancellationToken);

		HasCount(1, output, "an ordinary out message must pass the wrapper exactly once");
		AreSame(connected, output[0], "an ordinary out message must reach the consumer as it was emitted");
		IsFalse(inner.InMessages.Contains(connected), "an ordinary out message must not be pushed back into the pipeline");
	}

	/// <summary>
	/// Adapters are cloned before use, one copy per connection, and the copy is what actually runs. A
	/// clone that kept the original's pipeline would turn two connections into one, and a clone that
	/// stopped turning loopbacks around would lose every heartbeat on the connection it serves - while
	/// the original, the one nobody runs, went on looking correct.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Clone_KeepsTurningLoopbacksAroundOnItsOwnPipeline()
	{
		var (adapter, inner, _) = CreateSut();

		var clone = (LoopBackMessageAdapter)await adapter.CloneAsync(CancellationToken);

		AreNotSame(inner, clone.InnerAdapter, "a clone must carry its own pipeline, not share the original's");

		var cloneInner = (RecordingAdapter)clone.InnerAdapter;

		var heartbeat = new TimeMessage().LoopBack(clone);

		await cloneInner.EmitAsync(heartbeat, CancellationToken);

		IsTrue(cloneInner.InMessages.Contains(heartbeat),
			"the clone is the copy that actually runs, so it must put loopbacks back into its own pipeline");
		IsEmpty(inner.InMessages, "the clone must not feed the original's pipeline");
	}
}

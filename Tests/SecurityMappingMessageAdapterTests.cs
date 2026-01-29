namespace StockSharp.Tests;

using StockSharp.Algo.Storages;

[TestClass]
public class SecurityMappingMessageAdapterTests : BaseTestClass
{
	private static ISecurityMappingStorageProvider CreateProvider() => new InMemorySecurityMappingStorageProvider();

	#region Constructor Tests

	[TestMethod]
	public void Constructor_NullProvider_ThrowsArgumentNullException()
	{
		var inner = new RecordingMessageAdapter();

		var thrown = false;
		try
		{
			_ = new SecurityMappingMessageAdapter(inner, null);
		}
		catch (ArgumentNullException)
		{
			thrown = true;
		}
		thrown.AssertTrue();
	}

	[TestMethod]
	public void Constructor_WithManager_NullManager_ThrowsArgumentNullException()
	{
		var inner = new RecordingMessageAdapter();
		var provider = CreateProvider();

		var thrown = false;
		try
		{
			_ = new SecurityMappingMessageAdapter(inner, provider, null);
		}
		catch (ArgumentNullException)
		{
			thrown = true;
		}
		thrown.AssertTrue();
	}

	[TestMethod]
	public void Constructor_WithManager_NullProvider_ThrowsArgumentNullException()
	{
		var inner = new RecordingMessageAdapter();
		var manager = new Mock<ISecurityMappingManager>();

		var thrown = false;
		try
		{
			_ = new SecurityMappingMessageAdapter(inner, null, manager.Object);
		}
		catch (ArgumentNullException)
		{
			thrown = true;
		}
		thrown.AssertTrue();
	}

	#endregion

	#region SendInMessage Tests

	[TestMethod]
	public async Task SendInMessage_DelegatesToManager_AndRoutesMessage()
	{
		var inner = new RecordingMessageAdapter();
		var manager = new Mock<ISecurityMappingManager>();

		var message = new SecurityMessage
		{
			SecurityId = Helper.CreateSecurityId()
		};

		manager
			.Setup(m => m.ProcessInMessage(It.IsAny<Message>()))
			.Returns((message: message, forward: true));

		var provider = CreateProvider();
		using var adapter = new SecurityMappingMessageAdapter(inner, provider, manager.Object);

		await adapter.SendInMessageAsync(message, CancellationToken);

		inner.InMessages.Count.AssertEqual(1);
		inner.InMessages[0].AssertSame(message);

		manager.Verify(m => m.ProcessInMessage(It.IsAny<Message>()), Times.Once);
	}

	[TestMethod]
	public async Task SendInMessage_WhenForwardFalse_DoesNotRouteToInner()
	{
		var inner = new RecordingMessageAdapter();
		var manager = new Mock<ISecurityMappingManager>();

		var message = new SecurityMessage
		{
			SecurityId = Helper.CreateSecurityId()
		};

		manager
			.Setup(m => m.ProcessInMessage(It.IsAny<Message>()))
			.Returns((message: (Message)null, forward: false));

		var provider = CreateProvider();
		using var adapter = new SecurityMappingMessageAdapter(inner, provider, manager.Object);

		await adapter.SendInMessageAsync(message, CancellationToken);

		inner.InMessages.Count.AssertEqual(0);
	}

	[TestMethod]
	public async Task SendInMessage_WhenMessageNull_DoesNotRouteToInner()
	{
		var inner = new RecordingMessageAdapter();
		var manager = new Mock<ISecurityMappingManager>();

		var message = new SecurityMessage
		{
			SecurityId = Helper.CreateSecurityId()
		};

		manager
			.Setup(m => m.ProcessInMessage(It.IsAny<Message>()))
			.Returns((message: (Message)null, forward: true));

		var provider = CreateProvider();
		using var adapter = new SecurityMappingMessageAdapter(inner, provider, manager.Object);

		await adapter.SendInMessageAsync(message, CancellationToken);

		inner.InMessages.Count.AssertEqual(0);
	}

	#endregion

	#region OutMessage Tests

	[TestMethod]
	public async Task OutMessage_DelegatesToManager_AndRoutesMessage()
	{
		var inner = new RecordingMessageAdapter();
		var manager = new Mock<ISecurityMappingManager>();

		var message = new SecurityMessage
		{
			SecurityId = Helper.CreateSecurityId()
		};

		manager
			.Setup(m => m.ProcessOutMessage(It.IsAny<Message>()))
			.Returns((message: message, forward: true));

		var provider = CreateProvider();
		using var adapter = new SecurityMappingMessageAdapter(inner, provider, manager.Object);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await inner.SendOutMessageAsync(message, CancellationToken);

		output.Count.AssertEqual(1);
		output[0].AssertSame(message);

		manager.Verify(m => m.ProcessOutMessage(It.IsAny<Message>()), Times.Once);
	}

	[TestMethod]
	public async Task OutMessage_WhenForwardFalse_DoesNotRouteOut()
	{
		var inner = new RecordingMessageAdapter();
		var manager = new Mock<ISecurityMappingManager>();

		var message = new SecurityMappingMessage
		{
			StorageName = "Test",
			Mapping = new SecurityIdMapping
			{
				StockSharpId = Helper.CreateSecurityId(),
				AdapterId = Helper.CreateSecurityId()
			}
		};

		manager
			.Setup(m => m.ProcessOutMessage(It.IsAny<Message>()))
			.Returns((message: (Message)null, forward: false));

		var provider = CreateProvider();
		using var adapter = new SecurityMappingMessageAdapter(inner, provider, manager.Object);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await inner.SendOutMessageAsync(message, CancellationToken);

		output.Count.AssertEqual(0);
	}

	[TestMethod]
	public async Task OutMessage_WhenMessageNull_DoesNotRouteOut()
	{
		var inner = new RecordingMessageAdapter();
		var manager = new Mock<ISecurityMappingManager>();

		var message = new SecurityMessage
		{
			SecurityId = Helper.CreateSecurityId()
		};

		manager
			.Setup(m => m.ProcessOutMessage(It.IsAny<Message>()))
			.Returns((message: (Message)null, forward: true));

		var provider = CreateProvider();
		using var adapter = new SecurityMappingMessageAdapter(inner, provider, manager.Object);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await inner.SendOutMessageAsync(message, CancellationToken);

		output.Count.AssertEqual(0);
	}

	#endregion

	#region Integration Tests (with real manager)

	[TestMethod]
	public async Task Integration_SecurityLookupMessage_PassesThrough()
	{
		var inner = new RecordingMessageAdapter();
		var provider = CreateProvider();

		using var adapter = new SecurityMappingMessageAdapter(inner, provider);

		var message = new SecurityLookupMessage { TransactionId = 123 };

		await adapter.SendInMessageAsync(message, CancellationToken);

		inner.InMessages.Count.AssertEqual(1);
		inner.InMessages[0].AssertSame(message);
	}

	[TestMethod]
	public async Task Integration_ResetMessage_PassesThrough()
	{
		var inner = new RecordingMessageAdapter();
		var provider = CreateProvider();

		using var adapter = new SecurityMappingMessageAdapter(inner, provider);

		var message = new ResetMessage();

		await adapter.SendInMessageAsync(message, CancellationToken);

		inner.InMessages.Count.AssertEqual(1);
		inner.InMessages[0].AssertSame(message);
	}

	#endregion

	#region Clone Tests

	[TestMethod]
	public void Clone_ReturnsNewInstanceWithSameProvider()
	{
		var inner = new RecordingMessageAdapter();
		var provider = CreateProvider();

		using var adapter = new SecurityMappingMessageAdapter(inner, provider);

		var clone = (SecurityMappingMessageAdapter)adapter.Clone();

		clone.AssertNotNull();
		clone.AssertNotSame(adapter);
		clone.Provider.AssertSame(provider);
	}

	#endregion
}

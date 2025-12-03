namespace StockSharp.Algo.Storages;

/// <summary>
/// ChannelExecutor group that maintains a reusable resource (like stream/writer).
/// Mimics the behavior of DelayAction.IGroup for better performance.
/// </summary>
/// <typeparam name="TResource">Type of the reusable resource (e.g., CsvFileWriter).</typeparam>
public sealed class ChannelExecutorGroup<TResource> : Disposable
	where TResource : class, IDisposable
{
	private readonly ChannelExecutor _executor;
	private readonly Func<TResource> _resourceFactory;
	private readonly Lock _lock = new();
	private TResource _resource;
	private bool _needRecreate;

	/// <summary>
	/// Initializes a new instance of the <see cref="ChannelExecutorGroup{TResource}"/>.
	/// </summary>
	/// <param name="executor">Sequential operation executor.</param>
	/// <param name="resourceFactory">Factory to create the reusable resource.</param>
	public ChannelExecutorGroup(ChannelExecutor executor, Func<TResource> resourceFactory)
	{
		_executor = executor ?? throw new ArgumentNullException(nameof(executor));
		_resourceFactory = resourceFactory ?? throw new ArgumentNullException(nameof(resourceFactory));
	}

	/// <summary>
	/// Add an action that uses the shared resource.
	/// </summary>
	/// <param name="action">Action to execute with the resource.</param>
	public void Add(Action<TResource> action)
	{
		if (action == null)
			throw new ArgumentNullException(nameof(action));

		_executor.Add(() =>
		{
			using (_lock.EnterScope())
			{
				if (_resource == null || _needRecreate)
				{
					_resource?.Dispose();
					_resource = _resourceFactory();
					_needRecreate = false;
				}

				action(_resource);
			}
		});
	}

	/// <summary>
	/// Add an action that uses the shared resource with additional data.
	/// </summary>
	/// <param name="action">Action to execute with the resource and data.</param>
	/// <param name="data">Data to pass to the action.</param>
	public void Add<TData>(Action<TResource, TData> action, TData data)
	{
		if (action == null)
			throw new ArgumentNullException(nameof(action));

		var dataCopy = data;
		_executor.Add(() =>
		{
			using (_lock.EnterScope())
			{
				if (_resource == null || _needRecreate)
				{
					_resource?.Dispose();
					_resource = _resourceFactory();
					_needRecreate = false;
				}

				action(_resource, dataCopy);
			}
		});
	}

	/// <summary>
	/// Force recreation of the resource on next operation.
	/// Use this when you need to change file mode (e.g., from Append to Create).
	/// </summary>
	public void RecreateResource()
	{
		using (_lock.EnterScope())
		{
			_needRecreate = true;
		}
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		base.DisposeManaged();

		using (_lock.EnterScope())
		{
			_resource?.Dispose();
			_resource = null;
		}
	}
}

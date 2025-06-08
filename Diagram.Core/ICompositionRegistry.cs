namespace StockSharp.Diagram;

using System.Security;

using Ecng.Security;

using StockSharp.Configuration;

/// <summary>
/// The storage of composite elements.
/// </summary>
public interface ICompositionRegistry
{
	/// <summary>
	/// List of elements.
	/// </summary>
	INotifyList<DiagramElement> DiagramElements { get; }

	/// <summary>
	/// To serialize the composite element.
	/// </summary>
	/// <param name="element"><see cref="CompositionDiagramElement"/></param>
	/// <param name="storage">Settings storage.</param>
	/// <param name="includeCoordinates">Include coordinates.</param>
	/// <param name="password">Password.</param>
	/// <returns>Settings storage.</returns>
	void Serialize(CompositionDiagramElement element, SettingsStorage storage, bool includeCoordinates, SecureString password);

	/// <summary>
	/// To deserialize the composite element.
	/// </summary>
	/// <param name="element"><see cref="CompositionDiagramElement"/></param>
	/// <param name="storage">Settings storage.</param>
	/// <param name="getPassword">Get password handler.</param>
	/// <returns>Is encryption used.</returns>
	bool Deserialize(CompositionDiagramElement element, SettingsStorage storage, Func<SecureString> getPassword);

	/// <summary>
	/// Create <see cref="CompositionDiagramElement"/> instance.
	/// </summary>
	/// <returns><see cref="CompositionDiagramElement"/></returns>
	CompositionDiagramElement CreateComposition();
}

/// <summary>
/// Default <see cref="ICompositionRegistry"/> implementation.
/// </summary>
/// <typeparam name="TNode">Node type.</typeparam>
/// <typeparam name="TLink">Link type.</typeparam>
public class CompositionRegistry<TNode, TLink> : ICompositionRegistry
	where TNode : ICompositionModelNode, new()
	where TLink : ICompositionModelLink, new()
{
	private class Keys
	{
		public const string Version = nameof(Version);
		public const string Type = nameof(Type);
		public const string Scheme = nameof(Scheme);
		public const string Encrypted = nameof(Encrypted);
		public const string Composition = nameof(Composition);
		public const string Settings = nameof(Settings);
		public const string TypeId = nameof(TypeId);
		public const string ElementModel = nameof(ElementModel);
	}

	private static readonly Version _minVersion = new(1, 0);

	private static readonly byte[] _initVectorBytes = "ss14fgty650h8u82".ASCII();

	private readonly SynchronizedDictionary<Guid, List<(TNode element, SettingsStorage settings)>> _notLoadedElements = [];
	private readonly Func<ICompositionModelBehavior<TNode, TLink>> _createBehavior;

	private readonly SynchronizedSet<DiagramElement> _diagramElements;

	/// <inheritdoc />
	public INotifyList<DiagramElement> DiagramElements => _diagramElements;

	/// <summary>
	/// Initializes a new instance of the <see cref="CompositionRegistry{TNode,TLink}"/>.
	/// </summary>
	/// <param name="createBehavior"><see cref="ICompositionModelBehavior{TNode,TLink}"/></param>
	public CompositionRegistry(Func<ICompositionModelBehavior<TNode, TLink>> createBehavior)
	{
		_createBehavior = createBehavior ?? throw new ArgumentNullException(nameof(createBehavior));

		_diagramElements = [];
		_diagramElements.Added += item =>
		{
			if (!_notLoadedElements.TryGetAndRemove(item.TypeId, out var list))
				return;

			foreach (var (element, settings) in list)
			{
				var clone = item.Clone(false);
				clone.Load(settings);
				element.Element = clone;
			}
		};
	}

	private CompositionModel<TNode, TLink> CreateModel() => new(_createBehavior());

	/// <inheritdoc />
	public CompositionDiagramElement CreateComposition() => new(CreateModel());

	/// <inheritdoc />
	public void Serialize(CompositionDiagramElement element, SettingsStorage container, bool includeCoordinates, SecureString password)
	{
		if (element is null)	throw new ArgumentNullException(nameof(element));
		if (container is null)	throw new ArgumentNullException(nameof(container));

		var settings = SaveElement(element, includeCoordinates);

		container.Set(Keys.Version, _minVersion.ToString());

		if (password is null)
		{
			container.Set(Keys.Scheme, settings);
		}
		else
		{
			var encryptedStr = settings
				.SerializeInvariant()
				.Encrypt(password.UnSecure(), _initVectorBytes, _initVectorBytes)
				.Base64();

			container
				.Set(Keys.Encrypted, true)
				.Set(Keys.Scheme, encryptedStr);
		}
	}

	/// <inheritdoc />
	public bool Deserialize(CompositionDiagramElement element, SettingsStorage container, Func<SecureString> getPassword)
	{
		if (element is null)		throw new ArgumentNullException(nameof(element));
		if (container is null)		throw new ArgumentNullException(nameof(container));
		if (getPassword is null)	throw new ArgumentNullException(nameof(getPassword));

		var version = container.GetValue<Version>(Keys.Version);

		if (version is null || version > _minVersion)
			throw new InvalidOperationException(LocalizedStrings.UnsupportedSchemeVersionParams.Put(version));

		var isEncrypted = container.GetValue<bool>(Keys.Encrypted);

		SettingsStorage storage;

		if (!isEncrypted)
		{
			storage = container.GetValue<SettingsStorage>(Keys.Scheme);
		}
		else
		{
			var scheme = container.GetValue<string>(Keys.Scheme);

			storage = scheme
				.Base64()
				.Decrypt(getPassword().UnSecure(), _initVectorBytes, _initVectorBytes)
				.DeserializeInvariant();
		}

		LoadElement(element, storage);

		return isEncrypted;
	}

	private void LoadElement(CompositionDiagramElement element, SettingsStorage storage)
	{
		if (element is null)
			throw new ArgumentNullException(nameof(element));

		if (storage is null)
			throw new ArgumentNullException(nameof(storage));

		element.SchemaVersion = storage.GetValue(nameof(element.SchemaVersion), element.SchemaVersion);
		element.Category = storage.GetValue(nameof(element.Category), string.Empty);
		element.DocUrl = storage.GetValue(nameof(element.DocUrl), string.Empty);
		element.Model = DeserializeModel(storage.GetValue<SettingsStorage>(nameof(element.Model)));
		element.Load(storage, Keys.Composition);
	}

	private SettingsStorage SaveElement(CompositionDiagramElement element, bool includeCoordinates)
	{
		if (element is null)
			throw new ArgumentNullException(nameof(element));

		var storage = new SettingsStorage();

		storage
			.Set(nameof(element.SchemaVersion), element.SchemaVersion)
			.Set(nameof(element.Category), element.Category)
			.Set(nameof(element.DocUrl), element.DocUrl)
			.Set(Keys.Composition, element.Save())
			.Set(nameof(element.Model), SerializeModel((CompositionModel<TNode, TLink>)element.Model, includeCoordinates))
		;

		return storage;
	}

	private SettingsStorage SerializeModel(CompositionModel<TNode, TLink> model, bool includeCoordinates)
	{
		SettingsStorage SerializeNode(TNode item)
		{
			var storage = new SettingsStorage();

			storage
				.Set(nameof(item.Key), item.Key)
				.Set(nameof(item.Figure), item.Figure)
			;

			if (includeCoordinates)
			{
				storage
					.Set(nameof(item.Location.X), item.Location.X)
					.Set(nameof(item.Location.Y), item.Location.Y)
				;
			}

			if (item.Element != null)
			{
				storage
					.Set(Keys.TypeId, item.Element.TypeId)
					.Set(Keys.Settings, item.Element.Save());
			}
			else
			{
				storage.SetValue(Keys.TypeId, item.TypeId);

				if (!_notLoadedElements.TryGetValue(item.TypeId, out var elements))
					return storage;

				var element = elements.Where(i => ReferenceEquals(i.element, item)).FirstOr();

				if (element != null)
					storage.SetValue(Keys.Settings, element.Value.settings);
			}

			return storage;
		}

		static SettingsStorage SerializeLink(TLink item) => new SettingsStorage()
			.Set(nameof(item.From), item.From)
			.Set(nameof(item.FromPort), item.FromPort)
			.Set(nameof(item.To), item.To)
			.Set(nameof(item.ToPort), item.ToPort)
		;

		return new SettingsStorage()
			.Set(nameof(model.Nodes), model.Nodes.Select(SerializeNode).ToArray())
			.Set(nameof(model.Links), model.Links.Select(SerializeLink).ToArray());
	}

	private ICompositionModel DeserializeModel(SettingsStorage storage)
	{
		var model = CreateModel();

		TNode DeserializeNode(SettingsStorage storage)
		{
			void AddNotLoadedElement(Guid typeId, TNode baseElement, SettingsStorage settings, string error)
			{
				_notLoadedElements
					.SafeAdd(typeId)
					.Add((baseElement, settings));

				baseElement.Element = null;
				baseElement.Text = error;
			}

			var x = storage.GetValue<float>(nameof(ICompositionModelNode.Location.X));
			var y = storage.GetValue<float>(nameof(ICompositionModelNode.Location.Y));

			var baseElement = new TNode
			{
				Key = storage.GetValue<string>(nameof(ICompositionModelNode.Key)),
				Location = new(x, y),
				Figure = storage.GetValue<string>(nameof(ICompositionModelNode.Figure)),
			};

			var hasModel = storage.ContainsKey(Keys.ElementModel);

			if (!hasModel)
			{
				var typeId = storage.GetValue<string>(Keys.TypeId).To<Guid>();
				var settings = storage.GetValue<SettingsStorage>(Keys.Settings);

				baseElement.TypeId = typeId;

				var element = DiagramElements.FirstOrDefault(e => e.TypeId == typeId);

				if (element != null)
				{
					try
					{
						element = element.Clone(false);

						if (settings != null)
							element.Load(settings);

						baseElement.Element = element;
					}
					catch (Exception excp)
					{
						excp.LogError();

						AddNotLoadedElement(typeId, baseElement, settings, excp.Message);
					}
				}
				else
					AddNotLoadedElement(typeId, baseElement, settings, LocalizedStrings.ElementWithTypeNotFound.Put(typeId));
			}
			else
			{
				var settings = storage.GetValue<SettingsStorage>(Keys.ElementModel);
				var element = CreateComposition();

				LoadElement(element, settings);

				baseElement.Element = element;
			}

			return baseElement;
		}

		static TLink DeserializeLink(SettingsStorage storage) => new()
		{
			From = storage.GetValue<string>(nameof(ICompositionModelLink.From)),
			FromPort = storage.GetValue<string>(nameof(ICompositionModelLink.FromPort)),
			To = storage.GetValue<string>(nameof(ICompositionModelLink.To)),
			ToPort = storage.GetValue<string>(nameof(ICompositionModelLink.ToPort))
		};

		model.ExecuteTransaction("Load", m =>
		{
			m.Nodes = new ObservableCollection<TNode>(storage.GetValue<SettingsStorage[]>(nameof(m.Nodes)).Select(DeserializeNode));
			m.Links = new ObservableCollection<TLink>(storage.GetValue<SettingsStorage[]>(nameof(m.Links)).Select(DeserializeLink));
		});

		return model;
	}
}

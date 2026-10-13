namespace StockSharp.Tests;

using System.ComponentModel;

using StockSharp.Charting;
using StockSharp.Diagram;
using StockSharp.Diagram.Elements;

/// <summary>
/// The behaviour is the store a diagram host reads and writes its graph through, and its notifications are
/// the only way that host learns the graph moved on. What it owes is therefore not a particular constant
/// on an event but an answer that is current by the time it is given: which links are still there, which
/// node has to be read again, and whether the diagram changed at all.
/// </summary>
[TestClass]
public class InMemoryCompositionModelBehaviorTests : BaseTestClass
{
	/// <summary>
	/// A behaviour with the model a host actually reads it through, and the one call a test needs to put
	/// an element into it.
	/// </summary>
	private sealed class Graph
	{
		public Graph()
		{
			Behavior = new();
			Model = new(Behavior);
		}

		public InMemoryCompositionModelBehavior Behavior { get; }
		public CompositionModel<InMemoryCompositionModelNode, InMemoryCompositionModelLink> Model { get; }

		public InMemoryCompositionModelNode Add(DiagramElement element)
		{
			var node = new InMemoryCompositionModelNode { Element = element };
			Model.AddNode(node);
			return node;
		}
	}

	private static string SocketId(StaticSocketIds id) => id.ToString();

	private sealed class UndoManagerStub : IUndoManager
	{
		public bool IsUndoingRedoing { get; set; }

		public bool CanUndo() => false;
		public bool CanRedo() => false;

		public void Undo()
		{
		}

		public void Redo()
		{
		}
	}

	/// <summary>
	/// A source of values of one type, so a chart panel has something to be wired to.
	/// </summary>
	private sealed class SourceElement : DiagramElement
	{
		public SourceElement(DiagramSocketType type)
			=> AddOutput(StaticSocketIds.Output, "Out", type);

		public override Guid TypeId { get; } = "6A3D6B0E-51C4-4F7B-8E0A-2C1D9F4B7A35".To<Guid>();

		public override string IconName { get; } = "Pi";
	}

	/// <summary>
	/// An axis that reports a change of its title the way the axis of a real chart does.
	/// </summary>
	private class NotifyingAxis : IChartAxis
	{
		private string _title;

		public event PropertyChangingEventHandler PropertyChanging;
		public event PropertyChangedEventHandler PropertyChanged;

		public IChartArea ChartArea => null;
		public string Id { get; set; }
		public bool IsVisible { get; set; }

		public string Title
		{
			get => _title;
			set
			{
				if (_title == value)
					return;

				PropertyChanging?.Invoke(this, new(nameof(Title)));
				_title = value;
				PropertyChanged?.Invoke(this, new(nameof(Title)));
			}
		}

		public string Group { get; set; }
		public bool SwitchAxisLocation { get; set; }
		public ChartAxisType AxisType { get; set; }
		public bool AutoRange { get; set; }
		public bool FlipCoordinates { get; set; }
		public bool DrawMajorTicks { get; set; }
		public bool DrawMajorGridLines { get; set; }
		public bool DrawMinorTicks { get; set; }
		public bool DrawMinorGridLines { get; set; }
		public bool DrawLabels { get; set; }
		public string TextFormatting { get; set; }
		public string CursorTextFormatting { get; set; }
		public string SubDayTextFormatting { get; set; }
		public TimeZoneInfo TimeZone { get; set; }

		public Task LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
			=> Task.CompletedTask;

		public Task SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
			=> Task.CompletedTask;

		void INotifyPropertyChangedEx.NotifyPropertyChanged(string propertyName)
			=> PropertyChanged?.Invoke(this, new(propertyName));
	}

	/// <summary>
	/// An axis that can also copy its settings in memory.
	/// </summary>
	private sealed class SnapshotAxis : NotifyingAxis, IChartSnapshotPart
	{
		object IChartSnapshotPart.CaptureSettings() => Title;

		void IChartSnapshotPart.RestoreSettings(object settings) => Title = (string)settings;
	}

	/// <summary>
	/// A composition with an undo manager that keeps the operations it is handed.
	/// </summary>
	private sealed class UndoGraph
	{
		private readonly UndoManagerStub _undoManager = new();

		public UndoGraph()
		{
			var behavior = new InMemoryCompositionModelBehavior { UndoManager = _undoManager };

			behavior.BehaviorChanged += change =>
			{
				if (change.oldValue is IUndoableEdit edit)
					Edits.Add(edit);
			};

			Model = new(behavior);
			Composition = new(Model);
		}

		public CompositionModel<InMemoryCompositionModelNode, InMemoryCompositionModelLink> Model { get; }
		public CompositionDiagramElement Composition { get; }
		public List<IUndoableEdit> Edits { get; } = [];

		public InMemoryCompositionModelNode Add(DiagramElement element)
		{
			var node = new InMemoryCompositionModelNode { Element = element };
			Model.AddNode(node);
			return node;
		}

		public void Undo()
		{
			_undoManager.IsUndoingRedoing = true;

			try
			{
				foreach (var edit in Edits.AsEnumerable().Reverse())
					edit.Undo();
			}
			finally
			{
				_undoManager.IsUndoingRedoing = false;
			}
		}

		public void Redo()
		{
			_undoManager.IsUndoingRedoing = true;

			try
			{
				foreach (var edit in Edits)
					edit.Redo();
			}
			finally
			{
				_undoManager.IsUndoingRedoing = false;
			}
		}
	}

	/// <summary>
	/// A link names the socket it lands on, and an element may give that socket up and take it back - a
	/// limit order has a price input, a market order has none. When the socket returns the link must be
	/// honoured: the model has to expose its far end again, and the host has to be told which node to read
	/// again, or the diagram is drawn with a link nobody can follow.
	/// </summary>
	[TestMethod]
	public void SocketAdded_InvalidatesTheRelationshipsTheModelExposes()
	{
		var graph = new Graph();

		var source = new OrderRegisterDiagramElement();
		var target = new OrderRegisterDiagramElement { IsMarket = true };

		var sourceNode = graph.Add(source);
		var targetNode = graph.Add(target);

		var orderId = SocketId(StaticSocketIds.Order);
		var priceId = SocketId(StaticSocketIds.Price);

		target.InputSockets.FindById(priceId).AssertNull("a market order element has no price input for a link to land on");

		graph.Model.AddLink(sourceNode, orderId, targetNode, priceId);

		InMemoryCompositionModelNode invalidated = null;

		graph.Behavior.BehaviorChanged += t =>
		{
			if (t.change == ModelChange.InvalidateRelationships)
				invalidated = (InMemoryCompositionModelNode)t.data;
		};

		target.IsMarket = false;

		var price = target.InputSockets.FindById(priceId);

		price.AssertNotNull("a limit order element must have its price input back");
		invalidated.AssertSame(targetNode, "the node that gained the socket is the one whose relationships have to be read again");
		graph.Behavior.Links.Count().AssertEqual(1, "a link whose socket came back leads somewhere again and must be kept");

		((ICompositionModel)graph.Model).GetConnectedSocketsFor(target, price).Single()
			.AssertSame(source.OutputSockets.FindById(orderId), "the model must expose the far end of the link once the socket it lands on exists");
	}

	/// <summary>
	/// The mirror promise. A socket an element gives up leaves the link that landed on it pointing nowhere,
	/// so the model must stop exposing that link - and must have stopped before the host is told the node
	/// changed, or the host reads the node back and is handed the relationship that has just died.
	/// </summary>
	[TestMethod]
	public void SocketRemoved_DropsTheLinkThatLandedOnIt_BeforeTheNodeIsAnnouncedAsChanged()
	{
		var graph = new Graph();

		// Both are limit order elements, so the target has a price input for the link to land on.
		var source = new OrderRegisterDiagramElement();
		var target = new OrderRegisterDiagramElement();

		var sourceNode = graph.Add(source);
		var targetNode = graph.Add(target);

		var orderId = SocketId(StaticSocketIds.Order);
		var priceId = SocketId(StaticSocketIds.Price);

		graph.Model.AddLink(sourceNode, orderId, targetNode, priceId);

		graph.Behavior.Links.Count().AssertEqual(1, "the link must be in the model before the socket goes");

		InMemoryCompositionModelNode invalidated = null;
		var linksWhenAnnounced = -1;

		graph.Behavior.BehaviorChanged += t =>
		{
			if (t.change != ModelChange.InvalidateRelationships)
				return;

			invalidated = (InMemoryCompositionModelNode)t.data;
			linksWhenAnnounced = graph.Behavior.GetLinksForNode(invalidated).Count();
		};

		target.IsMarket = true;

		target.InputSockets.FindById(priceId).AssertNull("a market order element has no price input");
		invalidated.AssertSame(targetNode, "the node that lost the socket is the one whose relationships have to be read again");
		graph.Behavior.Links.Count().AssertEqual(0, "a link that lands nowhere must stop being part of the graph");
		linksWhenAnnounced.AssertEqual(0, "the dead link must already be gone when the host is told to read the node again");
	}

	/// <summary>
	/// A host keeps a diagram it can save and redraw, and the model is the only place it learns the diagram
	/// moved on. An edit committed on an element is such a move: it must reach the host, and it must name
	/// the node and the operation so the right part can be redrawn or undone. Turning editing off changes
	/// nothing anyone could save, so it must not be announced as a change of the diagram.
	/// </summary>
	[TestMethod]
	public void CommittedElementEdit_IsAnnouncedAsAChangeOfTheDiagram_AndEditabilityIsNot()
	{
		var graph = new Graph();
		var node = graph.Add(new OrderRegisterDiagramElement());

		var changes = 0;
		graph.Model.ModelChanged += () => changes++;

		object edited = null;
		string operation = null;

		graph.Behavior.BehaviorChanged += t =>
		{
			if (t.change != ModelChange.Property)
				return;

			edited = t.data;
			operation = t.propName;
		};

		graph.Behavior.RaiseCommited("op", node, null);

		changes.AssertEqual(1, "an edit committed on an element is a change of the diagram and must reach the host");
		edited.AssertSame(node, "the announcement must name the node that was edited");
		operation.AssertEqual("op", "the announcement must name the operation that was committed");

		graph.Behavior.Modifiable = false;

		changes.AssertEqual(1, "turning editing off changes nothing that could be saved, so it is not a change of the diagram");
	}

	/// <summary>
	/// An element is built - sockets added, parameters given their defaults - before it is put into a
	/// composition, so before any undo manager is in sight. What it did then must not count against
	/// the edits made once there is one.
	/// </summary>
	[TestMethod]
	public void ParameterEdit_ReachesTheUndoManager_AndCanBeUndoneAndRedone()
	{
		var graph = new UndoGraph();
		var variable = new VariableDiagramElement();
		graph.Add(variable);
		graph.Edits.Clear();

		variable.InputAsTrigger.AssertFalse();
		variable.InputAsTrigger = true;

		IsTrue(graph.Edits.Count > 0, "an edit of a parameter must be handed to the undo manager");

		graph.Undo();
		variable.InputAsTrigger.AssertFalse("undo puts the parameter back");

		graph.Redo();
		variable.InputAsTrigger.AssertTrue("redo makes the edit again");
	}

	/// <summary>
	/// An edit made while the manager is undoing or redoing is the manager's own work and is not
	/// reported back to it as a new operation.
	/// </summary>
	[TestMethod]
	public void Undo_WhatItChangesItself_IsNotReportedAsANewEdit()
	{
		var graph = new UndoGraph();
		var variable = new VariableDiagramElement();
		graph.Add(variable);
		graph.Edits.Clear();

		variable.InputAsTrigger = true;
		var reported = graph.Edits.Count;

		graph.Undo();
		graph.Redo();

		graph.Edits.Count.AssertEqual(reported);

		variable.InputAsTrigger = false;

		IsTrue(graph.Edits.Count > reported, "an edit made after an undo and a redo is reported like any other");
	}

	/// <summary>
	/// A security is held by reference: the element, the provider and the connector all mean the same
	/// object. Undo must hand that object back, not a copy nobody else holds.
	/// </summary>
	[TestMethod]
	public void Undo_LeavesTheSecurityAnElementHolds_TheSameObject()
	{
		var security = new Security { Id = "AAPL@NASDAQ" };

		var graph = new UndoGraph();
		var variable = new VariableDiagramElement { Type = DiagramSocketType.Security, Value = security };
		graph.Add(variable);
		graph.Edits.Clear();

		variable.InputAsTrigger = true;
		IsTrue(graph.Edits.Count > 0, "an edit of a parameter must be handed to the undo manager");

		graph.Undo();

		variable.InputAsTrigger.AssertFalse();
		AreSame(security, variable.Value);

		graph.Redo();

		AreSame(security, variable.Value);
	}

	/// <summary>
	/// A chart panel keeps an indicator series inside a wrapper. Undo of an edit of the panel leaves
	/// the series on it once, in the wrapper it already had.
	/// </summary>
	[TestMethod]
	public void Undo_OfAChartPanelEdit_KeepsOneWrapperPerIndicator()
	{
		var graph = new UndoGraph();

		var source = new SourceElement(DiagramSocketType.IndicatorValue);
		var panel = new DummyChartDiagramElement();

		var sourceNode = graph.Add(source);
		var panelNode = graph.Add(panel);

		graph.Model.AddLink(sourceNode, StaticSocketIds.Output.ToString(), panelNode, panel.InputSockets.First().Id);

		panel.IndicatorElements.Count.AssertEqual(1, "wiring indicator values into a panel puts an indicator series on it");
		var wrapper = panel.IndicatorElements.First();

		graph.Edits.Clear();
		panel.ShowNonFormedIndicators = !panel.ShowNonFormedIndicators;
		IsTrue(graph.Edits.Count > 0, "an edit of a panel parameter must be handed to the undo manager");

		graph.Undo();

		panel.IndicatorElements.Count.AssertEqual(1, "undo must not put a second wrapper around the same series");
		AreSame(wrapper, panel.IndicatorElements.First());

		graph.Redo();

		panel.IndicatorElements.Count.AssertEqual(1);
		AreSame(wrapper, panel.IndicatorElements.First());
	}

	/// <summary>
	/// A series or an axis of a panel is edited in place, and the edit is one of the panel. Undo has to
	/// put back what the part was set to, and redo what it was changed to - for a part that can copy its
	/// settings in memory, since a snapshot is taken where nothing can wait for a part to be saved.
	/// </summary>
	[TestMethod]
	public void Undo_OfAnAxisEdit_PutsBackWhatTheAxisWasSetTo()
	{
		var graph = new UndoGraph();
		var panel = new DummyChartDiagramElement();
		graph.Add(panel);

		var axis = new SnapshotAxis { Id = "Y2", Title = "price" };
		panel.YAxes.Add(axis);

		graph.Edits.Clear();
		axis.Title = "volume";
		IsTrue(graph.Edits.Count > 0, "an edit of an axis must be handed to the undo manager as an edit of its panel");

		graph.Undo();

		axis.Title.AssertEqual("price", "undo must put back what the axis was set to");
		IsTrue(panel.YAxes.Contains(axis), "the axis itself stays on the panel");

		graph.Redo();

		axis.Title.AssertEqual("volume", "redo must put back what the axis was changed to");
	}

	/// <summary>
	/// The parts of a chart come from the chart package the application was built with, and one built
	/// before a part could copy its settings has parts that cannot. Such a part has to stay on its panel
	/// through undo and redo of its own edit.
	/// </summary>
	[TestMethod]
	public void Undo_OfAnAxisThatCannotCopyItsSettings_KeepsItOnThePanel()
	{
		var graph = new UndoGraph();
		var panel = new DummyChartDiagramElement();
		graph.Add(panel);

		var axis = new NotifyingAxis { Id = "Y2", Title = "price" };
		panel.YAxes.Add(axis);
		var axes = panel.YAxes.ToArray();

		graph.Edits.Clear();
		axis.Title = "volume";
		IsTrue(graph.Edits.Count > 0, "an edit of an axis must be handed to the undo manager as an edit of its panel");

		graph.Undo();

		panel.YAxes.ToArray().AssertEqual(axes, "undo must leave the panel with the axes it had");

		graph.Redo();

		panel.YAxes.ToArray().AssertEqual(axes, "redo must leave the panel with the axes it had");
	}
}

using ActiproSoftware.Windows;

namespace StockSharp.Configuration.ConfigManager.Layout.Windows.ViewModels
{

	/// <summary>
	/// Represents a base class for all view-models.
	/// </summary>
	public abstract class ViewModelBase : ObservableObjectBase {

		private string name;

		/// <summary>
		/// Gets or sets the name of the view-model.
		/// </summary>
		/// <value>The name of the view-model.</value>
		public string Name {
			get {
				return this.name;
			}
			set {
			    if (this.name == value) return;
			    this.name = value;
			    this.NotifyPropertyChanged(nameof(Name));
			}
		}

	}
}

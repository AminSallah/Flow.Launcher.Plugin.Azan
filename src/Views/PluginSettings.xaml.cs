using Modern = ModernWpf.Controls;
using System.Windows.Controls;
using Flow.Launcher.Plugin.Azan.ViewModels;
using System.Windows;
namespace Flow.Launcher.Plugin.Azan.Views
{
	
	public partial class PluginSettings : UserControl
	{
        PluginInitContext Context;
        public Settings Settings;
        SettingsViewModel _viewModel;

        public PluginSettings(PluginInitContext context, Settings settings, SettingsViewModel viewModel)
		{
            this.InitializeComponent();
            Context = context;
            _viewModel = viewModel;
            this.Settings = settings;
            DataContext = viewModel;
        }

    }


}

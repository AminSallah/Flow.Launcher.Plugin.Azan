using Modern = ModernWpf.Controls;
using System.Windows.Controls;
using Flow.Launcher.Plugin.Azan.ViewModels;
using System.Windows;
using ModernWpf.Controls;
namespace Flow.Launcher.Plugin.Azan.Views
{
	
	public partial class PluginSettings : UserControl
	{
        PluginInitContext _context;
        public Settings Settings;
        SettingsViewModel _viewModel;

        public PluginSettings(PluginInitContext context, Settings settings, SettingsViewModel viewModel)
		{
            this.InitializeComponent();
            _context = context;
            _viewModel = viewModel;
            this.Settings = settings;
            DataContext = viewModel;
        }

        private void OnToggleSwitchToggled(object sender, RoutedEventArgs e)
        {
            var toggleSwitch = sender as ToggleSwitch;
            bool isOn = toggleSwitch.IsOn;
            if (isOn)
            {
                _context.API.AddActionKeyword(_context.CurrentPluginMetadata.ID,"*");
            }
            else
            {
                while (_context.CurrentPluginMetadata.ActionKeywords.Contains("*")) {
                    _context.API.RemoveActionKeyword(_context.CurrentPluginMetadata.ID,"*");
                }
            }
        }

    }


}

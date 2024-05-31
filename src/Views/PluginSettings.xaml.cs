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
            // foreach (ComboBoxItem item in SyncComboBox.Items)
            // {
            //     if (item.Content.ToString() == Settings.Sync)
            //     {
            //         SyncComboBox.SelectedItem = item;
            //         break;
            //     }
            // }

        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                Settings.Sync = SyncComboBox.SelectedItem as string;
                // MessageBox.Show($"Selected item changed to: {selectedValue}");
                // Call your method or logic here
            }
        }

    }


}


using System.Collections.Generic;

namespace Flow.Launcher.Plugin.Azan.ViewModels
{
    public class SettingsViewModel : BaseModel
    {

        public SettingsViewModel(Settings settings)
        {
            Settings = settings;
        }


        public List<string> SyncOptions {get;} = new List<string> {"Monthly", "Yearly"};

        public string SyncProperty 
        {
            get => Settings.Sync;
            set
            {
                Settings.Sync = value;
                OnPropertyChanged(nameof(Settings.Sync));
            }
        }



        public Settings Settings { get; set; }
    }
}
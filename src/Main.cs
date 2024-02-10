using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Flow.Launcher.Plugin.Azan.Views;
using Flow.Launcher.Plugin;
using Flow.Launcher.Plugin.Azan.ViewModels;
using System.IO;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Flow.Launcher.Plugin.Azan
{
    public class Azan : IPlugin, ISettingProvider
    {
        private PluginInitContext _context;
        private Settings _settings;
        internal static Prayers _prayers;
        private static SettingsViewModel? _viewModel;

        public void Init(PluginInitContext context)
        {
            _context = context;
            _settings = _context.API.LoadSettingJsonStorage<Settings>();
            //if(!string.IsNullOrEmpty(_settings.Latitude)&& !string.IsNullOrEmpty(_settings.Longitude))
            if (!File.Exists(Path.Combine(_context.CurrentPluginMetadata.PluginDirectory,"Timings.json")))
                File.WriteAllText(Path.Combine(_context.CurrentPluginMetadata.PluginDirectory, "Timings.json"),"{}");
            _prayers = new Prayers(context,_settings);
            Azan._viewModel = new SettingsViewModel(this._settings);


        }

        internal static void LocationUpdated()
        {
            _ = Task.Run(() =>
            {
                _prayers.GetTimingsFromJson();

            });
        }

        public List<Result> Query(Query query)
        {
            List<Result> resultList = new List<Result>();
            if (true)
            {
                foreach (var _pray in _prayers.PrayerTimes)
                {
                    if (_settings.Timings.Contains(_pray.Key))
                        {
                        var result = new Result
                        {
                            Title = $"{_pray.Key}: {_pray.Value[0]}",
                            SubTitle = _pray.Value[2],
                            RoundedIcon = true,
                            IcoPath = $"Icons/{_pray.Key}.png",
                            Score = Convert.ToInt32(_pray.Value[1])

                        };
                        resultList.Add(result);
                        if (string.IsNullOrEmpty(query.FirstSearch))
                        {
                            break;
                        }
                        else
                        {
                            
                        }
                    }
                }
                if (!string.IsNullOrEmpty(query.FirstSearch))
                {
                    var resultDate = new Result
                    {
                        Title = $"Hijri date",
                        SubTitle = _prayers.HijriDate,
                        RoundedIcon = true,
                        IcoPath = $"Icons.png",
                        Score = 10000

                    };
                    resultList.Add(resultDate);
                }

            }
            return resultList;
        }


        public Control CreateSettingPanel()
        {
            return new PluginSettings(_context, _settings, Azan._viewModel!);
        }
    }
}
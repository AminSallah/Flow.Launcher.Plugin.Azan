using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Flow.Launcher.Plugin.Azan.Views;
using Flow.Launcher.Plugin;
using Flow.Launcher.Plugin.Azan.ViewModels;
using System.IO;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Linq;
using System.Net.NetworkInformation;

namespace Flow.Launcher.Plugin.Azan
{
    public class Azan : IPlugin, ISettingProvider
    {
        private PluginInitContext _context;
        private Settings _settings;
        internal static Prayers _prayers;
        private static SettingsViewModel? _viewModel;
        private bool CurrentPray = true;

        public void Init(PluginInitContext context)
        {
            _context = context;
            _settings = _context.API.LoadSettingJsonStorage<Settings>();
            //if(!string.IsNullOrEmpty(_settings.Latitude)&& !string.IsNullOrEmpty(_settings.Longitude))
            if (!File.Exists(Path.Combine(_context.CurrentPluginMetadata.PluginDirectory, "Timings.json")))
                File.WriteAllText(Path.Combine(_context.CurrentPluginMetadata.PluginDirectory, "Timings.json"), "{}");
            _prayers = new Prayers(context, _settings);
            Azan._viewModel = new SettingsViewModel(this._settings);
            _context.API.RegisterGlobalKeyboardCallback(MyKeyboardCallback);



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
            if (!string.IsNullOrEmpty(query.Search))
            {
                CurrentPray = true;
            }

            if (string.IsNullOrEmpty(_settings.Latitude) || string.IsNullOrEmpty(_settings.Longitude))
            {
                var result = new Result
                {
                    Title = "Location Coordinates Missing",
                    SubTitle = "Please ensure you have provided both latitude and longitude values for the location."
                };
                resultList.Add(result);
                return resultList ;
            }
            else if (_prayers.TimingsResponse.Count() == 0)
            {
                if (IsInternetConnected())
                {
                    _prayers.GetTimingsFromJson();
                }
                else
                {
                var result = new Result
                {
                    Title = "Internet Connection Error",
                    SubTitle = "Please ensure your internet connection is stable before initiating the plugin."
                };
                resultList.Add(result);
                return resultList ;
                }

            }


            if (true)
            {
                int Score = 100000;
                foreach (var _pray in _prayers.PrayerTimes)
                {
                    Score -= 1000;
                    var result = new Result
                    {
                        Title = $"{_pray.Key}",
                        SubTitle = $"{_pray.Value[2]} | {_pray.Value[0]}",
                        RoundedIcon = true,
                        IcoPath = $"Icons/{_pray.Key}.png",
                        //Score = Convert.ToInt32(_pray.Value[1])
                        Score = Score

                    };

                    if (!CurrentPray && _pray.Key == _prayers.PrayerTimesSorted.Keys.First())
                    {
                        resultList.Add(result);
                        break;
                    }
                    else if (!CurrentPray)
                    {
                        continue;
                    }
                    else
                    {
                        resultList.Add(result);

                    }


                }
                if (_settings.HijriDate && CurrentPray)
                {
                    var resultDate = new Result
                    {
                        Title = $"Hijri date",
                        SubTitle = _prayers.HijriDate,
                        RoundedIcon = true,
                        IcoPath = $"Icons/date.png",
                        Score = 101000
                    };
                    resultList.Add(resultDate);
                }

            }
            return resultList;
        }
        bool MyKeyboardCallback(int keyCode, int additionalInfo, SpecialKeyState keyState)
        {
            if (additionalInfo == 32)
            {
                CurrentPray = true;

            }
            else
            {
                CurrentPray = false;
            }

            // ...

            // Return true if the event was handled, false otherwise
            return true;
        }

        static bool IsInternetConnected()
        {
            try
            {
                Ping ping = new Ping();
                PingReply reply = ping.Send("8.8.8.8", 500);

                return (reply != null && reply.Status == IPStatus.Success);
            }
            catch (PingException)
            {
                return false;
            }
        }

        public Control CreateSettingPanel()
        {
            return new PluginSettings(_context, _settings, Azan._viewModel!);
        }
    }
}
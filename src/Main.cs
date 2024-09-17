using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Controls;
using Flow.Launcher.Plugin.Azan.Views;
using Flow.Launcher.Plugin;
using Flow.Launcher.Plugin.Azan.ViewModels;
using System.IO;
using System.Threading.Tasks;
using System.Device.Location;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Globalization;
using System.ComponentModel;

namespace Flow.Launcher.Plugin.Azan
{
    public class Azan : IPlugin, ISettingProvider
    {
        private PluginInitContext _context;
        private Settings _settings;
        internal static Prayers _prayers;
        private static SettingsViewModel? _viewModel;
        private bool CurrentPray = true;
        public string QueryString = string.Empty;

        (string, string) GetMyLocationUsingGPS()
        {
            GeoCoordinateWatcher watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);

            watcher.TryStart(false, TimeSpan.FromMilliseconds(1000));
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < 5000) // Try for 10 seconds
            {
                if (watcher.Status == GeoPositionStatus.Ready)
                {
                    GeoCoordinate geoPosition = watcher.Position.Location;
                    string latitude = geoPosition.Latitude.ToString();
                    string longitude = geoPosition.Longitude.ToString();
                    return (latitude, longitude);
                }
                Task.Delay(100).Wait();
            }
            return ("", "");
        }
        public void Init(PluginInitContext context)
        {
            _context = context;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            if (!File.Exists(Path.Combine(_context.CurrentPluginMetadata.PluginDirectory, "Timings.json")))
                File.WriteAllText(Path.Combine(_context.CurrentPluginMetadata.PluginDirectory, "Timings.json"), "{}");
            _settings = _context.API.LoadSettingJsonStorage<Settings>();
            if (string.IsNullOrEmpty(_settings.Latitude) && string.IsNullOrEmpty(_settings.Longitude))
            {
                (_settings.Latitude, _settings.Longitude) = GetMyLocationUsingGPS();
            }
            _prayers = new Prayers(context, _settings);
            Azan._viewModel = new SettingsViewModel(this._settings);
            _context.API.RegisterGlobalKeyboardCallback(KeyboardCallback);

        }

        

        internal static void LocationUpdated()
        {
            _ = Task.Run(() =>
            {
                _prayers.GetTimingsFromJson();

            });
        }

        void LogDebug()
        {
            string timingsfirstDate = string.Empty;
            if (_settings.Sync == "Monthly")
            {
                if (_prayers.TimingsResponse is Newtonsoft.Json.Linq.JObject timingsMonth)
                {   
                    if(timingsMonth.ContainsKey("1"))
                        timingsfirstDate = null;
                    else
                        timingsfirstDate = _prayers.TimingsResponse.First()["date"]["gregorian"]["date"].ToString();
                }
                else
                {
                    timingsfirstDate = "Unable to case";
                }
            }
            else
            {
                if (_prayers.TimingsResponse is Newtonsoft.Json.Linq.JObject timingsYear)
                {
                    if(timingsYear.ContainsKey("1"))
                        timingsfirstDate = _prayers.TimingsResponse["1"].First()["date"]["gregorian"]["date"].ToString();
                    else
                        timingsfirstDate = null;
                }
                else
                {
                    timingsfirstDate = "Unable to case";
                }
            }
            var logData = new Dictionary<string, object>
            {
                { "log_date", DateTime.Now },
                { "internet", IsInternetConnected() },
                { "url", _prayers.ParseUrlTimingsForMonth() },
                { "sync_automatically", _settings.SyncAutomatically },
                { "sync_type", _settings.Sync },
                { "curr_year", DateTime.Now.Year },
                { "curr_month", DateTime.Now.Month },
                { "timings_count", _prayers.TimingsResponse.Count() },
                { "timings_date_first",  timingsfirstDate}
            };

            string logText = System.Text.Json.JsonSerializer.Serialize(logData, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_context.CurrentPluginMetadata.PluginDirectory + "\\Azan.Log.json", logText);
            _context.API.ShowMsg("Azan.Main Error", "Azan failed to retreive prayer times. Please, see log file in plugin directory.");

        }

        public List<Result> Query(Query query)
        {
            List<Result> resultList = new List<Result>();
            QueryString = query.RawQuery;
            if (!string.IsNullOrEmpty(query.Search))
            {
                CurrentPray = true;
            }

            if (string.IsNullOrEmpty(_settings.Latitude) || string.IsNullOrEmpty(_settings.Longitude))
            {
                (_settings.Latitude, _settings.Longitude) = GetMyLocationUsingGPS();
                if (string.IsNullOrEmpty(_settings.Latitude) || string.IsNullOrEmpty(_settings.Longitude))
                {
                    var _settingsLoction = new Result
                    {
                        Title = "Location Services Disabled",
                        SubTitle = "Click to open windows Location Settings.",
                        Glyph = new GlyphInfo(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\ue819"),
                        Action = c =>
                            {
                                _context.API.ShellRun("start ms-settings:privacy-location");
                                return true;
                            },
                    };
                    resultList.Add(_settingsLoction);
                    var _settingsFLow = new Result
                    {
                        Title = "Location Services Disabled",
                        SubTitle = "Click to provide them manually in settings.",
                        Glyph = new GlyphInfo(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\ue819"),
                        Action = c =>
                            {
                                _context.API.OpenSettingDialog();
                                return true;
                            },
                    };
                    resultList.Add(_settingsFLow);
                    return resultList;
                }
            }
            if (_prayers.TimingsResponse.Count() == 0 && !(string.IsNullOrEmpty(_settings.Latitude) || string.IsNullOrEmpty(_settings.Longitude)))
            {
                if (IsInternetConnected())
                {
                    _prayers.GetTimingsFromJson(true);
                }
                else
                {
                    var result = new Result
                    {
                        Title = "Internet Connection Error",
                        SubTitle = "Please ensure your internet connection is stable before initiating the plugin.",
                        Glyph = new GlyphInfo(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\ue774"),
                    };
                    resultList.Add(result);
                    return resultList;
                }


            }
            else
            {
                int Score = 100000;

                

                if (_prayers.PrayerTimes.Count() == 0)
                {
                    if (IsInternetConnected())
                        _prayers.GetTimingsFromJson(true);

                    if (_prayers.PrayerTimes.Count() == 0)
                    {
                        LogDebug();
                    }
                }

                
                if (_settings.Globally && query.RawQuery.StartsWith(_context.CurrentPluginMetadata.ActionKeyword) || !_settings.Globally) {
                   

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
                            result.SubTitle = IdentifierCounDown(result.SubTitle);

                            if (_settings.Refresh)
                            {
                                Task.Run(() =>
                                {
                                    Thread.Sleep(500);
                                    _context.API.ReQuery();
                                    Thread.Sleep(500);
                                });
                            }
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
                else if (_settings.Globally) {
                    var _pray = _prayers.PrayerTimesSorted.FirstOrDefault();
                    resultList.Add(new Result
                    {
                        Title = $"{_pray.Key}",
                        SubTitle = $"{_pray.Value[2]} | {_pray.Value[0]}",
                        RoundedIcon = true,
                        IcoPath = $"Icons/{_pray.Key}.png",
                    });
                    return resultList;
                }

            }
            return resultList;
        }

        string IdentifierCounDown(string TimeDifference)
        {
            if (TimeDifference.Contains("+"))
            {
                return TimeDifference;
            }
            else
            {
                TimeDifference = "-" + TimeDifference;
            }
            return TimeDifference;
        }
        bool KeyboardCallback(int keyCode, int additionalInfo, SpecialKeyState keyState)
        {
            if (additionalInfo == 32)
            {
                CurrentPray = true;
            }
            else
            {
                CurrentPray = false;
            }
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
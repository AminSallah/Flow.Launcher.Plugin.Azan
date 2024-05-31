using System.Collections.Generic;
using System.Linq;
using System;
using Flow.Launcher.Plugin;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;

namespace Flow.Launcher.Plugin.Azan
{
    class Prayers
    {


        public JToken TimingsResponse;

        private PluginInitContext _context;

        private Settings _settings;
        public Dictionary<string, List<string>> PrayerTimesSorted
        {
            get
            {
                return GetPrayTime(true);
            }
        }
        public Dictionary<string, List<string>> PrayerTimes
        {
            get
            {
                return GetPrayTime(false);
            }
        }

        public string HijriDate { get; set; } = string.Empty;

        internal Prayers(PluginInitContext context, Settings settings)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            this._context = context;
            this._settings = settings;
            GetTimingsFromJson();

        }

        Dictionary<string, List<string>> GetPrayTime(bool sort = true)
        {
            Dictionary<string, List<string>> _prayerTimes = new Dictionary<string, List<string>>();
            if (TimingsResponse != null)
            {
                JToken timingsResponseMonth;
                if (_settings.Sync != "Monthly")
                {
                    try
                    {
                        timingsResponseMonth = TimingsResponse[DateTime.Now.Month.ToString(new CultureInfo("en-US"))];
                    }
                    catch
                    {
                        timingsResponseMonth = JToken.FromObject(new object());
                    }
                }
                else
                {
                    try
                    {
                        timingsResponseMonth = TimingsResponse;
                        var isValid = timingsResponseMonth.First()["date"]["gregorian"]["date"];
                    }
                    catch
                    {
                        timingsResponseMonth = JToken.FromObject(new object());
                    }
                    
                }
                foreach (var day in timingsResponseMonth)
                {
                    if (day["date"]["gregorian"]["date"].ToString() == DateTime.Now.ToString("dd-MM-yyyy", new CultureInfo("en-US")))
                    {
                        foreach (var _pray in day["timings"].ToObject<Dictionary<string, string>>())
                        {
                            if (_settings.Timings.Contains(_pray.Key))
                            {
                                string Name;
                                if (day["date"]["gregorian"]["weekday"]["en"].ToString() == "Friday" && _pray.Key == "Dhuhr")
                                    Name = "Al Juma'a";
                                else
                                    Name = _pray.Key;

                                if (!_prayerTimes.ContainsKey(Name))
                                {
                                    _prayerTimes[Name] = new List<string>();
                                }
                                if (!_settings.Timeformat24)
                                    _prayerTimes[Name].Add(DateTime.ParseExact(_pray.Value.Split("(")[0].Trim(), "HH:mm", null).ToString("hh:mm tt", new CultureInfo("en-US")));
                                else
                                    _prayerTimes[Name].Add(_pray.Value.Split("(")[0].Trim());
                                string prayTimeString = _pray.Value.Split("(")[0].Trim();
                                var dateTime = DateTime.Parse(DateTime.Now.ToShortDateString() + " " + prayTimeString);
                                TimeSpan TimeDifference = dateTime - DateTime.Now;
                                int Score;
                                try
                                {
                                    Score = 1000000 / (int)TimeDifference.TotalMinutes;
                                }
                                catch (DivideByZeroException)
                                {
                                    Score = 10000000;
                                }
                                _prayerTimes[Name].Add(Score.ToString());
                                _prayerTimes[Name].Add(TimeDifference.ToString().Split(".")[0].Replace("-", "+ "));

                                if (Score < 0)
                                {
                                    if (Math.Abs(Score) > (1000000 / Convert.ToInt32(_settings.Duration)) && Name != "Imsak")
                                    {
                                        _prayerTimes[Name][1] = "10000000";
                                    }
                                    else
                                    {
                                        string Count = (dateTime - DateTime.Now.AddDays(-1)).ToString().Split(".")[0];
                                        _prayerTimes[Name][2] = Count;
                                    }
                                }
                            }
                        }
                        HijriDate = $"{day["date"]["hijri"]["day"]} {day["date"]["hijri"]["month"]["en"]} {day["date"]["hijri"]["year"]}";
                        if (!sort)
                            return _prayerTimes;
                    }
                }
                var sortedPrayerTimes = _prayerTimes
                                        .OrderByDescending(prayer =>
                                        {
                                            int value = int.Parse(prayer.Value[1]);
                                            return value;
                                        });
                _prayerTimes = sortedPrayerTimes.ToDictionary(prayer => prayer.Key, prayer => prayer.Value);
            }
            return _prayerTimes;
        }

        public void GetTimingsFromJson(bool force = false)
        {
            if (_settings.SyncAutomatically || force)
            {
                JObject ResponseDates = FireAPIRequest();

                if (ResponseDates.ContainsKey("data"))
                {
                    TimingsResponse = ResponseDates["data"];
                    File.WriteAllText(Path.Combine(_context.CurrentPluginMetadata.PluginDirectory, "Timings.json"), JsonConvert.SerializeObject(TimingsResponse));
                }
                else
                {
                    string jsonString = File.ReadAllText(Path.Combine(_context.CurrentPluginMetadata.PluginDirectory, "Timings.json"));
                    TimingsResponse = JsonConvert.DeserializeObject<JToken>(jsonString);
                }
            }
            else
            {
                string jsonString = File.ReadAllText(Path.Combine(_context.CurrentPluginMetadata.PluginDirectory, "Timings.json"));
                if (string.IsNullOrEmpty(jsonString))
                {
                    JObject ResponseDates = FireAPIRequest();
                    if (ResponseDates.ContainsKey("data") && ResponseDates["data"].Count() != 0)
                    {
                        TimingsResponse = ResponseDates["data"];
                        File.WriteAllText(Path.Combine(_context.CurrentPluginMetadata.PluginDirectory, "Timings.json"), JsonConvert.SerializeObject(TimingsResponse));
                        return;
                    }
                }
                TimingsResponse = JsonConvert.DeserializeObject<JToken>(jsonString);
            }

        }

        public string ParseUrlTimingsForMonth()
        {
            string apiUrl = "http://api.aladhan.com/v1/calendar";
            string requestUrl = string.Empty;
            if (_settings.Sync == "Monthly")
            {
                requestUrl = $"{apiUrl}/{DateTime.Now.Year}/{DateTime.Now.Month}?latitude={_settings.Latitude}&longitude={_settings.Longitude}";
            }
            else
            {
                requestUrl = $"{apiUrl}/{DateTime.Now.Year}?latitude={_settings.Latitude}&longitude={_settings.Longitude}";
            }
            if (!string.IsNullOrEmpty(_settings.Method))
                requestUrl = requestUrl + $"&method={_settings.Method}";
            if (!string.IsNullOrEmpty(_settings.Tune))
                requestUrl = requestUrl + $"&tune={_settings.Tune}";
            if (!string.IsNullOrEmpty(_settings.Adjustment))
                requestUrl = requestUrl + $"&adjustment={_settings.Adjustment}";
            return requestUrl;
        }

        JObject FireAPIRequest()
        {
            string requestUrl = ParseUrlTimingsForMonth();
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = client.GetAsync(requestUrl).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = response.Content.ReadAsStringAsync().Result;
                        JObject ResponseDates = JObject.Parse(responseBody);
                        return ResponseDates;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex.Message}");
                }
            }
            return new JObject();
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections.Generic;
using Flow.Launcher.Plugin;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;

namespace Flow.Launcher.Plugin.Azan
{
    class Prayers
    {
   

        private JToken TimingsResponse;

        private PluginInitContext _context;

        private Settings _settings;
        public Dictionary<string, List<string>> PrayerTimes
        {
            get
            {
                return GetPrayTime();
            }
        }

        public string HijriDate { get; set; } = string.Empty;

        internal Prayers(PluginInitContext context, Settings settings)
        {
            this._context = context;
            this._settings = settings;
            GetTimingsFromJson();

        }

        Dictionary<string, List<string>> GetPrayTime()
        {
            Dictionary<string, List<string>> _prayerTimes = new Dictionary<string, List<string>>();
            if (TimingsResponse != null)
            { 
                foreach (var day in TimingsResponse)
                {
                    if (day["date"]["gregorian"]["date"].ToString() == DateTime.Now.ToString("dd-MM-yyyy"))
                    {
                        foreach (var _pray in day["timings"].ToObject<Dictionary<string, string>>())
                        {
                            if (!_prayerTimes.ContainsKey(_pray.Key))
                            {
                                _prayerTimes[_pray.Key] = new List<string>();
                            }
                            if (!_settings.Timeformat24)
                                _prayerTimes[_pray.Key].Add(DateTime.ParseExact(_pray.Value.Split("(")[0].Trim(), "HH:mm", null).ToString("hh:mm tt"));
                            else
                                _prayerTimes[_pray.Key].Add(_pray.Value.Split("(")[0].Trim());
                            string prayTimeString = _pray.Value.Split("(")[0].Trim();
                            TimeSpan TimeDifference = DateTime.Parse(DateTime.Now.ToShortDateString() + " " + prayTimeString) - DateTime.Now;
                            int Score = 10000 / (int)TimeDifference.TotalMinutes;
                            _prayerTimes[_pray.Key].Add(Score.ToString());
                            _prayerTimes[_pray.Key].Add(TimeDifference.ToString().Split(".")[0]);
                        }
                        HijriDate = $"{day["date"]["hijri"]["day"]} {day["date"]["hijri"]["month"]["en"]} {day["date"]["hijri"]["year"]}";
                        break;
                    }
                }
                var sortedPrayerTimes = _prayerTimes.OrderByDescending(prayer => int.Parse(prayer.Value[1]));
                _prayerTimes = sortedPrayerTimes.ToDictionary(prayer => prayer.Key, prayer => prayer.Value);
            }
            return _prayerTimes;
        }

        public void GetTimingsFromJson()
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

        string ParseUrlTimingsForMonth()
        {
            string apiUrl = "http://api.aladhan.com/v1/calendar";
            string requestUrl = $"{apiUrl}/{DateTime.Now.Year}/{DateTime.Now.Month}?latitude={_settings.Latitude}&longitude={_settings.Longitude}";
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
                    else
                    {
                        Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                        _context.API.ShowMsgError(response.StatusCode.ToString(), requestUrl);

                    }
                }
                catch (Exception ex)
                {
                    _context.API.ShowMsgError("error", ex.Message);
                    Console.WriteLine($"Exception: {ex.Message}");
                }
            }
            return new JObject();
        }
    }
}
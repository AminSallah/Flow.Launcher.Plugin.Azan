using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Flow.Launcher.Plugin.Azan
{
    public class Settings
    {
        public Settings()
        {
            UpdateTimings();
        }
        public bool Timeformat24 {get; set;} = false;
        public bool Refresh {get; set;} = true;

        
        private string _duration = "30";
        public string Duration
        {
            get
            {
                return _duration;
            }
            set
            {
                if (Convert.ToInt32(value) > 0)
                {
                    _duration = value;
                }
                else
                {
                    _duration = "1";
                }
            }
        }

        public bool HijriDate { get; set;} = true;

        private string _latitude = string.Empty;
        private string _longitude = string.Empty;


        public string Latitude { 
            get
            {
                return _latitude;
            } 
            set
            {
                _latitude = value;
                if (Azan._prayers != null && !string.IsNullOrEmpty(_longitude))
                {
                    Azan.LocationUpdated();
                }
            }

        }
        public string Longitude
        {
            get
            {
                return _longitude;
            }
            set
            {
                _longitude = value;
                if (Azan._prayers != null && !string.IsNullOrEmpty(_latitude))
                {
                    Azan.LocationUpdated();
                }
            }

        }

        private string _sync = "Yearly";
        public string Sync 
        {
            get
            {
                return _sync;
            }
            set
            {
                _sync = value;
                Azan.LocationUpdated();
            }

        }

        private string _tune;
        public string Tune
        {
            get
            {
                return _tune;
            }
            set
            {
                _tune = value;
                if (Azan._prayers != null)
                {
                    Azan.LocationUpdated();
                }
            }
        }
        public string Method {get;set;}  = string.Empty;

        private string _adjustment;
        public string Adjustment
        {
            get
            {
                return _adjustment;
            }
            set
            {
                _adjustment = value;
                if (Azan._prayers != null )
                {
                    Azan.LocationUpdated();
                }
            }
        }
        private bool _imsak = false;
        private bool _fajr = true;
        private bool _sunrise = false;
        private bool _dhuhr = true;
        private bool _asr = true;
        private bool _maghrib = true;
        private bool _sunset = false;
        private bool _isha = true;
        private bool _firstthird = false;
        private bool _lastthird = false;

        public bool Imsak
        {
            get => _imsak;
            set
            {
                _imsak = value;
                UpdateTimings();
            }
        }

        public bool Fajr
        {
            get => _fajr;
            set
            {
                _fajr = value;
                UpdateTimings();
            }
        }

        public bool Sunrise
        {
            get => _sunrise;
            set
            {
                _sunrise = value;
                UpdateTimings();
            }
        }

        public bool Dhuhr
        {
            get => _dhuhr;
            set
            {
                _dhuhr = value;
                UpdateTimings();
            }
        }

        public bool Asr
        {
            get => _asr;
            set
            {
                _asr = value;
                UpdateTimings();
            }
        }

        public bool Maghrib
        {
            get => _maghrib;
            set
            {
                _maghrib = value;
                UpdateTimings();
            }
        }

        public bool Sunset
        {
            get => _sunset;
            set
            {
                _sunset = value;
                UpdateTimings();
            }
        }

        public bool Isha
        {
            get => _isha;
            set
            {
                _isha = value;
                UpdateTimings();
            }
        }
        public bool Firstthird
        {
            get => _firstthird;
            set
            {
                _firstthird = value;
                UpdateTimings();
            }
        }
        public bool Lastthird
        {
            get => _lastthird;
            set
            {
                _lastthird = value;
                UpdateTimings();
            }
        }

        private List<string> _timings = new List<string>();

        [JsonIgnore]
        public List<string> Timings
        {
            get
            {
                return _timings;
            }
            private set
            {
                _timings = value;
            }
        }


        private void UpdateTimings()
        {
            _timings.Clear(); // Clear the existing list

            var properties = this.GetType().GetProperties();
            foreach (var property in properties)
            {
                if (property.PropertyType == typeof(bool) && (bool)property.GetValue(this))
                {
                    _timings.Add(property.Name);
                }
            }
        }
     
    }
}
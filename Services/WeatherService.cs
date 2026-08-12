using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace DynamicNotch.Services
{
    public class WeatherData
    {
        public string Temperature { get; set; } = "--°";
        public string Condition { get; set; } = "";
        public string Icon { get; set; } = "";
        public string City { get; set; } = "";
        public string FeelsLike { get; set; } = "";
        public string Humidity { get; set; } = "";
    }

    public class WeatherService
    {
        private static readonly HttpClient _http = new();
        private DispatcherTimer? _timer;
        public event EventHandler<WeatherData>? WeatherUpdated;

        public async Task StartAsync()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(15)
            };
            _timer.Tick += async (s, e) => await FetchAsync();
            _timer.Start();
            await FetchAsync();
        }

        public void Stop()
        {
            _timer?.Stop();
            _timer = null;
        }

        private async Task FetchAsync()
        {
            try
            {
                // 1. Get location from IP
                double lat = 0, lon = 0;
                string city = "";
                try
                {
                    var geoJson = await _http.GetStringAsync("http://ip-api.com/json/");
                    using var geoDoc = JsonDocument.Parse(geoJson);
                    lat = geoDoc.RootElement.GetProperty("lat").GetDouble();
                    lon = geoDoc.RootElement.GetProperty("lon").GetDouble();
                    city = geoDoc.RootElement.GetProperty("city").GetString() ?? "";
                }
                catch
                {
                    // Fallback to Bengaluru if geolocation fails
                    lat = 12.9716;
                    lon = 77.5946;
                    city = "Bengaluru";
                }

                // 2. Fetch weather in CELSIUS
                var weatherUrl = $"https://api.open-meteo.com/v1/forecast" +
                                 $"?latitude={lat}&longitude={lon}" +
                                 $"&current=temperature_2m,relative_humidity_2m,apparent_temperature,weather_code" +
                                 $"&temperature_unit=celsius" +
                                 $"&timezone=auto";

                var weatherJson = await _http.GetStringAsync(weatherUrl);
                using var doc = JsonDocument.Parse(weatherJson);
                var current = doc.RootElement.GetProperty("current");

                double temp = current.GetProperty("temperature_2m").GetDouble();
                double feels = current.GetProperty("apparent_temperature").GetDouble();
                int humidity = current.GetProperty("relative_humidity_2m").GetInt32();
                int code = current.GetProperty("weather_code").GetInt32();

                var (condition, icon) = InterpretCode(code);

                WeatherUpdated?.Invoke(this, new WeatherData
                {
                    Temperature = $"{Math.Round(temp)}°C",
                    Condition = condition,
                    Icon = icon,
                    City = city,
                    FeelsLike = $"Feels like {Math.Round(feels)}°C",
                    Humidity = $"{humidity}%"
                });
            }
            catch
            {
                WeatherUpdated?.Invoke(this, new WeatherData
                {
                    Temperature = "--°C",
                    Condition = "Unavailable",
                    Icon = "❓"
                });
            }
        }

        private (string condition, string icon) InterpretCode(int code)
        {
            return code switch
            {
                0            => ("Clear", "☀️"),
                1 or 2       => ("Partly cloudy", "⛅"),
                3            => ("Cloudy", "☁️"),
                45 or 48     => ("Foggy", "🌫️"),
                51 or 53 or 55 => ("Drizzle", "🌦️"),
                61 or 63 or 65 => ("Rainy", "🌧️"),
                71 or 73 or 75 => ("Snowy", "❄️"),
                80 or 81 or 82 => ("Showers", "🌦️"),
                95 or 96 or 99 => ("Thunderstorm", "⛈️"),
                _            => ("Clear", "🌤️")
            };
        }
    }
}
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace DynamicNotch.Services
{
    public class WeatherData
    {
        public string Temperature { get; set; } = "--";
        public string Condition { get; set; } = "Unknown";
        public string Icon { get; set; } = "🌤";
        public string City { get; set; } = "";
        public string FeelsLike { get; set; } = "--";
        public string Humidity { get; set; } = "--";
    }

    public class WeatherService
    {
        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        // Using Open-Meteo (free, no API key needed) + ip-api for location
        private const string GeoUrl = "http://ip-api.com/json/?fields=city,lat,lon,status";
        private const string WeatherBase = "https://api.open-meteo.com/v1/forecast";

        public event Action<WeatherData>? WeatherUpdated;

        private DispatcherTimer? _timer;
        private double _lat = 0;
        private double _lon = 0;
        private string _city = "";

        public async Task StartAsync()
        {
            try
            {
                await FetchLocationAsync();
                await FetchWeatherAsync();

                _timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMinutes(15)
                };
                _timer.Tick += async (s, e) => await FetchWeatherAsync();
                _timer.Start();
            }
            catch
            {
                WeatherUpdated?.Invoke(new WeatherData
                {
                    Temperature = "--",
                    Condition = "No connection",
                    Icon = "❓"
                });
            }
        }

        public void Stop()
        {
            _timer?.Stop();
        }

        private async Task FetchLocationAsync()
        {
            try
            {
                var json = await _http.GetStringAsync(GeoUrl);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("status", out var status) &&
                    status.GetString() == "success")
                {
                    _lat = root.GetProperty("lat").GetDouble();
                    _lon = root.GetProperty("lon").GetDouble();
                    _city = root.TryGetProperty("city", out var city)
                        ? city.GetString() ?? "" : "";
                }
            }
            catch
            {
                // Default to a neutral location if geo fails
                _lat = 40.7128;
                _lon = -74.0060;
                _city = "";
            }
        }

        private async Task FetchWeatherAsync()
        {
            try
            {
                if (_lat == 0 && _lon == 0)
                    await FetchLocationAsync();

                var url = $"{WeatherBase}?latitude={_lat}&longitude={_lon}" +
                          $"&current_weather=true" +
                          $"&hourly=relativehumidity_2m,apparent_temperature" +
                          $"&timezone=auto&forecast_days=1";

                var json = await _http.GetStringAsync(url);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var current = root.GetProperty("current_weather");
                var tempC = current.GetProperty("temperature").GetDouble();
                var weatherCode = current.GetProperty("weathercode").GetInt32();
                var isDay = current.TryGetProperty("is_day", out var dayProp)
                    ? dayProp.GetInt32() == 1 : true;

                // Get feels like from first hourly entry
                var feelsLikeC = tempC;
                var humidity = "--";
                if (root.TryGetProperty("hourly", out var hourly))
                {
                    if (hourly.TryGetProperty("apparent_temperature", out var at) &&
                        at.GetArrayLength() > 0)
                        feelsLikeC = at[0].GetDouble();

                    if (hourly.TryGetProperty("relativehumidity_2m", out var rh) &&
                        rh.GetArrayLength() > 0)
                        humidity = rh[0].GetInt32() + "%";
                }

                var tempF = (tempC * 9 / 5) + 32;
                var feelsF = (feelsLikeC * 9 / 5) + 32;

                var data = new WeatherData
                {
                    Temperature = $"{Math.Round(tempF)}°F",
                    FeelsLike = $"{Math.Round(feelsF)}°F",
                    Humidity = humidity,
                    Condition = GetConditionText(weatherCode),
                    Icon = GetWeatherIcon(weatherCode, isDay),
                    City = _city
                };

                WeatherUpdated?.Invoke(data);
            }
            catch
            {
                WeatherUpdated?.Invoke(new WeatherData
                {
                    Temperature = "--",
                    Condition = "Unavailable",
                    Icon = "❓"
                });
            }
        }

        private static string GetConditionText(int code) => code switch
        {
            0 => "Clear Sky",
            1 => "Mainly Clear",
            2 => "Partly Cloudy",
            3 => "Overcast",
            45 or 48 => "Foggy",
            51 or 53 or 55 => "Drizzle",
            61 or 63 or 65 => "Rainy",
            71 or 73 or 75 => "Snowy",
            77 => "Snow Grains",
            80 or 81 or 82 => "Showers",
            85 or 86 => "Snow Showers",
            95 => "Thunderstorm",
            96 or 99 => "Thunder + Hail",
            _ => "Unknown"
        };

        private static string GetWeatherIcon(int code, bool isDay) => code switch
        {
            0 => isDay ? "☀️" : "🌙",
            1 => isDay ? "🌤" : "🌤",
            2 => "⛅",
            3 => "☁️",
            45 or 48 => "🌫",
            51 or 53 or 55 => "🌦",
            61 or 63 or 65 => "🌧",
            71 or 73 or 75 => "❄️",
            77 => "🌨",
            80 or 81 or 82 => "🌧",
            85 or 86 => "❄️",
            95 => "⛈",
            96 or 99 => "⛈",
            _ => "🌤"
        };
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace DynamicNotch.Services
{
    public class EventUpdatedEventArgs : EventArgs
    {
        public string EventText { get; set; } = "No events today";
    }

    public class EventsService
    {
        private DispatcherTimer? _timer;
        private static readonly HttpClient _http = new();
        public event EventHandler<EventUpdatedEventArgs>? EventUpdated;

        // Cached festival dates fetched from API (Key: "yyyy-MM-dd", Value: event name)
        private Dictionary<string, string> _apiCachedEvents = new();
        private int _cachedYear = 0;

        // ══════════════════════════════════════════════════════════
        // FIXED-DATE INTERNATIONAL & INDIAN EVENTS
        // (These never change year to year)
        // ══════════════════════════════════════════════════════════
        private static readonly Dictionary<string, string> _fixedEvents = new()
        {
            // ── JANUARY ──
            { "01-01", "🎉 New Year's Day" },
            { "01-04", "🌍 World Braille Day" },
            { "01-06", "⭐ Epiphany" },
            { "01-09", "🇮🇳 Pravasi Bharatiya Divas" },
            { "01-12", "🇮🇳 National Youth Day" },
            { "01-15", "🇮🇳 Indian Army Day" },
            { "01-23", "🇮🇳 Netaji Subhas Chandra Bose Jayanti" },
            { "01-25", "🇮🇳 National Voters' Day" },
            { "01-26", "🇮🇳 Republic Day" },
            { "01-27", "🕯️ Holocaust Remembrance Day" },
            { "01-30", "🇮🇳 Martyrs' Day" },

            // ── FEBRUARY ──
            { "02-02", "🌍 World Wetlands Day" },
            { "02-04", "🌍 World Cancer Day" },
            { "02-11", "👩‍🔬 Intl. Day of Women in Science" },
            { "02-13", "📻 World Radio Day" },
            { "02-14", "❤️ Valentine's Day" },
            { "02-20", "⚖️ World Day of Social Justice" },
            { "02-21", "🗣️ Mother Language Day" },
            { "02-28", "🔬 National Science Day (India)" },

            // ── MARCH ──
            { "03-01", "🌍 Zero Discrimination Day" },
            { "03-03", "🐆 World Wildlife Day" },
            { "03-08", "👩 International Women's Day" },
            { "03-14", "🥧 Pi Day" },
            { "03-15", "🛒 World Consumer Rights Day" },
            { "03-17", "☘️ St. Patrick's Day" },
            { "03-20", "😊 Intl. Day of Happiness" },
            { "03-21", "📜 World Poetry Day" },
            { "03-22", "💧 World Water Day" },
            { "03-24", "🫁 World Tuberculosis Day" },
            { "03-27", "🎭 World Theatre Day" },

            // ── APRIL ──
            { "04-01", "😄 April Fools' Day" },
            { "04-02", "🧩 World Autism Awareness Day" },
            { "04-05", "🚢 National Maritime Day (India)" },
            { "04-07", "⚕️ World Health Day" },
            { "04-14", "🇮🇳 Ambedkar Jayanti" },
            { "04-18", "🏛️ World Heritage Day" },
            { "04-22", "🌍 Earth Day" },
            { "04-23", "📚 World Book Day" },
            { "04-25", "🦟 World Malaria Day" },
            { "04-26", "💡 World Intellectual Property Day" },
            { "04-29", "💃 International Dance Day" },

            // ── MAY ──
            { "05-01", "👷 Labour Day" },
            { "05-03", "📰 World Press Freedom Day" },
            { "05-04", "⭐ Star Wars Day" },
            { "05-08", "🌍 World Red Cross Day" },
            { "05-11", "💻 National Technology Day (India)" },
            { "05-12", "👩‍⚕️ International Nurses Day" },
            { "05-15", "👨‍👩‍👧 Intl. Day of Families" },
            { "05-17", "📡 World Telecommunication Day" },
            { "05-18", "🏛️ International Museum Day" },
            { "05-21", "🍵 Intl. Tea Day" },
            { "05-22", "🌿 Intl. Biological Diversity Day" },
            { "05-31", "🚭 World No Tobacco Day" },

            // ── JUNE ──
            { "06-01", "👨‍👩‍👧 Global Day of Parents" },
            { "06-05", "🌱 World Environment Day" },
            { "06-08", "🌊 World Oceans Day" },
            { "06-12", "🧒 World Day Against Child Labour" },
            { "06-14", "🩸 World Blood Donor Day" },
            { "06-19", "✊ Juneteenth" },
            { "06-20", "🌍 World Refugee Day" },
            { "06-21", "🧘 International Yoga Day" },
            { "06-26", "🚫 Intl. Day Against Drug Abuse" },
            { "06-30", "☄️ International Asteroid Day" },

            // ── JULY ──
            { "07-01", "🩺 National Doctor's Day (India)" },
            { "07-04", "🇺🇸 US Independence Day" },
            { "07-11", "🌍 World Population Day" },
            { "07-12", "📖 Malala Day" },
            { "07-14", "🇫🇷 Bastille Day" },
            { "07-17", "😀 World Emoji Day" },
            { "07-18", "✊ Nelson Mandela Day" },
            { "07-26", "🇮🇳 Kargil Vijay Diwas" },
            { "07-28", "🌿 World Nature Conservation Day" },
            { "07-29", "🐯 International Tiger Day" },
            { "07-30", "🤝 International Friendship Day" },

            // ── AUGUST ──
            { "08-06", "🕊️ Hiroshima Day" },
            { "08-07", "🧵 National Handloom Day (India)" },
            { "08-09", "🕊️ Nagasaki Day" },
            { "08-10", "🦁 World Lion Day" },
            { "08-12", "🐘 Intl. Youth Day / World Elephant Day" },
            { "08-15", "🇮🇳 Independence Day" },
            { "08-19", "📸 World Photography Day" },
            { "08-20", "🇮🇳 Sadbhavana Diwas" },
            { "08-26", "♀️ Women's Equality Day" },
            { "08-29", "🇮🇳 National Sports Day" },

            // ── SEPTEMBER ──
            { "09-02", "🥥 World Coconut Day" },
            { "09-05", "👨‍🏫 Teachers' Day (India)" },
            { "09-08", "📚 International Literacy Day" },
            { "09-10", "💚 World Suicide Prevention Day" },
            { "09-14", "🇮🇳 Hindi Diwas" },
            { "09-15", "🛠️ Engineers' Day (India)" },
            { "09-16", "🌫️ World Ozone Day" },
            { "09-21", "🕊️ International Day of Peace" },
            { "09-27", "✈️ World Tourism Day" },
            { "09-29", "❤️ World Heart Day" },

            // ── OCTOBER ──
            { "10-01", "👵 Intl. Day of Older Persons" },
            { "10-02", "🕊️ Gandhi Jayanti" },
            { "10-04", "🐾 World Animal Day" },
            { "10-05", "👨‍🏫 World Teachers' Day" },
            { "10-08", "✈️ Indian Air Force Day" },
            { "10-09", "📮 World Post Day" },
            { "10-10", "🧠 World Mental Health Day" },
            { "10-11", "👧 Intl. Day of the Girl Child" },
            { "10-15", "🧼 Global Handwashing Day" },
            { "10-16", "🍎 World Food Day" },
            { "10-24", "🇺🇳 United Nations Day" },
            { "10-29", "🧠 World Stroke Day" },
            { "10-31", "🎃 Halloween / 🇮🇳 Rashtriya Ekta Diwas" },

            // ── NOVEMBER ──
            { "11-01", "🌱 World Vegan Day" },
            { "11-05", "🌊 World Tsunami Awareness Day" },
            { "11-11", "🎖️ Veterans Day / Remembrance Day" },
            { "11-14", "🧒 Children's Day (India) / World Diabetes Day" },
            { "11-16", "🤝 Intl. Day for Tolerance" },
            { "11-17", "🎓 International Students' Day" },
            { "11-19", "🚹 International Men's Day" },
            { "11-20", "🧒 Universal Children's Day" },
            { "11-21", "📺 World Television Day" },
            { "11-25", "♀️ Day Against Violence to Women" },
            { "11-26", "🇮🇳 Constitution Day (India)" },

            // ── DECEMBER ──
            { "12-01", "🎗️ World AIDS Day" },
            { "12-03", "♿ Day of Persons with Disabilities" },
            { "12-04", "⚓ Indian Navy Day" },
            { "12-05", "🌱 World Soil Day" },
            { "12-07", "🛡️ Armed Forces Flag Day (India)" },
            { "12-10", "⚖️ Human Rights Day" },
            { "12-11", "⛰️ International Mountain Day" },
            { "12-14", "⚡ Energy Conservation Day" },
            { "12-16", "🇮🇳 Vijay Diwas" },
            { "12-22", "🔢 National Mathematics Day (India)" },
            { "12-23", "🌾 Kisan Diwas (Farmers' Day)" },
            { "12-24", "🎄 Christmas Eve" },
            { "12-25", "🎄 Christmas Day" },
            { "12-26", "🎁 Boxing Day" },
            { "12-31", "🎊 New Year's Eve" },
        };

        public void Start()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromHours(6)
            };
            _timer.Tick += async (s, e) => await UpdateAsync();
            _timer.Start();
            _ = UpdateAsync();
        }

        public void Stop()
        {
            _timer?.Stop();
            _timer = null;
        }

        // ══════════════════════════════════════════════════════════
        // Fetch year-specific festival dates from Nager.Date API
        // (India + US public holidays — real-time accurate every year)
        // ══════════════════════════════════════════════════════════
        private async Task FetchYearFestivalsAsync(int year)
        {
            if (_cachedYear == year && _apiCachedEvents.Count > 0) return;

            var newCache = new Dictionary<string, string>();

            // Fetch multiple countries for comprehensive coverage
            string[] countries = { "IN", "US", "GB" };

            foreach (var country in countries)
            {
                try
                {
                    var url = $"https://date.nager.at/api/v3/PublicHolidays/{year}/{country}";
                    var json = await _http.GetStringAsync(url);
                    using var doc = JsonDocument.Parse(json);

                    string flag = country switch
                    {
                        "IN" => "🇮🇳",
                        "US" => "🇺🇸",
                        "GB" => "🇬🇧",
                        _    => "🎊"
                    };

                    foreach (var holiday in doc.RootElement.EnumerateArray())
                    {
                        var date = holiday.GetProperty("date").GetString();
                        var name = holiday.GetProperty("localName").GetString()
                                ?? holiday.GetProperty("name").GetString();

                        if (!string.IsNullOrEmpty(date) && !string.IsNullOrEmpty(name))
                        {
                            // Merge: if same date already exists, append with slash
                            if (newCache.ContainsKey(date))
                            {
                                if (!newCache[date].Contains(name))
                                    newCache[date] += $" / {flag} {name}";
                            }
                            else
                            {
                                newCache[date] = $"{flag} {name}";
                            }
                        }
                    }
                }
                catch
                {
                    // Skip failed country, keep others
                }
            }

            // Also fetch year-specific Indian festival dates from Calendarific-like fallback
            // (Nager.Date already handles: Diwali, Holi, Eid, Christmas, etc. for IN)

            if (newCache.Count > 0)
            {
                _apiCachedEvents = newCache;
                _cachedYear = year;
            }
        }

        private async Task UpdateAsync()
        {
            try
            {
                var today = DateTime.Now;
                string todayIsoKey = today.ToString("yyyy-MM-dd");   // for API cache
                string todayFixedKey = today.ToString("MM-dd");       // for fixed events

                // Refresh year cache if year changed or empty
                await FetchYearFestivalsAsync(today.Year);

                string? eventText = null;

                // 1. Check API cache first (has real-time accurate festival dates)
                if (_apiCachedEvents.TryGetValue(todayIsoKey, out var apiEvent))
                {
                    eventText = apiEvent;
                }

                // 2. Check built-in fixed international events
                if (_fixedEvents.TryGetValue(todayFixedKey, out var fixedEvent))
                {
                    if (eventText == null)
                        eventText = fixedEvent;
                    else if (!eventText.Contains(fixedEvent))
                        eventText = $"{eventText} / {fixedEvent}";
                }

                // 3. No event found
                if (eventText == null)
                {
                    eventText = "📅 No events today";
                }

                EventUpdated?.Invoke(this, new EventUpdatedEventArgs { EventText = eventText });
            }
            catch
            {
                EventUpdated?.Invoke(this, new EventUpdatedEventArgs { EventText = "📅 No events today" });
            }
        }
    }
}
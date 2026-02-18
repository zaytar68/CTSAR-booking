// ====================================================================
// WeatherService.cs : Service de récupération météo via Open-Meteo
// ====================================================================
// API gratuite, sans clé, haute qualité – https://open-meteo.com
// Coordonnées Saint-Louis (68) : lat=47.5938, lon=7.5602
// Données quotidiennes + horaires (matin 10h / après-midi 15h).
// Cache de 30 minutes pour limiter les appels réseau.
// ====================================================================

using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;

namespace CTSAR.Booking.Services;

public class WeatherService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<WeatherService> _logger;

    private const string CacheKey = "weather_forecast_saint_louis";

    // Heure représentative du matin et de l'après-midi (0-23)
    private const int MorningHour   = 10;
    private const int AfternoonHour = 15;

    private const string ForecastUrl =
        "https://api.open-meteo.com/v1/forecast" +
        "?latitude=47.5938" +
        "&longitude=7.5602" +
        "&daily=temperature_2m_max,temperature_2m_min,precipitation_sum,weather_code" +
        "&hourly=temperature_2m,weather_code" +
        "&timezone=Europe%2FParis" +
        "&forecast_days=7";

    public WeatherService(HttpClient httpClient, IMemoryCache cache, ILogger<WeatherService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Retourne les prévisions météo pour les 7 prochains jours,
    /// avec détail matin (10h) et après-midi (15h).
    /// Résultat mis en cache pendant 30 minutes.
    /// </summary>
    public async Task<List<DayWeather>?> GetWeeklyForecastAsync()
    {
        if (_cache.TryGetValue(CacheKey, out List<DayWeather>? cached))
            return cached;

        try
        {
            var response = await _httpClient.GetFromJsonAsync<OpenMeteoResponse>(ForecastUrl);

            if (response?.Daily is null || response.Daily.Time.Count == 0)
                return null;

            var daily  = response.Daily;
            var hourly = response.Hourly;
            var days   = new List<DayWeather>();

            for (int dayIndex = 0; dayIndex < daily.Time.Count; dayIndex++)
            {
                // Index dans le tableau horaire : chaque jour = 24 valeurs
                int morningIndex   = dayIndex * 24 + MorningHour;
                int afternoonIndex = dayIndex * 24 + AfternoonHour;

                var morning = GetPeriodWeather(hourly, morningIndex);
                var afternoon = GetPeriodWeather(hourly, afternoonIndex);

                days.Add(new DayWeather(
                    Date:          DateOnly.Parse(daily.Time[dayIndex]),
                    MaxTemp:       dayIndex < daily.Temperature2mMax.Count  ? daily.Temperature2mMax[dayIndex]  : null,
                    MinTemp:       dayIndex < daily.Temperature2mMin.Count  ? daily.Temperature2mMin[dayIndex]  : null,
                    Precipitation: dayIndex < daily.PrecipitationSum.Count  ? daily.PrecipitationSum[dayIndex]  : null,
                    Code:          dayIndex < daily.WeatherCode.Count       ? daily.WeatherCode[dayIndex]       : null,
                    Morning:       morning,
                    Afternoon:     afternoon
                ));
            }

            _cache.Set(CacheKey, days, TimeSpan.FromMinutes(30));
            return days;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des données météo Open-Meteo.");
            return null;
        }
    }

    private static PeriodWeather? GetPeriodWeather(HourlyForecastData hourly, int index)
    {
        if (index < 0 || index >= hourly.Time.Count)
            return null;

        var temp = index < hourly.Temperature2m.Count ? hourly.Temperature2m[index] : null;
        var code = index < hourly.WeatherCode.Count   ? hourly.WeatherCode[index]   : null;

        return new PeriodWeather(temp, code);
    }
}

// ====================================================================
// WeatherModels.cs : Modèles pour la réponse API Open-Meteo
// ====================================================================

using System.Text.Json.Serialization;

namespace CTSAR.Booking.Services;

/// <summary>
/// Réponse brute de l'API Open-Meteo (données quotidiennes + horaires).
/// </summary>
public class OpenMeteoResponse
{
    [JsonPropertyName("daily")]
    public DailyForecastData Daily { get; set; } = new();

    [JsonPropertyName("hourly")]
    public HourlyForecastData Hourly { get; set; } = new();
}

/// <summary>
/// Données quotidiennes retournées par Open-Meteo.
/// </summary>
public class DailyForecastData
{
    [JsonPropertyName("time")]
    public List<string> Time { get; set; } = [];

    [JsonPropertyName("temperature_2m_max")]
    public List<double?> Temperature2mMax { get; set; } = [];

    [JsonPropertyName("temperature_2m_min")]
    public List<double?> Temperature2mMin { get; set; } = [];

    [JsonPropertyName("precipitation_sum")]
    public List<double?> PrecipitationSum { get; set; } = [];

    [JsonPropertyName("weather_code")]
    public List<int?> WeatherCode { get; set; } = [];
}

/// <summary>
/// Données horaires retournées par Open-Meteo.
/// </summary>
public class HourlyForecastData
{
    [JsonPropertyName("time")]
    public List<string> Time { get; set; } = [];

    [JsonPropertyName("temperature_2m")]
    public List<double?> Temperature2m { get; set; } = [];

    [JsonPropertyName("weather_code")]
    public List<int?> WeatherCode { get; set; } = [];
}

/// <summary>
/// Données météo pour une période de la journée (matin ou après-midi).
/// </summary>
public record PeriodWeather(double? Temp, int? Code);

/// <summary>
/// Données météo simplifiées pour un jour donné, avec détail matin/après-midi.
/// </summary>
public record DayWeather(
    DateOnly Date,
    double? MaxTemp,
    double? MinTemp,
    double? Precipitation,
    int? Code,
    PeriodWeather? Morning,
    PeriodWeather? Afternoon);

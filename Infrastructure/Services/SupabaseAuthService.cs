using Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Services;

public class SupabaseAuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly string _supabaseUrl;
    private readonly string _serviceRoleKey;

    // Zauważ: wstrzykujemy HttpClient, to najlepsza praktyka w .NET!
    public SupabaseAuthService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _supabaseUrl = config["Supabase:Url"] ?? throw new ArgumentNullException("Brak Supabase Url");
        _serviceRoleKey = config["Supabase:ServiceRoleKey"] ?? throw new ArgumentNullException("Brak Service Role Key");
    }

    public async Task<Guid> InviteUserAsync(string email, Guid studioId, long roleId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_supabaseUrl}/auth/v1/invite");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);
        request.Headers.Add("apikey", _serviceRoleKey);

        // MAGIA JEST TUTAJ: Dodajemy obiekt "data"
        request.Content = JsonContent.Create(new
        {
            email = email,
            data = new InviteMetadata
            {
                StudioId = studioId,
                RoleId = roleId
            }
        });

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Nie udało się wysłać zaproszenia. Supabase zwróciło: {error}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(responseJson);
        var userIdString = jsonDoc.RootElement.GetProperty("id").GetString();

        return Guid.Parse(userIdString!);
    }
}

// Stwórzmy małą klasę specjalnie do tego payloadu zamiast obiektu anonimowego:
public class InviteMetadata
{
    [JsonPropertyName("studio_id")]
    public Guid StudioId { get; set; }

    [JsonPropertyName("role_id")]
    public long RoleId { get; set; }
}
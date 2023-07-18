using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace OptiDataProfiles;

public class OptiDataProfileService : IOptiDataProfileService, IDisposable
{
    private readonly OptiDataProfileOptions _options;

    private readonly HttpClient _httpClient;

    public OptiDataProfileService(IOptions<OptiDataProfileOptions> options, HttpClient httpClient)
    {
        _options = options.Value;
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _options.ApiKey);
    }

    public Dictionary<string, object> Get(string id)
    {
        var result = _httpClient.GetStringAsync($"?{_options.KeyField}={id}").GetAwaiter().GetResult();
        var jsonObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

        return jsonObj ?? new Dictionary<string, object>();
    }

    public HttpResponseMessage? Update(Dictionary<string, object> values)
    {
        var json = JsonContent.Create(new
        {
            attributes = values
        });

        return _httpClient.PostAsync("/", json).Result;
    }

    public void Dispose()
    {
        if (_httpClient != null)
        {
            _httpClient.Dispose();
        }
    }
}
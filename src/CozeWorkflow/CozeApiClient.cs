using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

public class CozeApiClient<TRequest, TResponse>
{
    private readonly HttpClient _httpClient;

    public CozeApiClient(string baseUrl, string authToken)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        };
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
    }

    public async Task<TResponse> RunWorkflowAsync(TRequest payload)
    {
        var url = "/v1/workflow/run";
        return await PostAsync<TResponse>(url, payload);
    }

    public async Task<TResponse> RunWorkflowStreamingAsync(TRequest payload)
    {
        var url = "/v1/workflow/stream_run";
        return await PostAsync<TResponse>(url, payload);
    }

    public async Task<TResponse> ResumeWorkflowAsync(WorkflowResumeRequest payload)
    {
        var url = "/v1/workflow/stream_resume";
        return await PostAsync<WorkflowResumeRequest, TResponse>(url, payload);
    }

    private async Task<T> PostAsync<T>(string url, TRequest request)
            => await PostAsync<TRequest, T>(url, request);

    private async Task<T> PostAsync<TPayload, T>(string url, TPayload payload)
    {
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(url, content);

        if (response.IsSuccessStatusCode)
        {
            var jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(jsonResponse);
        }

        var error = await response.Content.ReadAsStringAsync();
        throw new Exception($"Error: {response.StatusCode}, Details: {error}");
    }


}

public class WorkflowResumeRequest
{
    [JsonPropertyName("event_id")]
    public string EventId { get; set; }

    [JsonPropertyName("workflow_id")]
    public string WorkflowId { get; set; }

    [JsonPropertyName("resume_data")]
    public string ResumeData { get; set; }

    [JsonPropertyName("interrupt_type")]
    public int InterruptType { get; set; }
}


public class RunWorkflowResponse<TWorkflowOutput>
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("cost")]
    public string Cost { get; set; }

    private string _data;

    [JsonPropertyName("data")]
    public string Data
    {
        get => _data;
        set
        {
            _data = value;
            ParsedData = JsonSerializer.Deserialize<TWorkflowOutput>(_data);
        }
    }

    [JsonIgnore]
    public TWorkflowOutput ParsedData { get; private set; }

    [JsonPropertyName("debug_url")]
    public string DebugUrl { get; set; }

    [JsonPropertyName("msg")]
    public string Msg { get; set; }

    [JsonPropertyName("token")]
    public int Token { get; set; }
}
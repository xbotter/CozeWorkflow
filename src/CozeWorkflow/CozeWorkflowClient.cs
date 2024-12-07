using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;

namespace CozeWorkflow
{
    /// <summary>
    /// Generic class to handle workflow operations.
    /// </summary>
    public class CozeWorkflow<TParameters, TResponse>
    {
        private readonly HttpClient _httpClient; // HTTP client for making requests
        private readonly string _workflowId; // Workflow ID
        private readonly string _appId; // Application ID

        /// <summary>
        /// Initializes a new instance of the <see cref="CozeWorkflow{TParameters, TResponse}"/> class.
        /// </summary>
        /// <param name="baseUrl">The base URL for the HTTP client.</param>
        /// <param name="authToken">The authorization token.</param>
        /// <param name="workflowId">The workflow ID.</param>
        /// <param name="appId">The application ID.</param>
        public CozeWorkflow(string baseUrl, string authToken, string workflowId, string appId)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl)
            };
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
            _workflowId = workflowId;
            _appId = appId;
        }

        /// <summary>
        /// Runs a workflow asynchronously.
        /// </summary>
        /// <param name="parameters">The parameters for the workflow.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the workflow response.</returns>
        public async Task<RunWorkflowResponse<TResponse>> RunWorkflowAsync(TParameters parameters)
        {
            var request = new WorkflowRequest<TParameters>
            {
                WorkflowId = _workflowId,
                AppId = _appId,
                Parameters = parameters
            };
            return await PostAsync<WorkflowRequest<TParameters>, RunWorkflowResponse<TResponse>>("/v1/workflow/run", request);
        }

        /// <summary>
        /// Runs a workflow with streaming asynchronously and processes events.
        /// </summary>
        /// <param name="parameters">The parameters for the workflow.</param>
        /// <returns>An asynchronous enumerable of workflow events.</returns>
        public async IAsyncEnumerable<WorkflowEvent> RunWorkflowStreamingAsync(TParameters parameters)
        {
            var request = new WorkflowRequest<TParameters>
            {
                WorkflowId = _workflowId,
                AppId = _appId,
                Parameters = parameters
            };

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/v1/workflow/stream_run")
            {
                Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var responseStream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(responseStream);

            var eventStringBuilder = new StringBuilder();

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(line))
                {
                    if (eventStringBuilder.Length > 0)
                    {
                        var eventString = eventStringBuilder.ToString();
                        var workflowEvent = WorkflowEvent.Parse(eventString);
                        yield return workflowEvent;
                        eventStringBuilder.Clear();
                    }
                    continue;
                }

                eventStringBuilder.AppendLine(line);
            }

            // Process any remaining event data
            if (eventStringBuilder.Length > 0)
            {
                var eventString = eventStringBuilder.ToString();
                var workflowEvent = WorkflowEvent.Parse(eventString);
                yield return workflowEvent;
            }
        }

        /// <summary>
        /// Resumes a workflow asynchronously and processes events.
        /// </summary>
        /// <param name="eventId">The event ID.</param>
        /// <param name="resumeData">The resume data.</param>
        /// <param name="interruptType">The interrupt type.</param>
        /// <returns>An asynchronous enumerable of workflow events.</returns>
        public async IAsyncEnumerable<WorkflowEvent> ResumeWorkflowAsync(string eventId, string resumeData, int interruptType)
        {
            var request = new WorkflowResumeRequest
            {
                EventId = eventId,
                WorkflowId = _workflowId,
                ResumeData = resumeData,
                InterruptType = interruptType
            };

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/v1/workflow/stream_resume")
            {
                Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var responseStream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(responseStream);

            var eventStringBuilder = new StringBuilder();

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(line))
                {
                    if (eventStringBuilder.Length > 0)
                    {
                        var eventString = eventStringBuilder.ToString();
                        var workflowEvent = WorkflowEvent.Parse(eventString);
                        yield return workflowEvent;
                        eventStringBuilder.Clear();
                    }
                    continue;
                }

                eventStringBuilder.AppendLine(line);
            }

            // Process any remaining event data
            if (eventStringBuilder.Length > 0)
            {
                var eventString = eventStringBuilder.ToString();
                var workflowEvent = WorkflowEvent.Parse(eventString);
                yield return workflowEvent;
            }
        }

        /// <summary>
        /// Posts a request and gets a response.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request.</typeparam>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="url">The URL to post the request to.</param>
        /// <param name="payload">The payload to post.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response.</returns>
        private async Task<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest payload)
        {
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TResponse>(jsonResponse);
            }

            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Error: {response.StatusCode}, Details: {error}");
        }
    }

    /// <summary>
    /// Class representing a workflow request.
    /// </summary>
    /// <typeparam name="TParameters">The type of the parameters.</typeparam>
    public class WorkflowRequest<TParameters>
    {
        [JsonPropertyName("workflow_id")]
        public string WorkflowId { get; set; }

        [JsonPropertyName("app_id")]
        public string AppId { get; set; }

        [JsonPropertyName("parameters")]
        public TParameters Parameters { get; set; }
    }

    /// <summary>
    /// Class representing a workflow resume request.
    /// </summary>
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

    /// <summary>
    /// Class representing the response from running a workflow.
    /// </summary>
    /// <typeparam name="TWorkflowOutput">The type of the workflow output.</typeparam>
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

    // Class to represent workflow events
    public class WorkflowEvent
    {
        public int Id { get; set; }
        public string Event { get; set; }
        public JsonElement Data { get; set; }

        public static WorkflowEvent Parse(string eventString)
        {
            var workflowEvent = new WorkflowEvent();
            var lines = eventString.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                if (line.StartsWith("id: "))
                {
                    if (int.TryParse(line.Substring(4).Trim(), out int id))
                    {
                        workflowEvent.Id = id;
                    }
                }
                else if (line.StartsWith("event: "))
                {
                    workflowEvent.Event = line.Substring(7).Trim();
                }
                else if (line.StartsWith("data: "))
                {
                    var dataContent = line.Substring(6).Trim();
                    workflowEvent.Data = JsonSerializer.Deserialize<JsonElement>(dataContent);
                }
            }

            return workflowEvent;
        }
    }
}

using System;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace CozeWorkflow
{
    public  class CozeWorkflow<TParameters, TResponse>
    {
        private readonly CozeApiClient<WorkflowRequest<TParameters>, RunWorkflowResponse<TResponse>> _client;
        private readonly string _workflowId;
        private readonly string _appId;

        public CozeWorkflow(string baseUrl, string authToken, string workflowId, string appId)
        {
            _client = new CozeApiClient<WorkflowRequest<TParameters>, RunWorkflowResponse<TResponse>>(baseUrl, authToken);
            _workflowId = workflowId;
            _appId = appId;
        }

        public async Task<RunWorkflowResponse<TResponse>> RunWorkflowAsync(TParameters parameters)
        {
            var request = new WorkflowRequest<TParameters>
            {
                WorkflowId = _workflowId,
                AppId = _appId,
                Parameters = parameters
            };
            return await _client.RunWorkflowAsync(request);
        }

        public async Task<RunWorkflowResponse<TResponse>> RunWorkflowStreamingAsync(TParameters parameters)
        {
            var request = new WorkflowRequest<TParameters>
            {
                WorkflowId = _workflowId,
                AppId = _appId,
                Parameters = parameters
            };
            return await _client.RunWorkflowStreamingAsync(request);
        }

        public async Task<RunWorkflowResponse<TResponse>> ResumeWorkflowAsync(string eventId, string resumeData, int interruptType)
        {
            var request = new WorkflowResumeRequest
            {
                EventId = eventId,
                WorkflowId = _workflowId,
                ResumeData = resumeData,
                InterruptType = interruptType
            };
            return await _client.ResumeWorkflowAsync(request);
        }
    }

    // Workflow请求的泛型模型
    public class WorkflowRequest<TParameters>
    {
        [JsonPropertyName("workflow_id")]
        public string WorkflowId { get; set; }
    
        [JsonPropertyName("app_id")]
        public string AppId { get; set; }
    
        [JsonPropertyName("parameters")]
        public TParameters Parameters { get; set; }
    }
}

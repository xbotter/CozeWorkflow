using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Xunit;  // Replace NUnit with xUnit
using Moq;
using Moq.Protected;

namespace CozeWorkflow.UnitTests
{
    // Remove [TestFixture] attribute
    public class CozeWorkflowClientUnitTests
    {
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _mockHttpClient;
        private const string WorkflowId = "test_workflow_id";
        private const string AppId = "test_app_id";

        // Replace [SetUp] with constructor
        public CozeWorkflowClientUnitTests()
        {
            // Setup code
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _mockHttpClient = new HttpClient(_httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("https://api.example.com")
            };
            _mockHttpClient.DefaultRequestHeaders.Add("Authorization", "Bearer test_token");
        }

        // Replace [Test] with [Fact]
        [Fact]
        public async Task RunWorkflowAsync_ReturnsExpectedResponse()
        {
            // Arrange
            var parameters = new { Key = "Value" };
            var expectedResponse = new RunWorkflowResponse<object>
            {
                Code = 200,
                Data = JsonSerializer.Serialize(new { result = "success" }),
                Msg = "Success"
            };
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse), Encoding.UTF8, "application/json")
            };
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(httpResponse);

            // 使用注入的方式创建 workflow 实例
            var workflow = new CozeWorkflow<object, object>(_mockHttpClient, WorkflowId, AppId);

            // Act
            var response = await workflow.RunWorkflowAsync(parameters);

            // Assert
            Assert.Equal(200, response.Code);
            Assert.Equal("success", ((JsonElement)response.ParsedData).GetProperty("result").GetString());
        }

        [Fact]
        public async Task RunWorkflowAsync_ThrowsException_WhenApiReturnsError()
        {
            // Arrange
            var parameters = new { Key = "Value" };
            var errorResponse = new { error = "Invalid request" };
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(JsonSerializer.Serialize(errorResponse))
                });

            var workflow = new CozeWorkflow<object, object>(_mockHttpClient, WorkflowId, AppId);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => workflow.RunWorkflowAsync(parameters));
        }

        [Fact]
        public async Task RunWorkflowStreamingAsync_YieldsExpectedEvents()
        {
            // Arrange
            var parameters = new { Key = "Value" };
            var events = new[]
            {
                "id: 1\nevent: Message\ndata: {\"content\":\"Hello\",\"node_title\":\"Start\",\"node_seq_id\":\"1\",\"node_is_finish\":false}\n\n",
                "id: 2\nevent: Message\ndata: {\"content\":\"World\",\"node_title\":\"End\",\"node_seq_id\":\"2\",\"node_is_finish\":true}\n\n"
            };

            var responseStream = new MemoryStream(Encoding.UTF8.GetBytes(string.Join("", events)));
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(responseStream)
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(response);

            var workflow = new CozeWorkflow<object, object>(_mockHttpClient, WorkflowId, AppId);

            // Act
            var receivedEvents = new List<WorkflowEvent>();
            await foreach (var evt in workflow.RunWorkflowStreamingAsync(parameters))
            {
                receivedEvents.Add(evt);
            }

            // Assert
            Assert.Equal(2, receivedEvents.Count);
            Assert.Equal(WorkflowEventType.Message, receivedEvents[0].EventType);
            Assert.Equal(WorkflowEventType.Message, receivedEvents[1].EventType);
            Assert.Equal("Hello", ((MessageEventData)receivedEvents[0].Data).Content);
            Assert.Equal("World", ((MessageEventData)receivedEvents[1].Data).Content);
        }

        [Fact]
        public async Task ResumeWorkflowAsync_HandlesInterruptEvent()
        {
            // Arrange
            var eventId = "event123";
            var resumeData = "resume_data";
            var interruptType = 1;
            var events = new[]
            {
                "id: 1\nevent: Interrupt\ndata: {\"interrupt_data\":{\"event_id\":\"event123\",\"type\":1},\"node_title\":\"Decision\"}\n\n"
            };

            var responseStream = new MemoryStream(Encoding.UTF8.GetBytes(string.Join("", events)));
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(responseStream)
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(response);

            var workflow = new CozeWorkflow<object, object>(_mockHttpClient, WorkflowId, AppId);

            // Act
            var receivedEvents = new List<WorkflowEvent>();
            await foreach (var evt in workflow.ResumeWorkflowAsync(eventId, resumeData, interruptType))
            {
                receivedEvents.Add(evt);
            }

            // Assert
            Assert.Single(receivedEvents);
            Assert.Equal(WorkflowEventType.Interrupt, receivedEvents[0].EventType);
            var interruptData = (InterruptEventData)receivedEvents[0].Data;
            Assert.Equal(eventId, interruptData.InterruptData.EventId);
            Assert.Equal(interruptType, interruptData.InterruptData.Type);
        }

        [Theory]
        [InlineData("Message", WorkflowEventType.Message)]
        [InlineData("Interrupt", WorkflowEventType.Interrupt)]
        [InlineData("Error", WorkflowEventType.Error)]
        [InlineData("Unknown", WorkflowEventType.Unknown)]
        public void WorkflowEvent_Parse_HandlesAllEventTypes(string eventType, WorkflowEventType expectedType)
        {
            // Arrange
            var eventString = $"id: 1\nevent: {eventType}\ndata: {{}}\n\n";

            // Act
            var workflowEvent = WorkflowEvent.Parse(eventString);

            // Assert
            Assert.Equal(1, workflowEvent.Id);
            Assert.Equal(expectedType, workflowEvent.EventType);
        }

        [Fact]
        public async Task RunWorkflowAsync_ValidatesRequestParameters()
        {
            // Arrange
            var workflow = new CozeWorkflow<object, object>(_mockHttpClient, WorkflowId, AppId);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => workflow.RunWorkflowAsync(null));
        }
    }
}

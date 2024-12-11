// 示例数据类型
using System.Text.Json;
using System.Text.Json.Serialization;
using CozeWorkflow;
using Microsoft.Extensions.Configuration;


public class WorkflowInput {
    [JsonPropertyName("user_id")]
    public string UserId { get; set; }
    
    [JsonPropertyName("user_name")]
    public string UserName { get; set; }
    [JsonPropertyName("BOT_USER_INPUT")]
    public string Input { get; set; }
}


public class WorkflowOutput {
    [JsonPropertyName("output")]
    public string Output {get;set;}
}



// 使用示例
public static class Program
{
    public static async Task Main(string[] args)
    {
        var baseUrl = "https://api.coze.com";
        // Get Oauth token from user secret
        var config = new ConfigurationBuilder().AddUserSecrets(typeof(Program).Assembly).Build();
        var authToken = config["COZE_AUTH_TOKEN"];
        

        var workflow = new CozeWorkflow<WorkflowInput, WorkflowOutput>(baseUrl, authToken, "7445581085495394309", "7408201825042186245");

        try
        {
            Console.WriteLine("What regex do you want to generate?");
            var input =  Console.ReadLine();

            var parameters = new WorkflowInput
            {
                UserId = "12345",
                UserName = "George",
                Input = input
            };

            Console.WriteLine("Streaming response:");

            await foreach (var workflowEvent in workflow.RunWorkflowStreamingAsync(parameters))
            {
                if (workflowEvent.EventType == WorkflowEventType.Message)
                {
                    var messageData = workflowEvent.Data as MessageEventData;
                    if (messageData != null)
                    {
                        Console.WriteLine(messageData.Content);
                    }
                }
                else if (workflowEvent.EventType == WorkflowEventType.Interrupt)
                {
                    var interruptData = workflowEvent.Data as InterruptEventData;
                    // 处理中断事件
                }
                else if (workflowEvent.EventType == WorkflowEventType.Error)
                {
                    var errorData = workflowEvent.Data as ErrorEventData;
                    Console.WriteLine($"Error: {errorData.ErrorMessage}");
                }
                else
                {
                    // 处理其他事件类型
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}

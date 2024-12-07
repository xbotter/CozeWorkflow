
// 示例数据类型
using System.Text.Json;
using System.Text.Json.Serialization;
using CozeWorkflow;


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
        // Get Oauth token from Environment Variables

        var authToken = Environment.GetEnvironmentVariable("COZE_AUTH_TOKEN");



        var workflow = new CozeWorkflow<WorkflowInput, WorkflowOutput>(baseUrl, authToken, "7445581085495394309", "7408201825042186245");

        try
        {
            Console.WriteLine("Do you want to generate what regex?");
            var input =  Console.ReadLine();
            var response = await workflow.RunWorkflowAsync(new WorkflowInput
            {
                UserId = "12345",
                UserName = "George",
                Input = input
            });

            Console.WriteLine("RunWorkflowAsync Response:");
            Console.WriteLine(JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine(response.ParsedData.Output);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}

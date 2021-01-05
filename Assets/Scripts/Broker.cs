using System.Threading.Tasks;
using Amazon.Runtime;

public static class Broker
{
    private const string url = "wss://23c7rz261g.execute-api.us-east-1.amazonaws.com/stage";
    private static WebSocketClient client = new WebSocketClient(url);
    public static ImmutableCredentials Credentials { set { client.Credentials = value; } }

    public async static Task Subscribe(string topic)
    {
        await client.Subscribe(topic);
    }

    public static void DispatchMessageQueue()
    {
        client.DispatchMessageQueue();
    }

    public async static Task UnSubscribe()
    {
        await client.UnSubscribe();
    }
}

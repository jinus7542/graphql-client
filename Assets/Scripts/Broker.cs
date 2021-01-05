using System;
using System.Threading.Tasks;
using Amazon.Runtime;
using NativeWebSocket;

public static class Broker
{
    private const string url = "wss://h7no5cf5ch.execute-api.us-east-1.amazonaws.com/stage";
    private static BrokerClient client = new BrokerClient(url);
    public static ImmutableCredentials Credentials { set { client.Credentials = value; } }

    public async static Task Subscribe(string topic, Action<string> onOpen, Action<string> onPublish, Action<string> onError, Action<WebSocketCloseCode> onClose)
    {
        await client.Subscribe(topic, onOpen, onPublish, onError, onClose);
    }

    public static void Dispatch()
    {
        client.Dispatch();
    }

    public async static Task Close()
    {
        await client.Close();
    }
}

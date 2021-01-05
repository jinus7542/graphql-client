using System;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Runtime;
using NativeWebSocket;
using AWSSignatureV4_S3_Sample.Signers;
using LitJson;

public class BrokerClient
{
    private string region;
    private string url;
    private WebSocket socket;
    public ImmutableCredentials Credentials { private get; set; }

    public BrokerClient(string url)
    {
        this.region = url.Split('.')[2];
        this.url = url;
    }

    private WebSocket newSocket()
    {
        var headers = new Dictionary<string, string>();

        if (null != this.Credentials)
        {
            var signer = new AWS4SignerForAuthorizationHeader
            {
                EndpointUri = new Uri(this.url),
                HttpMethod = "GET",
                Service = "execute-api",
                Region = this.region
            };
            var queryParameters = "";
            if (1 < this.url.Split('?').Length)
            {
                queryParameters = this.url.Split('?')[1];
            }
            var authorization = signer.ComputeSignature(headers,
                                                        queryParameters,
                                                        AWS4SignerBase.EMPTY_BODY_SHA256,
                                                        this.Credentials.AccessKey,
                                                        this.Credentials.SecretKey);
            headers.Add("Authorization", authorization);
            headers.Add("x-amz-security-token", this.Credentials.Token);
            headers.Remove("Host");
        }

        return (new WebSocket(this.url, headers));
    }

    public async Task Subscribe(string topic, Action<string> onOpen, Action<string> onPublish, Action<string> onError, Action<WebSocketCloseCode> onClose)
    {
        if (null != this.socket)
        {
            throw new Exception($"already subscribed.");
        }

        this.socket = newSocket();
        this.socket.OnOpen += () =>
        {
            var message = new { action = "send", data = topic };
            Task.Run(() => this.SendText(JsonMapper.ToJson(message)));
            onOpen(topic);
        };
        this.socket.OnMessage += (data) =>
        {
            var json = Encoding.UTF8.GetString(data);
            onPublish(json);
        };
        this.socket.OnError += (error) =>
        {
            onError(error);
        };
        this.socket.OnClose += (closeCode) =>
        {
            this.socket = null;
            onClose(closeCode);
        };
        await this.socket.Connect();
    }

    public async Task SendText(string message)
    {
        if (null != this.socket && WebSocketState.Open == this.socket.State)
        {
            await this.socket.SendText(message);
        }
    }

    public void Dispatch()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        this.socket?.DispatchMessageQueue();
#endif
    }

    public async Task Close()
    {
        await this.socket?.Close();
    }
}

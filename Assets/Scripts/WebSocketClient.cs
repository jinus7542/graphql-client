using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Amazon.Runtime;
using NativeWebSocket;
using AWSSignatureV4_S3_Sample.Signers;

public class WebSocketClient
{
    private string url;
    private WebSocket websocket;
    public ImmutableCredentials Credentials { private get; set; }

    public WebSocketClient(string url)
    {
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
                Region = "us-east-1"
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

    public async Task Subscribe(string topic)
    {
        if (null != this.websocket)
        {
            throw new Exception($"already subscribed.");
        }

        this.websocket = newSocket();
        this.websocket.OnOpen += () =>
        {
            Debug.Log("Connection open!");
        };
        this.websocket.OnError += (e) =>
        {
            Debug.Log("Error! " + e);
        };
        this.websocket.OnClose += (e) =>
        {
            this.websocket = null;
            Debug.Log("Connection closed!");
        };
        this.websocket.OnMessage += (bytes) =>
        {
            var message = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log("Received OnMessage! (" + bytes.Length + " bytes) " + message);
        };
        await this.websocket.Connect();
    }

    public void DispatchMessageQueue()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (null != this.websocket)
        {
            this.websocket.DispatchMessageQueue();
        }
#endif
    }

    public async Task UnSubscribe()
    {
        if (null != this.websocket)
        {
            await this.websocket.Close();
        }
    }
}

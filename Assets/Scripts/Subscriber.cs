using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Amazon.Runtime;
using NativeWebSocket;
using AWSSignatureV4_S3_Sample.Signers;
using LitJson;

namespace GW
{
    public static class Subscriber
    {
        public class Message
        {
            public string Raw { get; private set; }

            public Message(string json)
            {
                Raw = json;
            }

            public JsonData ToObject()
            {
                return JsonMapper.ToObject(Raw);
            }

            public T ToObject<T>()
            {
                return JsonMapper.ToObject<T>(Raw);
            }
        }

        public delegate void EventHandlerOnOpen(string topic);
        public delegate void EventHandlerOnMessage(string topic, Message message);
        public delegate void EventHandlerOnError(string error);
        public delegate void EventHandlerOnClose(WebSocketCloseCode closeCode);

        public class Client
        {
            private string region;
            private string url;
            private WebSocket socket;
            public ImmutableCredentials Credentials { private get; set; }
            public event EventHandlerOnOpen OnOpen;
            public event EventHandlerOnMessage OnMessage;
            public event EventHandlerOnError OnError;
            public event EventHandlerOnClose OnClose;

            public Client(string url)
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

            public async Task Subscribe(string topic)
            {
                if (null != this.socket)
                {
                    throw new Exception($"already opened.");
                }

                this.socket = newSocket();
                this.socket.OnOpen += () =>
                {
                    this.OnOpen?.Invoke(topic);
                    var obj = new { action = "send", data = topic };
                    Task.Run(() => this.Send(obj));
                };
                this.socket.OnMessage += (data) =>
                {
                    var json = Encoding.UTF8.GetString(data);
                    this.OnMessage?.Invoke("", new Message(json));
                };
                this.socket.OnError += (error) =>
                {
                    this.OnError?.Invoke(error);
                };
                this.socket.OnClose += (closeCode) =>
                {
                    this.socket = null;
                    this.OnClose?.Invoke(closeCode);
                };
                await this.socket.Connect();
            }

            public void UnSubscribe(string topic)
            {
            }

            public async Task Send(object obj)
            {
                if (null != this.socket && WebSocketState.Open == this.socket.State)
                {
                    var json = JsonMapper.ToJson(obj);
                    await this.socket.SendText(json);
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

        private const string url = "wss://h7no5cf5ch.execute-api.us-east-1.amazonaws.com/stage";
        private static Client client = new Client(url);
        public static ImmutableCredentials Credentials { set { client.Credentials = value; } }
        public static event EventHandlerOnOpen OnOpen
        {
            add { client.OnOpen += value; }
            remove { client.OnOpen -= value; }
        }
        public static event EventHandlerOnMessage OnMessage
        {
            add { client.OnMessage += value; }
            remove { client.OnMessage -= value; }
        }
        public static event EventHandlerOnError OnError
        {
            add { client.OnError += value; }
            remove { client.OnError -= value; }
        }
        public static event EventHandlerOnClose OnClose
        {
            add { client.OnClose += value; }
            remove { client.OnClose -= value; }
        }

        public async static Task Subscribe(string topic)
        {
            await client.Subscribe(topic);
        }

        public static void UnSubscribe(string topic)
        {
            client.UnSubscribe(topic);
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
}

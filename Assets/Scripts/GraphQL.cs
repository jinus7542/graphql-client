using System;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using UnityEngine.Networking;
using Amazon.Runtime;
using LitJson;
using AWSSignatureV4_S3_Sample.Signers;

namespace GW
{
    public static class GraphQL
    {
        public class Client
        {
            private string region;
            private string url;
            public ImmutableCredentials Credentials { private get; set; }

            public Client(string url)
            {
                this.region = url.Split('.')[2];
                this.url = url;
            }

            private UnityWebRequest QueryRequest(string query)
            {
                var json = JsonMapper.ToJson(new { query = query });
                var request = UnityWebRequest.Post(this.url, UnityWebRequest.kHttpVerbPOST);
                var payload = Encoding.UTF8.GetBytes(json);
                var headers = new Dictionary<string, string>
            {
                { "content-type", "application/json" },
            };

                request.uploadHandler = new UploadHandlerRaw(payload);
                if (null != this.Credentials)
                {
                    var contentHash = AWS4SignerBase.CanonicalRequestHashAlgorithm.ComputeHash(payload);
                    var contentHashString = AWS4SignerBase.ToHexString(contentHash, true);

                    headers.Add(AWS4SignerBase.X_Amz_Content_SHA256, contentHashString);
                    var signer = new AWS4SignerForAuthorizationHeader
                    {
                        EndpointUri = new Uri(this.url),
                        HttpMethod = "POST",
                        Service = "execute-api",
                        Region = this.region
                    };
                    var authorization = signer.ComputeSignature(headers,
                                                                string.Empty,
                                                                contentHashString,
                                                                this.Credentials.AccessKey,
                                                                this.Credentials.SecretKey);
                    headers.Add("Authorization", authorization);
                    headers.Add("x-amz-security-token", this.Credentials.Token);   // Add the IAM authentication token
                    headers.Remove("Host");
                }
                foreach (var header in headers)
                {
                    request.SetRequestHeader(header.Key, header.Value);
                }

                return request;
            }

            private IEnumerator SendRequest(string query, Action<Response> callback = null, int timeoutSeconds = 10)
            {
                using (var www = QueryRequest(query))
                {
                    www.timeout = timeoutSeconds;
                    yield return www.SendWebRequest();

                    var text = (!www.isNetworkError) ? www.downloadHandler.text : "";
                    var error = (www.isNetworkError) ? www.error : null;
                    if (null != callback)
                    {
                        callback(new Response(text, error));
                    }
                }
            }

            public void Query(string query, Action<Response> callback = null, int timeoutSeconds = 10)
            {
                Coroutiner.StartCoroutine(SendRequest(query, callback, timeoutSeconds));
            }
        }

        public class Response
        {
            public string Raw { get; private set; }
            private readonly JsonData data;
            public string NetworkError { get; private set; }
            public string GraphQLError { get; private set; }

            public Response(string text, string error = null)
            {
                Raw = text;
                NetworkError = error;
                if (null == error)
                {
                    var root = JsonMapper.ToObject(text);
                    if (root.Keys.Contains("errors"))
                    {
                        GraphQLError = JsonMapper.ToJson(root["errors"][0]);
                        text = null;
                    }
                    if (!root.Keys.Contains("errors") && !root.Keys.Contains("data"))
                    {
                        GraphQLError = text;
                        text = null;
                    }
                }
                data = (null != text) ? JsonMapper.ToObject(text) : null;
            }

            public T ToObject<T>(string resolver)
            {
                return JsonMapper.ToObject<T>(GetData()[resolver].ToJson());
            }

            private JsonData GetData()
            {
                return (null == data) ? null : data["data"];
            }

            public string GetError()
            {
                if (null != NetworkError)
                {
                    return NetworkError;
                }
                if (null != GraphQLError)
                {
                    var error = JsonMapper.ToObject(GraphQLError);
                    if (!error.Keys.Contains("extensions"))
                    {
                        return GraphQLError;
                    }
                    return (string)error["extensions"]["code"]; // error code
                }
                return null;
            }
        }

        private const string url = "https://w4y6kms2ji.execute-api.us-east-1.amazonaws.com/stage/public";
        private static readonly Client client = new Client(url);
        public static ImmutableCredentials Credentials { set { client.Credentials = value; } }

        public static void Query(string query, Action<Response> callback = null, int timeoutSeconds = 10)
        {
            client.Query(query, callback, timeoutSeconds);
        }
    }
}

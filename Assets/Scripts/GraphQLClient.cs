using System;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using UnityEngine.Networking;
using LitJson;
using AWSSignatureV4_S3_Sample.Signers;

namespace GraphQL
{
    public class GraphQLClient
    {
        private string url;

        public GraphQLClient(string url)
        {
            this.url = url;
        }

        private UnityWebRequest QueryRequest(string query, string token = null)
        {
            var json = JsonMapper.ToJson(new { query = query });
            var request = UnityWebRequest.Post(url, UnityWebRequest.kHttpVerbPOST);
            var payload = Encoding.UTF8.GetBytes(json);

            var headers = new Dictionary<string, string>
            {
                { "content-type", "application/json" },
            };
            if (null != TestScript.ImmutableCredentials)
            {
                var contentHash = AWS4SignerBase.CanonicalRequestHashAlgorithm.ComputeHash(payload);
                var contentHashString = AWS4SignerBase.ToHexString(contentHash, true);

                headers.Add(AWS4SignerBase.X_Amz_Content_SHA256, contentHashString);

                var signer = new AWS4SignerForAuthorizationHeader
                {
                    EndpointUri = new Uri(url),
                    HttpMethod = "POST",
                    Service = "execute-api",
                    Region = "us-east-1"
                };
                var authorization = signer.ComputeSignature(headers,
                                                            string.Empty,
                                                            contentHashString,
                                                            TestScript.ImmutableCredentials.AccessKey,
                                                            TestScript.ImmutableCredentials.SecretKey);
                headers.Add("Authorization", authorization);
                headers.Remove("Host");

                // Add the IAM authentication token
                request.SetRequestHeader("x-amz-security-token", TestScript.ImmutableCredentials.Token);
            }
            foreach (var header in headers)
            {
                request.SetRequestHeader(header.Key, header.Value);
            }

            request.uploadHandler = new UploadHandlerRaw(payload);

            return request;
        }

        private IEnumerator SendRequest(string query, Action<GraphQLResponse> callback = null, int timeoutSeconds = 10, string token = null)
        {
            using (var www = QueryRequest(query, token))
            {
                www.timeout = timeoutSeconds;
                yield return www.SendWebRequest();
                if (www.isNetworkError)
                {
                    if (callback != null)
                    {
                        callback(new GraphQLResponse("", www.error));
                    }
                }
                else
                {
                    if (null != callback)
                    {
                        var text = www.downloadHandler.text;
                        callback(new GraphQLResponse(text));
                    }
                }
            }
        }

        public void Query(string query, Action<GraphQLResponse> callback = null, int timeoutSeconds = 10, string token = "")
        {
            Coroutiner.StartCoroutine(SendRequest(query, callback, timeoutSeconds, token));
        }
    }
}

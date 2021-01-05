using System;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using UnityEngine.Networking;
using LitJson;
using Amazon.Runtime;
using AWSSignatureV4_S3_Sample.Signers;

public class GraphQLClient
{
    private string url;
    public ImmutableCredentials Credentials { private get; set; }

    public GraphQLClient(string url)
    {
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
                Region = "us-east-1"
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

    private IEnumerator SendRequest(string query, Action<GraphQLResponse> callback = null, int timeoutSeconds = 10)
    {
        using (var www = QueryRequest(query))
        {
            www.timeout = timeoutSeconds;
            yield return www.SendWebRequest();

            var text = (!www.isNetworkError) ? www.downloadHandler.text : "";
            var error = (www.isNetworkError) ? www.error : null;
            if (null != callback)
            {
                callback(new GraphQLResponse(text, error));
            }
        }
    }

    public void Query(string query, Action<GraphQLResponse> callback = null, int timeoutSeconds = 10)
    {
        Coroutiner.StartCoroutine(SendRequest(query, callback, timeoutSeconds));
    }
}

using System;
using Amazon.Runtime;

public static class API
{
    private const string url = "https://w4y6kms2ji.execute-api.us-east-1.amazonaws.com/stage/public";
    private static readonly GraphQLClient client = new GraphQLClient(url);
    public static ImmutableCredentials Credentials { set { client.Credentials = value; } }

    public static void Query(string query, Action<GraphQLResponse> callback = null, int timeoutSeconds = 10)
    {
        client.Query(query, callback, timeoutSeconds);
    }
}

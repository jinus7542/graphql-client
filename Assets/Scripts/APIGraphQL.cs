using System;

namespace GraphQL
{
    public static class APIGraphQL
    {
        private const string ApiURL = "https://w4y6kms2ji.execute-api.us-east-1.amazonaws.com/stage/public";
        public static string Token = null;

        public static bool LoggedIn
        {
            get { return !Token.Equals(""); } // TODO:: improve loggedin verification
        }

        private static readonly GraphQLClient API = new GraphQLClient(ApiURL);

        public static void Query(string query, Action<GraphQLResponse> callback = null, int timeoutSeconds = 10)
        {
            API.Query(query, callback, timeoutSeconds, Token);
        }
    }
}
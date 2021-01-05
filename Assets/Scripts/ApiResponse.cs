using LitJson;

public class ApiResponse
{
    public string Raw { get; private set; }
    private readonly JsonData data;
    public string NetworkError { get; private set; }
    public string GraphQLError { get; private set; }

    public ApiResponse(string text, string error = null)
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

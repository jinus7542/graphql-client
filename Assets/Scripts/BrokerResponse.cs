using LitJson;

public class BrokerResponse
{
    public string Raw { get; private set; }

    public BrokerResponse(string json)
    {
        Raw = json;
    }

    public T ToObject<T>()
    {
        return JsonMapper.ToObject<T>(Raw);
    }
}

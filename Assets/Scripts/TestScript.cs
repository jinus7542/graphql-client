using System.Threading.Tasks;
using UnityEngine;
using Amazon;
using Amazon.CognitoIdentity;
using Amazon.Runtime;
using LitJson;
using NativeWebSocket;

public class TestScript : MonoBehaviour
{
    private class User
    {
        public int id;
        public string name;
    }

    void CognitoInit()
    {
        UnityInitializer.AttachToGameObject(this.gameObject);
        Amazon.AWSConfigs.HttpClient = AWSConfigs.HttpClientOption.UnityWebRequest;
        // Initialize the Amazon Cognito credentials provider
        var credentials = new CognitoAWSCredentials(
            "us-east-1:17291f69-1f95-4f69-976f-20f7a5937b0a", // Identity Pool ID
            Amazon.RegionEndpoint.USEast1 // Region
        );

        //Debug.Log("FB Cognito auth");
        //credentials.AddLogin("graph.facebook.com", AccessToken.CurrentAccessToken.TokenString);
        credentials.GetCredentialsAsync(CognitoGetCredentialsCallback, null);
    }

    private void CognitoGetCredentialsCallback(AmazonCognitoIdentityResult<ImmutableCredentials> result)
    {
        if (null != result.Exception)
        {
            Debug.Log(result.Exception);
            return;
        }

        var credentials = result.Response;
        Debug.Log(string.Format("Cognito credentials: {0},\n{1},\n,{2}", credentials.AccessKey, credentials.SecretKey, credentials.Token));
        Api.Credentials = credentials;
        Broker.Credentials = credentials;
    }

    void callbackSignup(ApiResponse response)
    {
        // TODO:: error handling
        var error = response.GetError();
        if (null != error)
        {
            Debug.Log(error);
            if ("INVALID_PARAMETERS" == error)
            {
            }
            return;
        }
        // TODO:: implementation
        {
            var obj = response.ToObject<User>("signup");
            Debug.Log(JsonMapper.ToJson(obj));
        }
    }

    void mutationSignup()
    {
        var param = new object[] { 1111, "jinus7542" };
        var query = @"mutation {
                            signup(id: $id, name: $name) {
                                id,
                                name
                            }
                        }".Build(param);
        Api.Query(query, callbackSignup);
    }

    void callbackSigninFriends(ApiResponse response)
    {
        // TODO:: error handling
        var error = response.GetError();
        if (null != error)
        {
            Debug.Log(error);
            if ("INVALID_PARAMETERS" == error)
            {
            }
            return;
        }
        // TODO:: implementation
        {
            var obj = response.ToObject<User>("signin");
            Debug.Log(JsonMapper.ToJson(obj));
        }
        {
            var obj = response.ToObject<User[]>("friends");
            Debug.Log(JsonMapper.ToJson(obj));
        }
    }

    void querySigninFriends()
    {
        var param = new object[] { 2222 };
        var query = @"query {
                            signin(id: $id) {
                                id,
                                name
                            },
                            friends {
                                id,
                                name
                            }
                        }".Build(param);
        Api.Query(query, callbackSigninFriends);
    }

    void OnOpen(string topic)
    {
        Debug.Log($"OnOpen. {topic}");
    }

    void OnPublish(string json)
    {
        Debug.Log($"OnPublish. {json}");
    }

    void OnError(string error)
    {
        Debug.Log($"OnError. {error}");
    }

    void OnClose(WebSocketCloseCode closeCode)
    {
        Debug.Log($"OnClose. {closeCode}");
    }

    void OnGUI()
    {
        GUI.Box(new Rect(10, 10, 150, 120), "Test");

        if (GUI.Button(new Rect(20, 40, 120, 20), "cognito"))
        {
            CognitoInit();
        }
        if (GUI.Button(new Rect(20, 70, 120, 20), "signup"))
        {
            mutationSignup();
        }
        if (GUI.Button(new Rect(20, 100, 120, 20), "subscribe"))
        {
            Task.Run(() => Broker.Subscribe("topic", OnOpen, OnPublish, OnError, OnClose));
        }
    }

    void Update()
    {
        Broker.Dispatch();
    }

    void OnApplicationQuit()
    {
        Task.Run(() => Broker.Close());
    }
}

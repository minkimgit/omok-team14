using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using SocketIOClient;
using Newtonsoft.Json.Linq;

public class NetworkManager : Singleton<NetworkManager>
{
    public SocketIOUnity Socket { get; private set; }
    public event Action<bool, string> OnRegisterResponseReceived;

    private void Start()
    {
        // Start에서는 소켓 객체만 생성하고 연결(Connect)은 하지 않습니다.
        if (Instance == this)
        {
            SetupSocket();
        }
    }

    private void SetupSocket()
    {
        var uri = new Uri("http://127.0.0.1:3000");
        Socket = new SocketIOUnity(uri);

        Socket.OnConnected += (sender, e) => Debug.Log("[Network] 서버 연결 성공!");
        Socket.OnDisconnected += (sender, e) => Debug.LogWarning("[Network] 서버 연결 종료");
        Socket.OnError += (sender, e) => Debug.LogError($"[Network] 소켓 에러: {e}");

        Socket.On("registerResponse", (response) =>
        {
            try
            {
                // 원본 JSON 문자열을 직접 JObject로 파싱합니다.
                // response.ToString()이 "[{...}]" 형태이므로 JArray로 먼저 받은 뒤 첫 번째 요소를 가져옵니다.
                var jArray = JArray.Parse(response.ToString());
                var data = jArray[0] as JObject;

                if (data != null)
                {
                    bool success = data["success"]?.Value<bool>() ?? false;
                    string message = data["message"]?.Value<string>() ?? "메시지 없음";

                    Debug.Log($"[Network] 파싱 성공: {success}, {message}");

                    UnityThread.executeInUpdate(() => {
                        OnRegisterResponseReceived?.Invoke(success, message);
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Network] 직접 파싱 에러: {ex.Message}");
            }
        });
    }

    // 외부에서 연결이 필요할 때 호출하는 함수
    public void ConnectToServer(Action onConnected = null)
    {
        if (Socket.Connected)
        {
            onConnected?.Invoke();
            return;
        }

        // 연결 성공 시 콜백 실행을 위해 일회성 이벤트 등록 가능
        if (onConnected != null)
        {
            EventHandler connectedHandler = null;
            connectedHandler = (sender, e) => {
                onConnected.Invoke();
                Socket.OnConnected -= connectedHandler;
            };
            Socket.OnConnected += connectedHandler;
        }

        Socket.Connect();
    }

    public void RequestRegister(string email, string pw)
    {
        // 연결된 상태라면 바로 요청, 아니면 연결 후 요청
        if (Socket.Connected)
        {
            EmitRegister(email, pw);
        }
        else
        {
            Debug.Log("[Network] 연결되지 않음. 연결 시도 후 요청합니다.");
            ConnectToServer(() => EmitRegister(email, pw));
        }
    }

    private void EmitRegister(string email, string pw)
    {
        var data = new { email = email, password = pw };
        Socket.Emit("register", data);
        Debug.Log($"[Network] 가입 요청 발송: {email}");
    }

    protected override void OnSceneLoad(Scene scene, LoadSceneMode mode) { }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (Socket != null)
        {
            Socket.Disconnect();
            Socket.Dispose();
        }
    }
}
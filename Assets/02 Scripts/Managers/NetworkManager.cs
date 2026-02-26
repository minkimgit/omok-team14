using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using SocketIOClient;
using Newtonsoft.Json.Linq;

public class NetworkManager : Singleton<NetworkManager>
{
    public SocketIOUnity Socket { get; private set; }
    public event Action<bool, string, int> OnRegisterResponseReceived;
    public event Action<bool, string, int> OnLoginResponseReceived;

    private Coroutine connectionTimer; // 타임아웃 체크용 코루틴

    private void Start()
    {
        if (Instance == this)
        {
            SetupSocket();
            
            Debug.Log("[Network] 서버 연결 시도 중...");
            ConnectToServer();
        }
    }

    private void SetupSocket()
    {
        var uri = new Uri("http://127.0.0.1:3000");
        
        Socket = new SocketIOUnity(uri, new SocketIOOptions
        {
            Reconnection = true,
            ReconnectionAttempts = 3,
            ReconnectionDelay = 2000,
            ConnectionTimeout = TimeSpan.FromSeconds(3) // 소켓 자체 타임아웃 설정
        });

        // 연결 성공 시 타이머 중단
        Socket.OnConnected += (sender, e) => {
            // 반드시 UnityThread.executeInUpdate를 사용하여 메인 스레드로 넘겨야 합니다.
            UnityThread.executeInUpdate(() => {
                if (connectionTimer != null) StopCoroutine(connectionTimer);
                Debug.Log("<color=green>[Network] 서버 연결이 최종적으로 완료되었습니다!</color>");
            });
        };
        
        Socket.OnDisconnected += (sender, e) => Debug.Log("[Network] 서버 연결 종료 (오프라인 모드 활성)");
        
        Socket.OnError += (sender, e) => Debug.Log($"[Network] 소켓 연결 오류 발생.");

        // --- 응답 처리 로직 (기존과 동일하지만 registerResponse 오타 수정됨) ---
        Socket.On("registerResponse", (response) =>
        {
            try {
                var jArray = JArray.Parse(response.ToString());
                var data = jArray[0] as JObject;
                if (data != null) {
                    bool success = data["success"]?.Value<bool>() ?? false;
                    string message = data["message"]?.Value<string>() ?? "";
                    int code = data["code"]?.Value<int>() ?? -1;

                    UnityThread.executeInUpdate(() => {
                        // OnRegisterResponseReceived로 올바르게 호출되도록 수정
                        OnRegisterResponseReceived?.Invoke(success, message, code);
                    });
                }
            } catch (Exception ex) { Debug.LogError($"[Network] 가입 파싱 에러: {ex.Message}"); }
        });
        
        Socket.On("loginResponse", (response) =>
        {
            try {
                var jArray = JArray.Parse(response.ToString());
                var data = jArray[0] as JObject;
                if (data != null) {
                    bool success = data["success"]?.Value<bool>() ?? false;
                    string message = data["message"]?.Value<string>() ?? "";
                    int code = data["code"]?.Value<int>() ?? -1;
                    UnityThread.executeInUpdate(() => OnLoginResponseReceived?.Invoke(success, message, code));
                }
            } catch (Exception ex) { Debug.LogError($"[Network] 로그인 파싱 에러: {ex.Message}"); }
        });
    }

    public void ConnectToServer(Action onConnected = null)
    {
        if (Socket.Connected)
        {
            onConnected?.Invoke();
            return;
        }

        // 연결 성공 시 콜백 등록
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

        // [추가] 3초 타임아웃 체크 시작
        if (connectionTimer != null) StopCoroutine(connectionTimer);
        connectionTimer = StartCoroutine(ConnectWithTimeout(3f));
    }

    // --- [핵심 추가] 타임아웃 코루틴 ---
    private IEnumerator ConnectWithTimeout(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (!Socket.Connected)
        {
            // 3초가 지났는데도 연결이 안 된 경우
            Debug.Log($"<color=orange>[Network] {duration}초 동안 서버 응답이 없습니다. 연결 시도를 중단하고 오프라인 모드로 전환합니다.</color>");
            
            // 더 이상의 시도를 중단하려면 Disconnect() 호출
            Socket.Disconnect(); 

            // !! 여기에 나중에 팝업창을 띄우는 함수를 넣으시면 됩니다 !!
        }
    }

    public void RequestRegister(string email, string pw)
    {
        if (Socket.Connected) EmitRegister(email, pw);
        else OnRegisterResponseReceived?.Invoke(false, "오프라인 상태입니다.", -1);
    }

    private void EmitRegister(string email, string pw)
    {
        var data = new { email = email, password = pw };
        Socket.Emit("register", data);
    }
    
    public void RequestLogin(string email, string pw)
    {
        if (Socket.Connected)
        {
            Socket.Emit("login", new { email, password = pw });
        }
        else
        {
            ConnectToServer(() => {
                Socket.Emit("login", new { email, password = pw });
            });
        }
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
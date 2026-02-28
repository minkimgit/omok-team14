using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class NetworkManager : Singleton<NetworkManager>
{
    public SocketIOUnity Socket { get; private set; }
    
    // 인증 관련 이벤트
    public event Action<bool, string, int> OnRegisterResponseReceived;
    public event Action<bool, string, int> OnLoginResponseReceived;

    private Coroutine connectionTimer;

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
            ConnectionTimeout = TimeSpan.FromSeconds(3)
        });

        // 기능별 리스너 분리 호출
        SetupSystemListeners();    // 연결, 에러 등 시스템 이벤트
        SetupAuthListeners();      // 회원가입, 로그인 응답
        SetupGameplayListeners();  // 매칭, 돌 놓기 등 게임 플레이
    }

    #region 1. 시스템 리스너 (Connection, Error)
    private void SetupSystemListeners()
    {
        Socket.OnConnected += (sender, e) => {
            UnityThread.executeInUpdate(() => {
                if (connectionTimer != null) StopCoroutine(connectionTimer);
                Debug.Log("<color=green>[Network] 서버 연결이 최종적으로 완료되었습니다!</color>");
            });
        };
        
        Socket.OnDisconnected += (sender, e) => Debug.Log("[Network] 서버 연결 종료 (오프라인 모드 활성)");
        Socket.OnError += (sender, e) => Debug.Log($"[Network] 소켓 연결 오류 발생.");
    }
    #endregion

    #region 2. 인증 리스너 (Login, Register)
    private void SetupAuthListeners()
    {
        // 회원가입 응답
        Socket.On("registerResponse", (response) =>
        {
            ParseAndInvokeAuthResponse(response, OnRegisterResponseReceived, "가입");
        });
        
        // 로그인 응답
        Socket.On("loginResponse", (response) =>
        {
            ParseAndInvokeAuthResponse(response, OnLoginResponseReceived, "로그인");
        });
    }

    // 공통 인증 데이터 파싱 도우미
    private void ParseAndInvokeAuthResponse(SocketIOResponse response, Action<bool, string, int> action, string label)
    {
        try {
            var data = JArray.Parse(response.ToString())[0] as JObject;
            if (data != null) {
                bool success = data["success"]?.Value<bool>() ?? false;
                string message = data["message"]?.Value<string>() ?? "";
                int code = data["code"]?.Value<int>() ?? -1;
                UnityThread.executeInUpdate(() => action?.Invoke(success, message, code));
            }
        } catch (Exception ex) { Debug.LogError($"[Network] {label} 파싱 에러: {ex.Message}"); }
    }
    #endregion

    #region 3. 게임플레이 리스너 (Match, Stone)
    private void SetupGameplayListeners()
    {
        Socket.On("matchFound", (response) =>
        {
            try {
                // 기존 파싱 로직: response를 JArray로 파싱 후 첫 번째 요소(JObject) 추출
                var data = JArray.Parse(response.ToString())[0] as JObject;
        
                int playerNum = data["myPlayerNumber"].Value<int>();
                bool isTurn = data["isMyTurn"].Value<bool>();
                // [추가] 서버에서 보낸 누가 흑돌(선공)인지에 대한 정보 추출
                int startingPlayer = data["startingPlayer"].Value<int>();

                UnityThread.executeInUpdate(() => {
                    Debug.Log("<color=yellow>[Network] 매칭 성공! 게임을 시작합니다.</color>");
                    GameManager.Instance.OnMatchSuccess();
    
                    // startingPlayer 인자를 추가하여 코루틴 호출
                    StartCoroutine(SetupGameAfterSceneLoad(playerNum, isTurn, startingPlayer));
                });
            } catch (Exception ex) { 
                Debug.LogError($"[Network] 매칭 데이터 파싱 에러: {ex.Message}"); 
            }
        });

        // 상대방(또는 나)의 돌 놓기 정보 수신
        Socket.On("stonePlaced", (response) =>
        {
            try {
                var data = JArray.Parse(response.ToString())[0] as JObject;
                if (data != null) {
                    int row = data["row"].Value<int>();
                    int col = data["col"].Value<int>();
                    int player = data["player"].Value<int>();

                    UnityThread.executeInUpdate(() => {
                        MultiplayGameController.Instance.OnOpponentPlacedStone(row, col, player);
                        Debug.Log($"[Network] 돌 수신: ({row}, {col}) - Player {player}");
                    });
                }
            } catch (Exception ex) { Debug.LogError($"[Network] 돌 데이터 파싱 에러: {ex.Message}"); }
        });
    }
    #endregion

    #region 서버 요청 함수들 (Emit)
    public void ConnectToServer(Action onConnected = null)
    {
        if (Socket.Connected) { onConnected?.Invoke(); return; }

        if (onConnected != null) {
            EventHandler connectedHandler = null;
            connectedHandler = (sender, e) => {
                onConnected.Invoke();
                Socket.OnConnected -= connectedHandler;
            };
            Socket.OnConnected += connectedHandler;
        }

        Socket.Connect();
        if (connectionTimer != null) StopCoroutine(connectionTimer);
        connectionTimer = StartCoroutine(ConnectWithTimeout(3f));
    }

    public void RequestRegister(string email, string pw)
    {
        if (Socket.Connected) Socket.Emit("register", new { email, password = pw });
        else OnRegisterResponseReceived?.Invoke(false, "오프라인 상태입니다.", -1);
    }

    public void RequestLogin(string email, string pw)
    {
        if (Socket.Connected) Socket.Emit("login", new { email, password = pw });
        else ConnectToServer(() => Socket.Emit("login", new { email, password = pw }));
    }

    public void JoinMatchmaking()
    {
        if (Socket == null || !Socket.Connected) return;
        Debug.Log("<color=cyan>서버에 매칭 대기열 합류 요청을 보냈습니다. (매칭 중...)</color>");
        Socket.Emit("requestMatchmaking", new { email = GameManager.Instance.UserEmail });
    }
    
    public void EmitPlaceStone(int row, int col)
    {
        if (Socket == null || !Socket.Connected) return;
        Socket.Emit("placeStone", new { row, col });
    }
    #endregion

    private IEnumerator ConnectWithTimeout(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (!Socket.Connected) {
            Debug.Log($"<color=orange>[Network] {duration}초 동안 서버 응답이 없습니다.</color>");
            Socket.Disconnect(); 
        }
    }

    protected override void OnSceneLoad(Scene scene, LoadSceneMode mode) { }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (Socket != null) { Socket.Disconnect(); Socket.Dispose(); }
    }
    
    // 씬 로드가 완료될 때까지 잠시 기다렸다가 정보를 넘겨줌
    private IEnumerator SetupGameAfterSceneLoad(int playerNum, bool isTurn, int startingPlayer)
    {
        // 씬 전환 애니메이션 및 로딩 시간을 고려하여 대기
        yield return new WaitForSeconds(0.5f); 
    
        if (MultiplayGameController.Instance != null)
        {
            // MultiplayGameController에 모든 정보를 전달
            MultiplayGameController.Instance.SetGameStart(playerNum, isTurn, startingPlayer);
        }
        else
        {
            Debug.LogError("[Network] MultiplayGameController를 찾을 수 없습니다!");
        }
    }
}
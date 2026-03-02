using UnityEngine;

public class MultiplayGameController : Singleton<MultiplayGameController>
{
    [SerializeField] private GameSceneController gameSceneController;
    
    public bool IsMyTurn { get; private set; }
    public int MyPlayerNumber { get; private set; } // 1: 흑, 2: 백

    // 게임 시작 시 서버 정보를 받아 설정하는 함수
    public void SetGameStart(int myNum, bool startsFirst, int startingPlayer)
    {
        MyPlayerNumber = myNum;
        IsMyTurn = startsFirst;

        // 만약 인스펙터 연결이 빠졌다면, 현재 씬에서 직접 찾음
        if (gameSceneController == null)
        {
            gameSceneController = FindObjectOfType<GameSceneController>();
        }

        if (gameSceneController != null)
        {
            gameSceneController.InitializeMultiplay(startingPlayer, MyPlayerNumber, IsMyTurn);
        }
        else
        {
            Debug.LogError("[Multiplay] GameSceneController를 씬에서 찾을 수 없습니다!");
        }
    }

    // [내가 클릭했을 때]
    public void HandleBoardClick(int row, int col)
    {
        if (!IsMyTurn) return;  // 내 턴이 아닌 경우 착수 금지
        
        NetworkManager.Instance.EmitPlaceStone(row, col);
        
        // 💡 팁: 내 화면에 바로 그리지 않는 이유
        // 서버를 거쳐서 돌아오는 'stonePlaced' 이벤트를 통해 그리는 것이 
        // 양쪽 클라이언트의 데이터를 일치시키는 데 더 확실합니다.
    }

    // 서버에서 stonePlaced 이벤트가 오면 호출됨
    public void OnOpponentPlacedStone(int row, int col, int playerNum)
    {
        // 1. 돌 배치 (이 함수는 내부에서 보드에 돌을 그림)
        gameSceneController.PlaceStone(row, col);

        // 2. 서버가 알려준 '방금 둔 사람'의 다음 사람으로 턴 계산
        int nextPlayer = (playerNum == 1) ? 2 : 1;

        // 3. 내 턴 여부 업데이트
        IsMyTurn = (nextPlayer == MyPlayerNumber);

        // 4. [중요] GameSceneController의 내부 턴 상태와 UI를 강제 동기화
        // 타이머와 강조 UI를 한꺼번에 업데이트합니다.
        gameSceneController.SyncMultiplayState(
            nextPlayer, 
            gameSceneController.GetPlayerATime(), 
            gameSceneController.GetPlayerBTime()
        );
    }

    // 싱글톤 추상 함수 구현
    protected override void OnSceneLoad(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode) { }
}
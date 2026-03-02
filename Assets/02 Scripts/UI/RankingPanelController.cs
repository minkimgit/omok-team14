using UnityEngine;

// Ranking Popup Prefab에 부착하는 컨트롤러
// 인스펙터에서:
//   - Content    : Scroll View > Viewport > Content 오브젝트를 연결하세요.
//   - Ranking Item Prefab : RankingPrefab을 연결하세요.
public class RankingPanelController : PanelController
{
    [SerializeField] private Transform content;           // 스크롤 뷰의 Content 오브젝트
    [SerializeField] private GameObject rankingItemPrefab; // RankingPrefab

    // 패널 열기 — GameManager.OpenRankingPanel()에서 호출
    public new void Show()
    {
        base.Show();

        // 기존 목록 초기화
        foreach (Transform child in content)
            Destroy(child.gameObject);

        // 서버에서 랭킹 데이터를 받으면 PopulateList 한 번만 호출
        NetworkManager.Instance.OnLeaderboardReceived += PopulateList;
        NetworkManager.Instance.RequestLeaderboard();
    }

    // 랭킹 데이터 수신 후 목록 생성
    private void PopulateList(LeaderboardEntry[] entries)
    {
        // 데이터를 받았으면 바로 구독 해제 (중복 갱신 방지)
        NetworkManager.Instance.OnLeaderboardReceived -= PopulateList;

        // 혹시 남아 있는 이전 항목 제거
        foreach (Transform child in content)
            Destroy(child.gameObject);

        for (int i = 0; i < entries.Length; i++)
        {
            var item = Instantiate(rankingItemPrefab, content);
            item.GetComponent<RankingItemController>().Setup(i + 1, entries[i].email, entries[i].elo);
        }
    }

    // 닫기 버튼 — 인스펙터에서 닫기/X 버튼의 OnClick에 연결하세요.
    public void OnClickCloseButton()
    {
        Hide();
    }

    // 패널이 파괴될 때 이벤트 구독을 안전하게 해제
    private void OnDestroy()
    {
        if (NetworkManager.Instance != null)
            NetworkManager.Instance.OnLeaderboardReceived -= PopulateList;
    }
}

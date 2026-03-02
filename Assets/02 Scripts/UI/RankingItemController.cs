using TMPro;
using UnityEngine;

// RankingPrefab의 각 행(row)을 제어하는 컴포넌트
// 인스펙터에서 Rank Text / Name Text / Score Text 필드를 연결하세요.
public class RankingItemController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI scoreText;

    // 랭킹 행 초기화 — RankingPanelController에서 호출
    public void Setup(int rank, string playerName, int score)
    {
        rankText.text  = rank.ToString();
        nameText.text  = playerName;
        scoreText.text = score.ToString();
    }
}

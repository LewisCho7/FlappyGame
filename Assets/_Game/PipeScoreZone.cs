using UnityEngine;

/// <summary>
/// 파이프 틈 한가운데의 통과 판정. 새가 지나가면 점수를 1 올린다.
/// </summary>
/// <remarks>
/// 점수 숫자는 <see cref="UIManager"/>가 <see cref="ScoreManager.OnScoreChanged"/>를
/// 구독해서 알아서 갱신한다. 여기서 UI를 직접 건드리지 않는다.
/// </remarks>
[RequireComponent(typeof(Collider2D))]
public class PipeScoreZone : MonoBehaviour
{
    [Tooltip("통과했을 때 낼 소리. 비워 두면 소리 없이 동작한다. Audio/point를 넣으면 된다.")]
    [SerializeField] private AudioClip scoreClip;

    /// <summary>파이프 하나당 한 번만 센다. 풀에서 다시 꺼낼 때마다 초기화된다.</summary>
    private bool scored;

    private void OnEnable()
    {
        scored = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (scored) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        // 새 말고 다른 것이 지나가도 점수가 오르지 않게 한다.
        if (other.GetComponent<BirdController>() == null) return;

        scored = true;

        if (ScoreManager.Instance != null) ScoreManager.Instance.AddScore();
        if (scoreClip != null && SoundManager.Instance != null) SoundManager.Instance.PlaySFX(scoreClip);
    }
}

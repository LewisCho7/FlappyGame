using UnityEngine;

/// <summary>
/// 상태 전환에 붙는 연출용 소리. 새나 파이프에 딸리지 않는 소리를 여기서 낸다.
/// </summary>
/// <remarks>
/// 게임오버 소리는 <see cref="BirdDeath"/>가 hit / die로 이미 내고 있으므로
/// 여기서 또 겹쳐 틀지 않는다.
/// </remarks>
public class GameSoundPlayer : MonoBehaviour
{
    [Tooltip("게임이 시작될 때. Audio/swoosh를 넣으면 된다.")]
    [SerializeField] private AudioClip startClip;

    private void OnEnable()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnStateChanged += ApplyState;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= ApplyState;
    }

    /// <summary>일시정지 해제와 진짜 시작을 구분하기 위해 직전 상태를 들고 있는다.</summary>
    private GameState previous = GameState.Ready;

    private void ApplyState(GameState state)
    {
        GameState from = previous;
        previous = state;

        if (state != GameState.Playing) return;
        if (from == GameState.Paused) return;   // Esc로 재개한 것은 시작이 아니다

        if (startClip != null && SoundManager.Instance != null) SoundManager.Instance.PlaySFX(startClip);
    }
}

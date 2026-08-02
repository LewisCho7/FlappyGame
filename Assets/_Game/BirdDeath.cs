using UnityEngine;

/// <summary>
/// 새가 죽는 조건. 파이프에 부딪히거나 화면 위아래로 벗어나면 게임오버를 알린다.
/// 상태를 바꾸는 것은 <see cref="GameManager"/>이고 여기서는 알리기만 한다.
/// </summary>
/// <remarks>
/// <see cref="GameManager.TriggerGameOver"/>는 멱등이라, 부딪힌 프레임에 화면 밖 판정까지
/// 같이 걸려도 안전하다.
/// </remarks>
[RequireComponent(typeof(Collider2D))]
public class BirdDeath : MonoBehaviour
{
    [Header("화면 밖 판정")]
    [Tooltip("카메라 위/아래 끝에서 이만큼 더 벗어나면 죽는다.")]
    [SerializeField, Min(0f)] private float outOfViewMargin = 0.5f;

    [Header("소리")]
    [Tooltip("부딪혔을 때. 비워 두면 소리 없이 동작한다. Audio/hit을 넣으면 된다.")]
    [SerializeField] private AudioClip hitClip;

    [Tooltip("화면 밖으로 떨어져 나갈 때. Audio/die를 넣으면 된다.")]
    [SerializeField] private AudioClip fallClip;

    /// <summary>이 y를 넘어가면 죽는다. 카메라 크기에서 구하므로 화면 설정을 바꿔도 따라온다.</summary>
    private float deathY;

    /// <summary>떨어지는 소리는 한 번만 낸다. 재시작은 씬을 다시 부르므로 되돌릴 필요가 없다.</summary>
    private bool fallSoundPlayed;

    private void Awake()
    {
        Camera cam = Camera.main;
        deathY = (cam != null ? cam.orthographicSize : 5f) + outOfViewMargin;
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;
        if (Mathf.Abs(transform.position.y) <= deathY) return;

        // 화면 밖으로 나갔다. 살아 있었다면 이게 사망 원인이고,
        // 이미 파이프에 부딪혀 죽은 뒤라면 떨어져 나가는 마무리 소리다. 어느 쪽이든 한 번만 운다.
        if (!fallSoundPlayed)
        {
            fallSoundPlayed = true;
            PlayClip(fallClip);
        }

        GameManager.Instance.TriggerGameOver();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        PlayClip(hitClip);
        GameManager.Instance.TriggerGameOver();
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip != null && SoundManager.Instance != null) SoundManager.Instance.PlaySFX(clip);
    }
}

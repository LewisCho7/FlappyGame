using UnityEngine;

/// <summary>
/// 스프라이트를 차례로 바꿔서 날갯짓을 보여준다.
/// Animator Controller 없이 <see cref="SpriteRenderer"/>만 갈아끼우므로
/// 인스펙터에서 프레임을 바로 넣고 뺄 수 있다.
/// </summary>
public class BirdFlapAnimator : MonoBehaviour
{
    [Header("프레임")]
    [Tooltip("순서대로 보여줄 스프라이트. Bird1-1_0 ~ Bird1-1_3 순으로 넣는다.")]
    [SerializeField] private Sprite[] frames;

    [Tooltip("초당 프레임 수.")]
    [SerializeField, Min(0.1f)] private float framesPerSecond = 8f;

    private SpriteRenderer spriteRenderer;
    private float elapsed;
    private int index;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (spriteRenderer == null || frames == null || frames.Length == 0) return;

        // 떨어지는 새가 계속 퍼덕이면 어색하므로 게임오버에서는 프레임을 멈춘다.
        if (GameManager.Instance != null && GameManager.Instance.State == GameState.GameOver) return;

        elapsed += Time.deltaTime;

        float interval = 1f / framesPerSecond;
        // 프레임이 크게 밀렸을 때도 남은 시간이 쌓이지 않도록 while로 따라잡는다.
        while (elapsed >= interval)
        {
            elapsed -= interval;
            index = (index + 1) % frames.Length;
            spriteRenderer.sprite = frames[index];
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 새의 조작. Space를 누르면 위로 튀어오른다.
/// 상태 판단은 <see cref="GameManager"/>에 맡기고, 이 스크립트는 Playing일 때만 입력을 받는다.
/// </summary>
/// <remarks>
/// 게임오버 뒤에는 입력만 막고 물리는 그대로 둔다.
/// timeScale을 건드리지 않기 때문에 새는 그대로 떨어지면서 연출이 이어진다.
/// </remarks>
[RequireComponent(typeof(Rigidbody2D))]
public class BirdController : MonoBehaviour
{
    [Header("점프")]
    [Tooltip("Space를 눌렀을 때 위로 향하는 속도(월드 단위/초).")]
    [SerializeField] private float jumpVelocity = 5f;

    [Tooltip("점프할 때 낼 소리. 비워 두면 소리 없이 동작한다. Audio/wing을 넣으면 된다.")]
    [SerializeField] private AudioClip jumpClip;

    private Rigidbody2D body;

    /// <summary>Ready로 돌아갈 때 되돌릴 처음 위치.</summary>
    private Vector3 startPosition;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
    }

    private void OnEnable()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnStateChanged += ApplyState;
        ApplyState(GameManager.Instance.State);
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= ApplyState;
    }

    private void Update()
    {
        // 게임오버·일시정지·시작 전에는 입력을 아예 받지 않는다. 조작 차단은 이 한 줄이 전부다.
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.spaceKey.wasPressedThisFrame) Jump();
    }

    /// <summary>위로 튀어오른다. 인스펙터 버튼이나 다른 입력에서도 부를 수 있게 public으로 둔다.</summary>
    public void Jump()
    {
        // 힘을 더하지 않고 속도를 덮어쓴다. 연타해도 속도가 쌓이지 않아 조작감이 일정하다.
        body.linearVelocity = new Vector2(body.linearVelocity.x, jumpVelocity);

        if (jumpClip != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(jumpClip);
        }
    }

    /// <summary>
    /// Ready에서는 물리를 꺼서 제자리에 띄워 두고, Playing이 되면 그때부터 떨어지기 시작한다.
    /// GameOver에서는 아무것도 하지 않는다 — 물리가 계속 돌아 바닥까지 떨어진다.
    /// </summary>
    private void ApplyState(GameState state)
    {
        switch (state)
        {
            case GameState.Ready:
                transform.position = startPosition;
                body.linearVelocity = Vector2.zero;
                body.simulated = false;
                break;

            case GameState.Playing:
                body.simulated = true;
                break;
        }
    }
}

using UnityEngine;

/// <summary>
/// 배경 타일이 화면 밖으로 나가 오른쪽 끝으로 되돌아오는 순간에만 스프라이트를 갈아끼운다.
/// 화면 한가운데서 그림이 퍽 바뀌지 않고 새 배경이 오른쪽에서 밀려 들어온다.
/// </summary>
/// <remarks>
/// <see cref="AutoScroller2D"/>가 Update에서 타일을 옮기므로 판정은 LateUpdate에서 한다.
/// 덕분에 스크립트 실행 순서를 따로 지정할 필요가 없다.
/// 어떤 스프라이트로 바꿀지는 <see cref="StageProgression"/>이 정해서 넘겨준다.
/// </remarks>
[RequireComponent(typeof(AutoScroller2D))]
public class BackgroundThemeSwitcher : MonoBehaviour
{
    [Tooltip("되돌아온 것으로 볼 x 증가량. 타일은 왼쪽으로만 흐르므로 x가 이만큼 늘면 순환한 것이다.")]
    [SerializeField, Min(0.01f)] private float wrapThreshold = 1f;

    [Tooltip("화면 밖 판정에 둘 여유. 경계에 딱 걸친 타일을 바꿔서 깜빡이는 것을 막는다.")]
    [SerializeField, Min(0f)] private float edgeMargin = 0.1f;

    private SpriteRenderer[] tiles;
    private float[] lastX;
    private Camera view;

    /// <summary>다음에 되돌아오는 타일부터 적용할 스프라이트.</summary>
    private Sprite pending;

    private void Awake()
    {
        EnsureCached();
    }

    /// <summary>
    /// 배경을 바꾼다. <b>지금 화면 밖에 있는 타일은 즉시</b> 갈아끼우고,
    /// 화면에 보이는 타일만 순환할 때까지 기다린다.
    /// 이미 오른쪽 대기 중인 타일까지 바꿔야 다음에 들어오는 화면이 곧바로 새 배경이 된다.
    /// </summary>
    public void SetTheme(Sprite sprite)
    {
        if (sprite == null) return;

        EnsureCached();
        pending = sprite;

        if (view == null) return;

        float halfWidth = view.orthographicSize * view.aspect;
        float left = view.transform.position.x - halfWidth - edgeMargin;
        float right = view.transform.position.x + halfWidth + edgeMargin;

        for (int i = 0; i < tiles.Length; i++)
        {
            Bounds b = tiles[i].bounds;
            bool onScreen = b.max.x > left && b.min.x < right;

            if (!onScreen) tiles[i].sprite = sprite;
        }
    }

    /// <summary>모든 타일을 즉시 바꾼다. Ready로 되돌아갈 때 쓴다.</summary>
    public void ApplyThemeNow(Sprite sprite)
    {
        if (sprite == null) return;

        EnsureCached();
        pending = sprite;

        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i].sprite = sprite;
            lastX[i] = tiles[i].transform.localPosition.x;
        }
    }

    private void LateUpdate()
    {
        if (tiles == null || pending == null) return;

        for (int i = 0; i < tiles.Length; i++)
        {
            float x = tiles[i].transform.localPosition.x;

            // 왼쪽으로만 흐르는데 x가 크게 늘었다면 방금 오른쪽 끝으로 되돌아온 것이다.
            // 이 시점의 타일은 화면 바깥에 있으므로 갈아끼워도 보이지 않는다.
            if (x > lastX[i] + wrapThreshold) tiles[i].sprite = pending;

            lastX[i] = x;
        }
    }

    /// <summary>
    /// Awake 순서에 기대지 않도록 필요할 때 만들어 둔다.
    /// StageProgression이 OnEnable에서 먼저 부를 수 있기 때문이다.
    /// </summary>
    private void EnsureCached()
    {
        if (tiles != null) return;

        tiles = GetComponentsInChildren<SpriteRenderer>(true);
        lastX = new float[tiles.Length];
        for (int i = 0; i < tiles.Length; i++) lastX[i] = tiles[i].transform.localPosition.x;

        view = Camera.main;
    }
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 배경·지면 무한 스크롤. 이 오브젝트의 <b>자식들</b>을 한 방향으로 밀고,
/// 뒤로 벗어난 자식을 맨 끝으로 되돌려서 이어 붙인다.
/// <code>
/// Background (AutoScroller2D)
/// ├── Tile_0  (SpriteRenderer)
/// ├── Tile_1
/// └── Tile_2   ← 같은 간격으로 나란히 두면 개수는 상관없다
/// </code>
/// </summary>
/// <remarks>
/// GameManager의 상태를 구독해서 GameOver면 멈추고 Ready면 처음 배치로 돌아가므로,
/// 보통은 인스펙터에 값만 넣고 아무 것도 호출하지 않는다.
/// 일시정지(timeScale = 0)는 Time.deltaTime이 0이 되면서 자동으로 멈춘다.
/// </remarks>
public class AutoScroller2D : MonoBehaviour
{
    [Header("스크롤")]
    [Tooltip("초당 이동 거리(월드 단위).")]
    [SerializeField] private float speed = 2f;

    [Tooltip("이동 방향. 플래피류는 왼쪽으로 흐른다.")]
    [SerializeField] private Vector2 direction = Vector2.left;

    [Tooltip("타일 하나의 길이. 0이면 자식 간격이나 스프라이트 크기에서 자동으로 구한다.")]
    [SerializeField, Min(0f)] private float tileLength = 0f;

    [Tooltip("게임 시작 전(Ready)에도 흐를지 여부.")]
    [SerializeField] private bool scrollInReady = true;

    /// <summary>초당 이동 거리. 난이도에 따라 바꿀 수 있다.</summary>
    public float Speed
    {
        get => speed;
        set => speed = value;
    }

    public bool IsScrolling { get; private set; }

    /// <summary>움직일 자식들. 처음 위치를 그대로 기억해 둔다.</summary>
    private readonly List<Transform> tiles = new List<Transform>();
    private readonly List<Vector3> startPositions = new List<Vector3>();

    /// <summary>direction을 정규화한 축.</summary>
    private Vector2 axis = Vector2.left;

    /// <summary>각 자식의 시작 위치를 axis에 투영한 값.</summary>
    private readonly List<float> startOffsets = new List<float>();

    /// <summary>되돌림이 일어나는 구간의 시작점과 전체 길이.</summary>
    private float minOffset;
    private float span;

    /// <summary>지금까지 흐른 거리. span으로 감싸서 오차가 쌓이지 않게 한다.</summary>
    private float scrolled;

    private void Awake()
    {
        CacheTiles();
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += ApplyState;
            ApplyState(GameManager.Instance.State);
        }
        else
        {
            // GameManager 없이 단독으로 써도 동작하게 둔다.
            IsScrolling = true;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= ApplyState;
    }

    public void StartScroll()
    {
        IsScrolling = true;
    }

    public void StopScroll()
    {
        IsScrolling = false;
    }

    /// <summary>처음 배치로 되돌린다.</summary>
    public void ResetScroll()
    {
        scrolled = 0f;
        ApplyPositions();
    }

    private void Update()
    {
        if (!IsScrolling || span <= 0f) return;

        scrolled += speed * Time.deltaTime;
        // 절대값이 커지면 float 정밀도가 떨어지므로 한 바퀴마다 접어 준다.
        scrolled = Mathf.Repeat(scrolled, span);

        ApplyPositions();
    }

    private void ApplyState(GameState state)
    {
        switch (state)
        {
            case GameState.Ready:
                ResetScroll();
                IsScrolling = scrollInReady;
                break;

            case GameState.Playing:
                IsScrolling = true;
                break;

            case GameState.Paused:
                // timeScale이 0이라 Update가 진행되지 않는다. 상태를 건드릴 필요가 없다.
                break;

            case GameState.GameOver:
                IsScrolling = false;
                break;
        }
    }

    /// <summary>자식 목록과 되돌림 구간을 계산한다.</summary>
    private void CacheTiles()
    {
        tiles.Clear();
        startPositions.Clear();
        startOffsets.Clear();

        axis = direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector2.left;

        foreach (Transform child in transform)
        {
            tiles.Add(child);
            startPositions.Add(child.localPosition);
            startOffsets.Add(Vector2.Dot(child.localPosition, axis));
        }

        if (tiles.Count == 0)
        {
            span = 0f;
            Debug.LogWarning(
                $"[AutoScroller2D] '{name}'에 자식이 없습니다. 스크롤할 스프라이트를 자식으로 넣으세요.", this);
            return;
        }

        minOffset = float.MaxValue;
        foreach (float offset in startOffsets)
        {
            if (offset < minOffset) minOffset = offset;
        }

        float length = tileLength > 0f ? tileLength : MeasureTileLength();

        if (length <= 0f)
        {
            span = 0f;
            Debug.LogWarning(
                $"[AutoScroller2D] '{name}'의 타일 길이를 알 수 없습니다. tileLength를 직접 넣으세요.", this);
            return;
        }

        span = length * tiles.Count;
        ApplyPositions();
    }

    /// <summary>
    /// 타일 길이 추정. 자식이 둘 이상이면 실제 간격을 쓰고,
    /// 하나뿐이면 스프라이트 크기에서 구한다.
    /// </summary>
    private float MeasureTileLength()
    {
        if (tiles.Count >= 2)
        {
            float min = float.MaxValue;
            float max = float.MinValue;
            foreach (float offset in startOffsets)
            {
                if (offset < min) min = offset;
                if (offset > max) max = offset;
            }

            float spacing = (max - min) / (tiles.Count - 1);
            if (spacing > Mathf.Epsilon) return spacing;
        }

        var renderer2D = tiles[0].GetComponent<SpriteRenderer>();
        if (renderer2D == null || renderer2D.sprite == null) return 0f;

        // 로컬 공간 크기로 환산한다.
        Vector3 size = renderer2D.sprite.bounds.size;
        Vector3 scale = tiles[0].localScale;
        return Mathf.Abs(axis.x) * size.x * scale.x + Mathf.Abs(axis.y) * size.y * scale.y;
    }

    /// <summary>
    /// 각 자식을 (시작 위치 + 흐른 거리)에 놓되, 구간을 벗어난 만큼 되돌려서 순환시킨다.
    /// 위치를 매번 시작값에서 다시 계산하므로 오차가 누적되지 않는다.
    /// </summary>
    private void ApplyPositions()
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            Transform tile = tiles[i];
            if (tile == null) continue;

            float target = startOffsets[i] + scrolled;
            float wrapped = minOffset + Mathf.Repeat(target - minOffset, span);

            tile.localPosition = startPositions[i] + (Vector3)(axis * (wrapped - startOffsets[i]));
        }
    }
}

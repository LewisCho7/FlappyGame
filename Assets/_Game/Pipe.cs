using UnityEngine;

/// <summary>
/// 파이프 한 쌍. 왼쪽으로 흐르다가 화면을 벗어나면 스스로 풀에 돌아간다.
/// 색과 세로 위치는 <see cref="PipeSpawner"/>가 정해서 넘겨준다.
/// </summary>
/// <remarks>
/// 위쪽 파이프는 아래쪽과 같은 스프라이트를 상하반전(flipY)해서 쓴다.
/// 시트의 아래쪽 캡이 위쪽 캡의 정확한 상하반전이라 결과가 동일하고,
/// 스프라이트를 두 벌 들고 있을 필요가 없다.
/// </remarks>
public class Pipe : MonoBehaviour
{
    [Header("스프라이트")]
    [SerializeField] private SpriteRenderer topCap;
    [SerializeField] private SpriteRenderer topBody;
    [SerializeField] private SpriteRenderer bottomCap;
    [SerializeField] private SpriteRenderer bottomBody;

    [Header("반납")]
    [Tooltip("이 x보다 왼쪽으로 가면 풀에 반납한다. 카메라 왼쪽 끝(-8.89)보다 넉넉히 바깥이어야 한다.")]
    [SerializeField] private float releaseX = -10f;

    /// <summary>돌아갈 풀. 스폰될 때 받는다.</summary>
    private ObjectPool owner;

    private float speed;

    /// <summary>
    /// 흐르는 속도. 단계가 올라가면 <see cref="PipeSpawner"/>가 떠 있는 파이프까지 한꺼번에 바꾼다.
    /// 앞뒤 파이프의 속도가 다르면 뒤엣것이 앞엣것을 따라잡아 간격이 무너지기 때문이다.
    /// </summary>
    public float Speed
    {
        get => speed;
        set => speed = Mathf.Max(0f, value);
    }

    /// <summary>스폰 직후 한 번 부른다. 이동 속도와 돌아갈 풀을 받는다.</summary>
    public void Launch(ObjectPool pool, float moveSpeed)
    {
        owner = pool;
        speed = moveSpeed;
    }

    /// <summary>색을 갈아끼운다. 위아래가 같은 스프라이트를 공유한다.</summary>
    public void ApplyStyle(Sprite cap, Sprite body)
    {
        if (cap == null || body == null) return;

        topCap.sprite = cap;
        bottomCap.sprite = cap;
        topBody.sprite = body;
        bottomBody.sprite = body;
    }

    private void Update()
    {
        // 게임오버 후에도 계속 흐르는 것을 막는다. 배경(AutoScroller2D)과 같은 판단이다.
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        transform.position += Vector3.left * (speed * Time.deltaTime);

        // 화면 밖 판정은 _Game 책임이다. 풀은 화면을 모른다.
        if (transform.position.x < releaseX && owner != null) owner.Release(gameObject);
    }
}

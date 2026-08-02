using UnityEngine;

/// <summary>
/// 일정 간격으로 화면 오른쪽 밖에서 파이프를 꺼내 놓는다.
/// 생성은 <see cref="ObjectPool"/>에 맡기고, 반납은 <see cref="Pipe"/>가 스스로 한다.
/// </summary>
public class PipeSpawner : MonoBehaviour
{
    /// <summary>파이프 한 벌의 색. 캡과 몸통이 짝을 이룬다.</summary>
    [System.Serializable]
    public class PipeStyle
    {
        public Sprite cap;
        public Sprite body;
    }

    [Header("풀")]
    [Tooltip("Pipe 프리팹을 들고 있는 ObjectPool.")]
    [SerializeField] private ObjectPool pipePool;

    [Header("생성")]
    [Tooltip("몇 초마다 한 쌍씩 내보낼지.")]
    [SerializeField, Min(0.1f)] private float interval = 2f;

    [Tooltip("생성 x 위치. 카메라 오른쪽 끝(8.89)보다 바깥이어야 갑자기 나타나지 않는다.")]
    [SerializeField] private float spawnX = 10f;

    [Tooltip("틈 중심의 세로 흔들림 폭(±). 0이면 항상 화면 한가운데에 생긴다.")]
    [SerializeField, Min(0f)] private float gapCenterRange = 1.5f;

    [Tooltip("파이프가 왼쪽으로 흐르는 속도. 배경보다 빨라야 원근감이 산다.")]
    [SerializeField, Min(0f)] private float moveSpeed = 3f;

    [Header("색")]
    [Tooltip("스폰할 때마다 이 중 하나를 무작위로 고른다.")]
    [SerializeField] private PipeStyle[] styles;

    /// <summary>이 스포너가 내보낸 적 있는 파이프. 속도를 한꺼번에 바꾸려고 들고 있는다.</summary>
    private readonly System.Collections.Generic.List<Pipe> launched = new System.Collections.Generic.List<Pipe>();

    /// <summary>
    /// 파이프 속도. <see cref="StageProgression"/>이 단계에 따라 바꾼다.
    /// <b>이미 날아가는 파이프까지 같이</b> 바뀐다 — 앞뒤 속도가 다르면
    /// 뒤엣것이 앞엣것을 따라잡아 간격이 무너진다.
    /// </summary>
    public float MoveSpeed
    {
        get => moveSpeed;
        set
        {
            moveSpeed = Mathf.Max(0f, value);

            for (int i = 0; i < launched.Count; i++)
            {
                if (launched[i] != null) launched[i].Speed = moveSpeed;
            }
        }
    }

    /// <summary>생성 간격. 속도를 올릴 때 같이 줄여야 파이프 간격이 유지된다.</summary>
    public float Interval
    {
        get => interval;
        set => interval = Mathf.Max(0.1f, value);
    }

    private float timer;

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
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        timer += Time.deltaTime;
        if (timer < interval) return;

        // 프레임이 밀려도 간격이 늘어지지 않도록 뺄셈으로 처리한다.
        timer -= interval;
        Spawn();
    }

    private void Spawn()
    {
        if (pipePool == null) return;

        float gapCenterY = Random.Range(-gapCenterRange, gapCenterRange);

        GameObject instance = pipePool.Get(new Vector3(spawnX, gapCenterY, 0f), Quaternion.identity);
        if (instance == null) return;

        var pipe = instance.GetComponent<Pipe>();
        if (pipe == null)
        {
            Debug.LogError($"[PipeSpawner] 풀의 프리팹에 Pipe 컴포넌트가 없습니다.", this);
            return;
        }

        // 풀이 재사용하므로 같은 파이프가 여러 번 나온다. 목록에는 한 번만 넣는다.
        if (!launched.Contains(pipe)) launched.Add(pipe);

        pipe.Launch(pipePool, moveSpeed);

        if (styles != null && styles.Length > 0)
        {
            PipeStyle style = styles[Random.Range(0, styles.Length)];
            pipe.ApplyStyle(style.cap, style.body);
        }
    }

    /// <summary>Ready로 돌아가면 화면에 남아 있던 파이프를 전부 걷어낸다.</summary>
    private void ApplyState(GameState state)
    {
        if (state != GameState.Ready) return;

        timer = 0f;
        if (pipePool != null) pipePool.ReleaseAll();
    }
}

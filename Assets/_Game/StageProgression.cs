using UnityEngine;

/// <summary>
/// 점수가 오르면 배경과 속도를 단계별로 바꾼다.
/// 기준 점수·배경·속도를 전부 인스펙터의 <see cref="Stage"/> 목록에서 조절한다.
/// <b>마지막 단계를 넘어선 뒤에도</b> 일정 점수마다 속도가 계속 올라가고, 상한에서 멈춘다.
/// 배경은 목록에 있는 것까지만 쓰고 그 뒤로는 마지막 배경을 유지한다.
/// </summary>
/// <remarks>
/// 배경은 <see cref="BackgroundThemeSwitcher"/>에 맡긴다. 화면 밖 타일은 즉시 바뀌고
/// 보이는 타일만 순환할 때까지 기다리므로, 새 배경이 곧바로 오른쪽에서 밀려 들어온다.
/// 파이프 속도는 떠 있는 것까지 한꺼번에 바뀐다 — 앞뒤 속도가 다르면 간격이 무너진다.
/// </remarks>
public class StageProgression : MonoBehaviour
{
    /// <summary>점수 구간 하나.</summary>
    [System.Serializable]
    public class Stage
    {
        [Tooltip("이 점수 이상이면 이 단계가 된다. 오름차순으로 넣는다.")]
        public int minScore;

        [Tooltip("이 단계의 배경 스프라이트.")]
        public Sprite background;

        [Tooltip("파이프가 왼쪽으로 흐르는 속도.")]
        [Min(0f)] public float pipeSpeed = 3f;

        [Tooltip("파이프 생성 간격(초). 파이프 사이 거리 = 속도 x 간격. 속도만 올리면 간격이 벌어져 오히려 쉬워진다.")]
        [Min(0.1f)] public float spawnInterval = 2f;

        [Tooltip("배경이 흐르는 속도. 파이프보다 느려야 원근감이 산다.")]
        [Min(0f)] public float backgroundSpeed = 1f;
    }

    [Header("단계")]
    [Tooltip("minScore 오름차순. 첫 요소가 시작 단계다.")]
    [SerializeField] private Stage[] stages;

    [Header("마지막 단계 이후 — 계속 빨라지기")]
    [Tooltip("마지막 단계를 넘긴 뒤 이 점수마다 한 번씩 더 빨라진다. 0이면 마지막 단계 속도로 고정된다.")]
    [SerializeField, Min(0)] private int extraStepScore = 10;

    [Tooltip("한 번에 빨라지는 비율. 0.2 = 20%. 단계 사이 상승폭보다 완만하게 잡아 둔 값이다.")]
    [SerializeField, Min(0f)] private float extraSpeedRate = 0.2f;

    [Tooltip("파이프 속도 상한. 여기 닿으면 더 안 빨라진다.")]
    [SerializeField, Min(0.1f)] private float maxPipeSpeed = 8f;

    [Tooltip("배경 속도 상한.")]
    [SerializeField, Min(0.1f)] private float maxBackgroundSpeed = 3f;

    [Header("연결")]
    [SerializeField] private BackgroundThemeSwitcher backgroundSwitcher;
    [SerializeField] private AutoScroller2D backgroundScroller;
    [SerializeField] private PipeSpawner pipeSpawner;

    /// <summary>지금 적용된 단계. 같은 값을 반복 적용하지 않으려고 들고 있는다.</summary>
    private int current = -1;

    /// <summary>마지막 단계를 넘어선 뒤 몇 번 더 빨라졌는지.</summary>
    private int currentExtraSteps = -1;

    private void OnEnable()
    {
        if (ScoreManager.Instance != null) ScoreManager.Instance.OnScoreChanged += HandleScoreChanged;
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged += HandleStateChanged;

        ResetToFirstStage();
    }

    private void OnDisable()
    {
        if (ScoreManager.Instance != null) ScoreManager.Instance.OnScoreChanged -= HandleScoreChanged;
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= HandleStateChanged;
    }

    private void HandleScoreChanged(int score)
    {
        int index = FindStage(score);
        int steps = CountExtraSteps(score, index);

        if (index == current && steps == currentExtraSteps) return;

        current = index;
        currentExtraSteps = steps;
        Apply(stages[index], steps, immediate: false);
    }

    private void HandleStateChanged(GameState state)
    {
        if (state == GameState.Ready) ResetToFirstStage();
    }

    private void ResetToFirstStage()
    {
        if (stages == null || stages.Length == 0) return;

        current = 0;
        currentExtraSteps = 0;
        Apply(stages[0], 0, immediate: true);
    }

    /// <summary>점수가 도달한 가장 높은 단계를 찾는다.</summary>
    private int FindStage(int score)
    {
        int found = 0;
        for (int i = 0; i < stages.Length; i++)
        {
            if (score >= stages[i].minScore) found = i;
        }

        return found;
    }

    /// <summary>
    /// 마지막 단계에 도달한 뒤 extraStepScore마다 한 번씩 늘어난다.
    /// 중간 단계에서는 항상 0이다 — 다음 단계가 아직 남아 있기 때문이다.
    /// </summary>
    private int CountExtraSteps(int score, int stageIndex)
    {
        if (extraStepScore <= 0) return 0;
        if (stageIndex != stages.Length - 1) return 0;

        int over = score - stages[stageIndex].minScore;
        return over > 0 ? over / extraStepScore : 0;
    }

    private void Apply(Stage stage, int extraSteps, bool immediate)
    {
        if (backgroundSwitcher != null)
        {
            if (immediate) backgroundSwitcher.ApplyThemeNow(stage.background);
            else backgroundSwitcher.SetTheme(stage.background);
        }

        float multiplier = Mathf.Pow(1f + extraSpeedRate, extraSteps);

        float pipeSpeed = Mathf.Min(stage.pipeSpeed * multiplier, maxPipeSpeed);
        float backgroundSpeed = Mathf.Min(stage.backgroundSpeed * multiplier, maxBackgroundSpeed);

        if (backgroundScroller != null) backgroundScroller.Speed = backgroundSpeed;

        if (pipeSpawner != null)
        {
            pipeSpawner.MoveSpeed = pipeSpeed;

            // 상한에 걸린 뒤에도 간격이 계속 줄면 안 되므로, 실제로 적용된 배수로 나눈다.
            // 이렇게 해야 파이프 사이 거리(속도 x 간격)가 그 단계 값 그대로 유지된다.
            float applied = stage.pipeSpeed > 0f ? pipeSpeed / stage.pipeSpeed : 1f;
            pipeSpawner.Interval = stage.spawnInterval / applied;
        }
    }
}

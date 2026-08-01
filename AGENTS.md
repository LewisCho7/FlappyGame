---
created: 2026-07-28
status: 초안
---

# AGENTS 초안

[[보일러플레이트_구성]]의 4·6·8·9번을 종합해서 쓴 `AGENTS.md` 초안.

- **놓을 자리**: `FlappyBoilerplate/AGENTS.md` — `Assets/`와 나란한 프로젝트 루트
- 같은 자리에 `CLAUDE.md`를 두고 **`@AGENTS.md` 한 줄만** 적음. 전문 중복 금지
- 아래 `---` 밑부터 파일 전문. 이 머리말은 빼고 복사할 것
- `_Core` 구현 전에 쓴 것이라 **이 문서가 명세이고 구현이 이걸 따름.** 구현하면서 어긋나는 곳이 나오면
  코드가 아니라 이 문서를 먼저 고칠지 판단할 것

---

# FlappyBoilerplate

Unity 6 / 2D / Input System 패키지 전용 프로젝트.
"코딩 없이 게임 만들기" 스터디의 2주차 보일러플레이트다.

`Assets/_Core/`에는 이미 동작하는 시스템들이 들어 있고, 참가자와 에이전트는 `Assets/_Game/`에서 작업한다.
**이 문서의 목적은 `_Core`에 무엇이 있는지 알려서 같은 것을 다시 만들지 않게 하는 것이다.**

## 폴더 구조

```
AGENTS.md          ← 이 파일. 지시 파일 단일 원본
CLAUDE.md          ← "@AGENTS.md" 한 줄
Assets/
├── _Core/
│   ├── Common/    ← 5주 내내 동일한 공용 시스템
│   │   ├── GameManager.cs
│   │   ├── ScoreManager.cs
│   │   ├── SoundManager.cs
│   │   ├── UIManager.cs
│   │   └── ObjectPool.cs
│   └── Flappy/    ← 이번 게임 전용
│       └── AutoScroller2D.cs
├── _Game/         ← 새 스크립트는 전부 여기
├── Scenes/
├── Sprites/
└── Audio/
```

**`_Core/Flappy/`에 있는 것 = 이 게임에서 쓰라는 것.** 다른 게임용 시스템은 애초에 들어 있지 않다.

## 존재하는 시스템 — 새로 만들지 말 것

| 시스템 | 역할 |
| --- | --- |
| `GameManager` | 게임 상태(Ready/Playing/Paused/GameOver)의 단일 출처. 상태 전환과 알림 |
| `ScoreManager` | 점수·최고점수. 저장까지 포함 |
| `SoundManager` | 효과음/BGM 재생, 볼륨 |
| `UIManager` | 패널 전환·점수 표시. 상태와 점수를 구독해서 **자동으로** 갱신 |
| `ObjectPool` | 오브젝트 재사용. 싱글톤이 아니라 프리팹 종류마다 하나씩 씬에 배치 |
| `AutoScroller2D` | 배경·지면 무한 스크롤 |

"사운드 시스템 만들어줘", "점수 UI 붙여줘" 같은 요청을 받아도 **새로 만들지 말고 위 시스템을 호출한다.**
필요한 기능이 위에 없다고 판단되면 그때는 만들기 전에 사용자에게 먼저 확인한다.

## 호출 규약

아래 공개 시그니처는 고정이다. **이름과 인자 형태를 바꾸지 않는다.** 내부 구현은 자유다.

```csharp
// ── GameManager : 상태의 단일 출처
public enum GameState { Ready, Playing, Paused, GameOver }
public static GameManager Instance { get; }
public GameState State { get; }
public bool IsPlaying { get; }          // State == Playing 축약
public void StartGame();
public void TriggerGameOver();          // 멱등. 이미 GameOver면 즉시 반환
public void PauseGame();                // Time.timeScale = 0
public void ResumeGame();
public void RestartGame();
public event Action<GameState> OnStateChanged;

// ── ScoreManager
public static ScoreManager Instance { get; }
public int Score { get; }
public int BestScore { get; }           // PlayerPrefs 키에 Application.productName 포함
public void AddScore(int amount = 1);
public void SetScore(int value);
public void ResetScore();
public event Action<int> OnScoreChanged;

// ── SoundManager
public static SoundManager Instance { get; }
public void PlaySFX(AudioClip clip, float volume = 1f);
public void PlayBGM(AudioClip clip, bool loop = true);
public void StopBGM();
public void SetSFXVolume(float value);
public void SetBGMVolume(float value);

// ── UIManager : 패널 전환은 OnStateChanged 구독으로 자동. 거의 부를 일이 없다
public static UIManager Instance { get; }
public void ShowMessage(string text, float duration = 1.5f);

// ── ObjectPool : 싱글톤 아님. [SerializeField]로 참조해서 쓴다
public GameObject Get();
public GameObject Get(Vector3 position, Quaternion rotation);
public void Release(GameObject obj);    // 중복 반납은 내부에서 방어됨
public void ReleaseAll();
```

전형적인 사용 형태:

```csharp
void Update()
{
    if (!GameManager.Instance.IsPlaying) return;   // 게임오버 후에도 계속 움직이는 것을 막는다
    // ...
}

// 파이프를 통과했을 때
ScoreManager.Instance.AddScore();                  // UI 숫자는 UIManager가 알아서 바꾼다
SoundManager.Instance.PlaySFX(scoreClip);

// 부딪혔을 때
GameManager.Instance.TriggerGameOver();            // 두 번 불려도 안전하다

// 풀 사용
[SerializeField] private ObjectPool pipePool;
GameObject pipe = pipePool.Get(spawnPos, Quaternion.identity);
pipePool.Release(pipe);
```

### 이 계약에서 지켜야 할 것

1. **`_Core`는 `_Game`을 참조하지 않는다.** `_Game` → `_Core`는 직접 호출, 반대 방향은 이벤트로만.
2. **`ObjectPool`은 `GameObject` 단위로만 다룬다.** `Rigidbody2D` 같은 물리 타입을 시그니처에 등장시키지 않는다
   (3D 주차에서 같은 파일을 그대로 쓰기 위해서다).
3. **풀 반납 조건(화면 밖 판정)은 `_Game` 책임이다.** `_Core`가 정하려 들지 않는다.
4. **`TriggerGameOver()`는 `Time.timeScale`을 건드리지 않는다.** `timeScale` 조작은 `PauseGame()`에서만 한다.
5. **`PlaySFX`는 문자열 키가 아니라 `AudioClip`을 받는다.** 문자열 키 방식으로 바꾸지 않는다.

## 작업 규칙

- **새 스크립트는 `Assets/_Game/`에만 만든다.**
- **`_Core`를 수정해야 한다고 판단되면 수정하지 말고 먼저 설명한다.**
  어떤 파일을 왜 어떻게 바꿔야 하는지 사용자에게 말하고, 승인을 받은 뒤에만 수정한다.
  `_Game`에 우회 코드를 만들어 넘어가는 것보다 이쪽이 낫다.
- **공개 시그니처는 변경하지 않는다.** 내부 구현은 바꿔도 되지만 이름과 인자 형태는 유지한다.
- **입력은 `Keyboard.current`를 쓴다.** 이 프로젝트는 Input System 패키지 전용 모드라
  레거시 `Input.GetKey`는 컴파일은 되고 **실행 시 예외가 난다.**
- **UI 패널 전환 코드를 `_Game`에 만들지 않는다.** `UIManager`가 `OnStateChanged`를 구독해서 처리한다.
  게임 고유 연출이 필요하면 `ShowMessage()`를 쓴다.
- **점수 UI를 직접 갱신하지 않는다.** `AddScore()`만 부르면 `OnScoreChanged`로 화면이 따라온다.
- UI 외형(색·폰트·앵커·위치)은 인스펙터 작업이다. MCP로 `RectTransform`을 조작하려 들지 말고
  **사용자에게 인스펙터에서 직접 조정하라고 안내한다.**

## 알려진 함정 (MCP for Unity)

- **`manage_gameobject create`의 `component_properties`가 조용히 무시된다.**
  `success: true`가 와도 스프라이트가 `null`이고 `BoxCollider2D` 크기가 `0.0001`로 남는다.
  화면에 아무것도 안 보이고 충돌도 안 되는 상태가 된다.
  → 생성 후 `manage_components set_property`로 다시 넣고, **읽어서 확인한다.**
- **만든 뒤에는 반드시 다시 읽어서 확인한다.** "만들었습니다"로 끝내지 않는다.
- **플레이 모드 전환 중에는 리소스 읽기가 stale 값을 반환한다.**
  `editor/state`의 `is_stale`을 보고, `execute_code`로 `EditorApplication.isPlaying`을 교차 확인한다.
- `manage_asset`은 `Folder`/`Material`/`PhysicsMaterial`만 생성한다.
  `PhysicsMaterial2D`처럼 지원되지 않는 것은 `.physicsMaterial2D` YAML을 직접 써서 임포트시킨다.

## 프로젝트 설정 (바꾸지 말 것)

- Active Input Handling: **Input System Package (New)**
- Product Name: **`Flappy`** — `ScoreManager`의 `BestScore` 키에 쓰인다.
  이 값을 바꾸면 최고점수 저장 위치가 달라진다.

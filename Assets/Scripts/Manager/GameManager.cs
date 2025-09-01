using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using TMPro;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoSingleTone<GameManager>
{
    [Header("Manager")]
    public AudioManager audioManager;
    public PoolManager poolManager;
    public ShopManager shopManager;
    public WaveManager waveManager;

    [Header("플레이어 관련")]
    public PlayerAnimation playerAnimation;
    public PlayerController playerController;
    public PlayerDamaged playerDamaged;
    public PlayerDie playerDie;
    public PlayerStats playerStats;
    public JoystickDirectionIndicator joystickDirectionIndicator;
    public ShopEnter shopEnter;

    [Header("카메라 관련")]
    public CameraShake cameraShake;
    public CinemachineCamera cineCamera;

    [Header("몬스터 관련")]
    public EnemyStats enemyStats;
    public DashEnemyStats dashEnemyStats;
    public LongRangeEnemyStats longRangeEnemyStats;
    public PotionEnemyStats potionEnemyStats;

    [Header("패시브 관련")]
    public List<ItemStats> shops;
    public List<ItemStats> buffs;
    public List<ItemStats> debuffs;


    [Header("이벤트 버프 & 디버프 UI")]
    public GameObject eventBuffUI;
    public GameObject eventBuffUICanvas;
    public GameObject eventDebuffUI;
    public GameObject eventDebuffUICanvas;

    [Header("기타 설정")]
    private bool isGameStarted = false;
    private bool isEventBuffUIVisible = false;
    private bool isEventDebuffUIVisible = false;
    public Vector3 gPositionDamping = new Vector3(0.5f, 1000000, 0.5f);
    public Vector3 fixedPosition = new Vector3(-100, 0, 0);

    private enum GameState
    {
        Lobby,
        Game,
        Clear,
        Shop,
        EventBuff,
        EventDebuff,
        End
    }

    private GameState currentState = GameState.Lobby;
    public string CurrentState => currentState.ToString();

    protected new void Awake()
    {
        // VSync 비활성화 (모니터 주사율 영향 제거)
        QualitySettings.vSyncCount = 0;

        // 프레임 고정
        Application.targetFrameRate = 60;

        // 중복 GameManager 방지
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        base.Awake();
    }

    private void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName == "Lobby") // 로비 씬일 경우
        {
            ChangeStateToLobby();
            return;
        }
        else if (sceneName == "InGame") // 게임 씬일 경우
        {
            ChangeStateToGame();
        }

        playerStats.ResetStats();
        enemyStats.ResetStats();
        dashEnemyStats.ResetStats();
        longRangeEnemyStats.ResetStats();
        potionEnemyStats.ResetStats();

    }

    private IEnumerator MoveCoinToPlayer(GameObject coin, float duration)
    {
        float elapsed = 0f;
        Transform coinTransform = coin.transform;
        Vector3 startPos = coinTransform.position;

        while (elapsed < duration)
        {
            if (GameManager.Instance.playerController != null)
            {
                Vector3 playerPos = GameManager.Instance.playerController.transform.position;
                coinTransform.position = Vector3.Lerp(startPos, playerPos, elapsed / duration);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 마지막에 확실히 플레이어 위치로 이동
        coinTransform.position = GameManager.Instance.playerController.transform.position;

        // 코인 풀로 반환
        PoolManager.Instance.ReturnToPool(coin);
    }

    private void AutoCollectItems() // 코인 및 아이템 자동 수집 처리 함수
    {
        GameObject[] coins = GameObject.FindGameObjectsWithTag("Coin");
        foreach (GameObject coin in coins)
        {
            StartCoroutine(MoveCoinToPlayer(coin, 0.5f));
        }

        GameObject[] zacs = GameObject.FindGameObjectsWithTag("HPPotion");
        foreach (GameObject zac in zacs)
        {
            StartCoroutine(MoveCoinToPlayer(zac, 0.5f));
        }
    }




    public void ChangeStateToLobby()
    {
        currentState = GameState.Lobby;
        Debug.Log("상태: Lobby - 게임 대기 중");
    }

    public void ChangeStateToGame()
    {
        currentState = GameState.Game;
        Debug.Log("상태: Game - 웨이브 진행 중");

        GameObject[] zaces = GameObject.FindGameObjectsWithTag("HPPotion");
        foreach (GameObject zac in zaces)
        {
            PoolManager.Instance.ReturnToPool(zac);
        }

        joystickDirectionIndicator.StartRollingLoop();

        waveManager.StartSpawnLoop();

        if (!isGameStarted)
        {
            isGameStarted = true;
            // 첫 웨이브 시작 시 NavMesh 베이크
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = new Vector3(-9, 0, 0);
                playerController.canMove = true;
            }
        }

        if (cineCamera != null)
        {
            var followComponent = cineCamera.GetComponent<CinemachineFollow>();
            if (followComponent != null)
            {
                followComponent.TrackerSettings.PositionDamping = gPositionDamping;
            }
        }
    }


    public void ChangeStateToShop()
    {
        currentState = GameState.Shop;
        Debug.Log("상태: Shop - 상점 상태");
        playerController.canMove = true;

        //    if (cineCamera != null)
        //    {
        //        cineCamera.Follow = null;
        //        cineCamera.LookAt = null;

        //        // 위치 고정
        //        cineCamera.transform.position = fixedPosition;
        //    }
    }

    public void ChangeStateToEventBuff()
    {
        currentState = GameState.EventBuff;
        Debug.Log("상태: EventBuff - 이벤트 버프 상태");
        playerController.canMove = false;

        if (eventBuffUI != null)
        {
            CanvasGroup cg = eventBuffUI.GetComponent<CanvasGroup>();
            RectTransform rt = eventBuffUI.GetComponent<RectTransform>();

            if (cg != null && rt != null)
            {
                // UI를 처음에는 안 보이게, 인터랙션 막고, 위치를 위로 이동시킴
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;

                // sortingOrder 최상단으로 설정
                Canvas canvas = eventBuffUICanvas.GetComponent<Canvas>();
                if (canvas != null)
                    canvas.sortingOrder = 10;

                // 페이드 인과 아래 방향(원래 위치)으로 이동 애니메이션 실행
                cg.DOFade(1f, 0.7f).OnComplete(() =>
                {
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                    isEventBuffUIVisible = true;
                });

                rt.DOAnchorPosY(0, 0.7f).SetEase(Ease.OutCubic);
            }
        }
        else
        {
            Debug.LogWarning("[GameManager] 이벤트 버프 UI가 할당되지 않았습니다.");
        }
    }

    public void ChangeStateToEventDebuff()
    {
        currentState = GameState.EventDebuff;
        Debug.Log("상태: EventDebuff - 이벤트 디버프 상태");
        playerController.canMove = false;
        if (eventDebuffUI != null)
        {
            CanvasGroup cg = eventDebuffUI.GetComponent<CanvasGroup>();
            RectTransform rt = eventDebuffUI.GetComponent<RectTransform>();

            if (cg != null && rt != null)
            {
                // UI를 처음에는 안 보이게, 인터랙션 막고, 위치를 위로 이동시킴
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;

                // sortingOrder 최상단으로 설정
                Canvas canvas = eventDebuffUICanvas.GetComponent<Canvas>();
                if (canvas != null)
                    canvas.sortingOrder = 10;

                // 페이드 인과 아래 방향(원래 위치)으로 이동 애니메이션 실행
                cg.DOFade(1f, 0.7f).OnComplete(() =>
                {
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                    isEventDebuffUIVisible = true;
                });

                rt.DOAnchorPosY(0, 0.7f).SetEase(Ease.OutCubic);
            }
        }
        else
        {
            Debug.LogWarning("[GameManager] 이벤트 버프 UI가 할당되지 않았습니다.");
        }
    }

    public void ChangeStateToClear()
    {
        currentState = GameState.Clear;
        Debug.Log("상태: Clear - 웨이브 클리어");

        waveManager.StopSpawnLoop();

        isGameStarted = false;

        // 코인 자동 수집
        AutoCollectItems();
    }


    public void ChangeStateToEnd()
    {
        currentState = GameState.End;
        Debug.Log("상태: End - 게임 오버");
    }


    public bool IsLobby() => currentState == GameState.Lobby;
    public bool IsGame() => currentState == GameState.Game;
    public bool IsShop() => currentState == GameState.Shop;
    public bool IsEventBuff() => currentState == GameState.EventBuff;

    public bool IsEventDebuff() => currentState == GameState.EventDebuff;
    public bool IsClear() => currentState == GameState.Clear;
    public bool IsEnd() => currentState == GameState.End;
}
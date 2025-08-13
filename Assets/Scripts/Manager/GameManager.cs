using DG.Tweening;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public class GameManager : MonoSingleTone<GameManager>
{
    public CameraShake cameraShake;
    public CinemachineCamera cineCamera;
    public PlayerAnimation playerAnimation;
    public PlayerController playerController;
    public PlayerDamaged playerDamaged;
    public PlayerDie playerDie;
    public PlayerStats playerStats;
    public EnemyStats enemyStats;
    public DashEnemyStats dashEnemyStats;
    public LongRangeEnemyStats longRangeEnemyStats;
    public PotionEnemyStats potionEnemyStats;
    public EnemySpawner enemySpawner;
    public ItemStats itemStats1;
    public ItemStats itemStats2;
    public ItemStats itemStats3;
    public ItemStats itemStats4;
    public ItemStats itemStats5;
    public ItemStats itemStats6;
    public ItemStats itemStats7;
    public ItemStats itemStats8;
    public ItemStats itemStats9;
    public ItemStats itemStats10;
    public Enemy enemy;
    public DashEnemy dashEnemy;
    public LongRangeEnemy longRangeEnemy;
    public PotionEnemy potionEnemy;
    public Timer timer;
    public AudioManager audioManager;
    public PoolManager poolManager;
    public ShopManager shopManager;
    public WaveManager waveManager;

    public ShopEnter shopEnter;
    //public DialogManager dialogManager;
    public EnemyHP enemyHP;
    public DiceAnimation diceAnimation;
    public JoystickDirectionIndicator joystickDirectionIndicator;
    public ZacSkill zacSkill;

    private bool isGameStarted = false;

    public Vector3 gPositionDamping = new Vector3(0.5f, 1000000, 0.5f);
    public Vector3 fixedPosition = new Vector3(-100, 0, 0);

    private enum GameState
    {
        Lobby,
        Game,
        Shop,
        Clear,
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

        if (timer == null)
        {
            timer = Object.FindFirstObjectByType<Timer>();
            if (timer == null)
                Debug.LogWarning("Timer 컴포넌트를 씬에서 찾지 못했습니다.");
        }
    }

    private void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName == "Lobby") // 로비 씬일 경우
        {
            ChangeStateToIdle();
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

        // 예: 골드 추가 처리
        // GameManager.Instance.playerStats.AddGold(1);
    }

    private void AutoCollectCoins()
    {
        GameObject[] coins = GameObject.FindGameObjectsWithTag("Coin");

        if (coins.Length == 0) return;

        foreach (GameObject coin in coins)
        {
            StartCoroutine(MoveCoinToPlayer(coin, 0.5f));
        }
    }



    public void ChangeStateToIdle()
    {
        currentState = GameState.Lobby;
        Debug.Log("상태: Lobby - 게임 대기 중");
    }

    public void ChangeStateToGame()
    {
        currentState = GameState.Game;
        Debug.Log("상태: Game - 웨이브 진행 중");

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

        GameObject[] zaces = GameObject.FindGameObjectsWithTag("HPPotion");
        foreach (GameObject zac in zaces)
        {
            PoolManager.Instance.ReturnToPool(zac);
        }
        //    if (cineCamera != null)
        //    {
        //        cineCamera.Follow = null;
        //        cineCamera.LookAt = null;

        //        // 위치 고정
        //        cineCamera.transform.position = fixedPosition;
        //    }
    }

    public void ChangeStateToClear()
    {
        currentState = GameState.Clear;
        Debug.Log("상태: Clear - 웨이브 클리어");

        waveManager.StopSpawnLoop();

        isGameStarted = false;

        // 코인 자동 수집
        AutoCollectCoins();
    }


    public void ChangeStateToEnd()
    {
        currentState = GameState.End;
        Debug.Log("상태: End - 게임 오버");
    }


    public bool IsIdle() => currentState == GameState.Lobby;
    public bool IsGame() => currentState == GameState.Game;
    public bool IsShop() => currentState == GameState.Shop;
    public bool IsClear() => currentState == GameState.Clear;
    public bool IsEnd() => currentState == GameState.End;
}
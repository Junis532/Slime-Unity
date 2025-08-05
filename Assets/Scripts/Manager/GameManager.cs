using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoSingleTone<GameManager>
{
    public CameraShake cameraShake;
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
    public DialogManager dialogManager;
    public EnemyHP enemyHP;
    public DiceAnimation diceAnimation;
    public JoystickDirectionIndicator joystickDirectionIndicator;
    //public FireballProjectile fireballProjectile;
    //public LightningDamage lightningDamage;
    public ZacSkill zacSkill;

    [Header("UI")]
    public GameObject shopUI;

    [Header("상점 패널")]
    public RectTransform shopPanel;

    [Header("게임 시간 설정")]
    public float gameTime;

    private bool isGameStarted = false;

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


        if (shopUI != null)
        {
            CanvasGroup canvasGroup = shopPanel.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            shopUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (currentState == GameState.Game && timer != null && timer.timerRunning)
        {
            if (timer.timeRemaining > 0)
            {
                timer.timeRemaining -= Time.deltaTime;
                timer.UpdateTimerDisplay();
            }
            else
            {
                timer.timeRemaining = 0;
                timer.timerRunning = false;
                timer.UpdateTimerDisplay();

                // 적 죽음 및 상점 진입 코루틴 시작
                StartCoroutine(WaitForEnemiesDieAndGoShop());

            }
        }
    }

    private IEnumerator WaitForEnemiesDieAndGoShop()
    {
        // 모든 적들에게 죽으라고 명령
        string[] enemyTags = { "Enemy", "DashEnemy", "LongRangeEnemy", "PotionEnemy" };

        foreach (string tag in enemyTags)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject enemyObject in enemies)
            {
                EnemiesDie enemiesDie = enemyObject.GetComponent<EnemiesDie>();
                if (enemiesDie != null)
                {
                    enemiesDie.Die();
                }
            }
        }

        // 코인, 총알, 스킬 삭제 부분 그대로
        GameObject[] coins = GameObject.FindGameObjectsWithTag("Coin");
        foreach (GameObject coin in coins)
        {
            PoolManager.Instance.ReturnToPool(coin);
        }

        GameObject[] bullets = GameObject.FindGameObjectsWithTag("Bullet");
        foreach (GameObject bullet in bullets)
        {
            PoolManager.Instance.ReturnToPool(bullet);
        }

        GameObject[] enemyHPs = GameObject.FindGameObjectsWithTag("HP");
        foreach (GameObject enemyHP in enemyHPs)
        {
            PoolManager.Instance.ReturnToPool(enemyHP);
        }

        GameObject[] skills = GameObject.FindGameObjectsWithTag("Skill");
        foreach (GameObject skill in skills)
        {
            Destroy(skill);
        }


        // 기다리지 않고 바로 상점 상태로 전환
        ChangeStateToClear();
        //ChangeStateToClear();
        yield break;
    }


    public void ChangeStateToIdle()
    {
        currentState = GameState.Lobby;
        Debug.Log("상태: Lobby - 게임 대기 중");
        Time.timeScale = 1f;
        if (shopUI != null) shopUI.SetActive(false);
    }

    public void ChangeStateToGame()
    {
        currentState = GameState.Game;
        Debug.Log("상태: Game - 웨이브 진행 중");

        if (timer != null)
        {
            timer.ResetTimer(gameTime);
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
            }
        }
    }


    public void ChangeStateToShop()
    {
        currentState = GameState.Shop;
        Debug.Log("상태: Shop - 상점 상태");
        DialogManager.Instance.StartShopDialog();

        waveManager.StopSpawnLoop();

        shopManager.FirstRerollItems();

        if (shopUI != null)
        {
            shopUI.SetActive(true);
        }

        if (shopPanel != null)
        {

            CanvasGroup canvasGroup = shopPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.DOFade(1f, 0.7f);  // 0f = 완전 투명, 0.5초 동안
            }
            shopPanel.DOAnchorPosY(0f, 0.7f);
            playerController.canMove = false;
        }
    }

    public void ChangeStateToClear()
    {
        currentState = GameState.Clear;
        Debug.Log("상태: Clear - 웨이브 클리어");

        waveManager.StopSpawnLoop();

        Time.timeScale = 1f;
        if (shopUI != null) shopUI.SetActive(false);

        isGameStarted = false;
    }

    public void ChangeStateToEnd()
    {
        currentState = GameState.End;
        Debug.Log("상태: End - 게임 오버");
        Time.timeScale = 0f;
        if (shopUI != null) shopUI.SetActive(false);
    }


    public bool IsIdle() => currentState == GameState.Lobby;
    public bool IsGame() => currentState == GameState.Game;
    public bool IsShop() => currentState == GameState.Shop;
    public bool IsClear() => currentState == GameState.Clear;
    public bool IsEnd() => currentState == GameState.End;
}
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DashEnemy : EnemyBase
{
    private bool isLive = true;
    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;
    private GameObject player;
    NavMeshAgent navMesh;

    [Header("대시 관련")]
    public float dashSpeed = 25f;          // 돌진 속도
    public float dashDuration = 0.25f;     // 돌진 유지 시간
    public float waitAfterDash = 1.0f;     // 돌진 후 대기 시간

    private bool isDashing = false;
    private Vector2 dashDirection;

    [Header("잔상 관련")]
    public GameObject afterImagePrefab;
    public float afterImageSpawnInterval = 0.05f;
    public float afterImageFadeDuration = 0.3f;
    public float afterImageLifeTime = 0.5f;
    public int maxAfterImageCount = 10;

    private Coroutine afterImageCoroutine;
    private readonly List<GameObject> afterImages = new();

    private void Start()
    {
        navMesh = GetComponent<NavMeshAgent>();
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<EnemyAnimation>();
        player = GameObject.FindWithTag("Player");

        navMesh.updateRotation = false;
        navMesh.updateUpAxis = false;
        navMesh.speed = speed;

        if (player != null)
            StartCoroutine(DashLoop());
    }

    /// <summary>
    /// 대기 → 돌진 → 대기 → 반복
    /// </summary>
    private IEnumerator DashLoop()
    {
        transform.rotation = Quaternion.Euler(0, 0, 0);
        while (isLive)
        {
            Debug.Log(transform.rotation);
            // 1️⃣ 돌진 전 대기
            yield return new WaitForSeconds(waitAfterDash);

            // 2️⃣ 플레이어 방향 계산
            if (player != null)
            {
                dashDirection = (player.transform.position - transform.position).normalized;
                FlipSprite(dashDirection.x);
            }

            // 3️⃣ 돌진 시작
            isDashing = true;
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Move);

            if (afterImageCoroutine != null)
                StopCoroutine(afterImageCoroutine);
            afterImageCoroutine = StartCoroutine(SpawnAfterImages());

            float elapsed = 0f;
            while (elapsed < dashDuration)
            {
                transform.Translate(dashDirection * dashSpeed * Time.deltaTime, Space.World);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 4️⃣ 돌진 종료
            isDashing = false;
            if (afterImageCoroutine != null)
            {
                StopCoroutine(afterImageCoroutine);
                afterImageCoroutine = null;
            }
        }
    }


    private void FlipSprite(float dirX)
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (dirX < 0 ? -1 : 1);
        transform.localScale = scale;
    }

    // ────────── 잔상 관련 ──────────
    private IEnumerator SpawnAfterImages()
    {
        while (isDashing)
        {
            CreateAfterImage();
            yield return new WaitForSeconds(afterImageSpawnInterval);
        }
    }

    private void CreateAfterImage()
    {
        GameObject afterImage = new GameObject("AfterImage");
        afterImage.transform.position = transform.position;
        afterImage.transform.rotation = transform.rotation;
        afterImage.transform.localScale = transform.localScale;

        SpriteRenderer sr = afterImage.AddComponent<SpriteRenderer>();
        SpriteRenderer enemySR = GetComponent<SpriteRenderer>();

        if (enemySR != null)
        {
            sr.sprite = enemySR.sprite;
            sr.flipX = enemySR.flipX;
            Color c = enemySR.color;
            c.a = 0.5f; // 반투명
            sr.color = c;

            sr.sortingLayerID = enemySR.sortingLayerID;
            sr.sortingOrder = enemySR.sortingOrder - 1;
        }

        afterImages.Add(afterImage);
        if (afterImages.Count > maxAfterImageCount)
        {
            Destroy(afterImages[0]);
            afterImages.RemoveAt(0);
        }

        sr.DOFade(0f, afterImageFadeDuration)
          .SetDelay(afterImageLifeTime - afterImageFadeDuration)
          .OnComplete(() =>
          {
              afterImages.Remove(afterImage);
              Destroy(afterImage);
          });
    }
}

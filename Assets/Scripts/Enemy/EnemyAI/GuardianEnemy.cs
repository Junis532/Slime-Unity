using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class GuardianEnemy : EnemyBase
{
    private bool isLive = true;

    private SpriteRenderer spriter;
    private EnemyAnimation enemyAnimation;
    private NavMeshAgent agent;

    public float fireRange = 5f;
    private GameObject player;
    private LineRenderer laserLineRenderer;

    [Header("레이저 선 설정")]
    public Color laserColor = Color.red;
    public float laserWidth = 0.1f;

    private bool isDamaging = false;

    void Start()
    {
        spriter = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<EnemyAnimation>();
        agent = GetComponent<NavMeshAgent>();

        originalSpeed = GameManager.Instance.enemyStats.speed;
        speed = originalSpeed;

        player = GameObject.FindWithTag("Player");

        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = speed;

        // 레이저 세팅
        laserLineRenderer = gameObject.AddComponent<LineRenderer>();
        laserLineRenderer.positionCount = 2;
        laserLineRenderer.enabled = false;
        laserLineRenderer.startWidth = laserWidth;
        laserLineRenderer.endWidth = laserWidth;

        Material laserMat = new Material(Shader.Find("Unlit/GlowLine"));
        laserMat.SetColor("_Color", laserColor);
        laserMat.SetColor("_EmissionColor", laserColor * 5f);
        laserLineRenderer.material = laserMat;

        laserLineRenderer.startColor = laserColor;
        laserLineRenderer.endColor = laserColor;
        laserLineRenderer.sortingLayerName = "Default";
        laserLineRenderer.sortingOrder = 10;
    }

    void Update()
    {
        if (!isLive || player == null) return;

        // ------------ 추적 이동 ------------
        Vector2 currentPos = transform.position;
        Vector2 dirToPlayer = ((Vector2)player.transform.position - currentPos).normalized;
        Vector2 targetPos = currentPos + dirToPlayer * 1.5f;

        agent.isStopped = false;
        agent.SetDestination(targetPos);

        // ------------ 애니메이션 ------------
        Vector2 velocity = agent.velocity;
        if (velocity.magnitude > 0.1f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (velocity.x < 0 ? -1 : 1);
            transform.localScale = scale;

            enemyAnimation.PlayAnimation(EnemyAnimation.State.Move);
        }
        else
        {
            enemyAnimation.PlayAnimation(EnemyAnimation.State.Idle);
        }

        // ------------ 레이저 공격 ------------
        float distance = Vector2.Distance(player.transform.position, transform.position);
        if (distance <= fireRange)
        {
            FireLaser();

            if (!isDamaging)
            {
                isDamaging = true;
                StartCoroutine(DealDamageRoutine());
            }
        }
        else
        {
            StopLaser();
        }
    }

    private void FireLaser()
    {
        laserLineRenderer.enabled = true;
        Vector3 startPos = transform.position;
        Vector3 endPos = player.transform.position;
        startPos.z = -1f;
        endPos.z = -1f;
        laserLineRenderer.SetPosition(0, startPos);
        laserLineRenderer.SetPosition(1, endPos);
    }

    private void StopLaser()
    {
        if (laserLineRenderer.enabled)
            laserLineRenderer.enabled = false;

        if (isDamaging)
        {
            isDamaging = false;
            StopAllCoroutines();
        }
    }

    private IEnumerator DealDamageRoutine()
    {
        while (isDamaging)
        {
            if (player == null) yield break;
            yield return new WaitForSeconds(1f);

            var playerStats = GameManager.Instance.playerStats;
            playerStats.currentHP -= 1;
            GameManager.Instance.playerDamaged.PlayDamageEffect();

            if (playerStats.currentHP <= 0)
            {
                playerStats.currentHP = 0;
                // 사망 처리
            }
        }
    }
}

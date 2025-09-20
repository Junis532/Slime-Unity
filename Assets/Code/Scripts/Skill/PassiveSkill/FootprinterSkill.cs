using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FootprinterSkill : MonoBehaviour
{
    public GameObject footprinterPrefab;
    public GameObject poisonEffectPrefab;
    public float skillDuration = 3f;
    public float fadeSpeed = 0.5f;
    public float footprinterInterval = 0.3f;

    private float lastFootprinterTime = 0f;
    private SpriteRenderer spriteRenderer;
    private Color initialColor;

    private bool isPoisonGasActive = false;
    public bool isFootprint = false;

    private static List<GameObject> footprintList = new List<GameObject>();

    // 자동 토글 기능 변수
    public bool autoTogglePoisonGas = false;   // 자동 켜기/끄기 활성화 여부
    public float toggleInterval = 15f;          // 토글 주기 (초)

    private Coroutine autoToggleCoroutine;

    public static Vector3 OldestFootprintPosition
    {
        get
        {
            if (footprintList.Count > 0 && footprintList[0] != null)
                return footprintList[0].transform.position;
            return Vector3.zero;
        }
    }

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            initialColor = spriteRenderer.color;
    }

    void OnEnable()
    {
        if (autoTogglePoisonGas)
        {
            autoToggleCoroutine = StartCoroutine(AutoTogglePoisonGasRoutine());
        }
    }

    void OnDisable()
    {
        if (autoToggleCoroutine != null)
        {
            StopCoroutine(autoToggleCoroutine);
            autoToggleCoroutine = null;
        }
    }

    void Update()
    {
        if (!isFootprint)
        {
            if (Time.time - lastFootprinterTime >= footprinterInterval)
            {
                CreateFootprint();
                lastFootprinterTime = Time.time;
            }
        }
        else
        {
            // 점점 사라지게 하고 반환
            if (spriteRenderer != null)
            {
                if (spriteRenderer.color.a > 0)
                {
                    float alpha = spriteRenderer.color.a - fadeSpeed * Time.deltaTime;
                    spriteRenderer.color = new Color(initialColor.r, initialColor.g, initialColor.b, Mathf.Max(alpha, 0f));
                }
                else
                {
                    PoolManager.Instance.ReturnToPool(gameObject); // Destroy → 풀반납
                }
            }
        }
    }

    void CreateFootprint()
    {
        // 풀매니저 활용
        GameObject footprint = PoolManager.Instance.SpawnFromPool(footprinterPrefab.name, transform.position, Quaternion.identity);
        if (footprint == null) return;

        FootprinterSkill footprintScript = footprint.GetComponent<FootprinterSkill>();
        if (footprintScript != null)
        {
            footprintScript.isFootprint = true;
        }

        footprintList.Add(footprint);

        SpriteRenderer footprintRenderer = footprint.GetComponent<SpriteRenderer>();
        PoisonDamage poison = footprint.GetComponent<PoisonDamage>();
        if (poison != null) poison.Init();
        if (footprintRenderer != null) footprintRenderer.color = initialColor;

        if (isPoisonGasActive)
        {
            Collider2D col = footprint.GetComponent<Collider2D>();
            if (col != null) col.enabled = true;

            if (poisonEffectPrefab != null)
            {
                // 이펙트도 풀링 권장!
                GameObject effect = PoolManager.Instance.SpawnFromPool(poisonEffectPrefab.name, footprint.transform.position, Quaternion.identity);
                if (effect != null)
                    effect.transform.SetParent(footprint.transform, false); // 별도로 부모설정

                effect.transform.localPosition = Vector3.zero;
                StartCoroutine(DestroyEffectAfterDelay(effect, 10f));
            }
            StartCoroutine(DisablePoisonEffect(footprint, 10f));
        }

        StartCoroutine(DestroyFootprintAfterDelay(footprint, skillDuration));
    }

    IEnumerator DestroyFootprintAfterDelay(GameObject footprint, float delay)
    {
        yield return new WaitForSeconds(delay);
        footprintList.Remove(footprint);
        PoolManager.Instance.ReturnToPool(footprint); // Destroy → ReturnToPool
    }

    IEnumerator DisablePoisonEffect(GameObject footprint, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (footprint != null && footprint.TryGetComponent(out Collider2D col))
        {
            col.enabled = false;
        }
    }

    IEnumerator DestroyEffectAfterDelay(GameObject effect, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (effect != null) PoolManager.Instance.ReturnToPool(effect); // Destroy → ReturnToPool
    }

    public void ActivatePoisonGasMode(float duration)
    {
        StartCoroutine(PoisonGasRoutine(duration));
    }

    IEnumerator PoisonGasRoutine(float duration)
    {
        isPoisonGasActive = true;
        yield return new WaitForSeconds(duration);
        isPoisonGasActive = false;
    }

    IEnumerator AutoTogglePoisonGasRoutine()
    {
        while (true)
        {
            ActivatePoisonGasMode(toggleInterval / 3f);  // 예: 활성화는 토글 주기 1/3 만큼 유지
            yield return new WaitForSeconds(toggleInterval);
        }
    }
}

using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BulletVFXDriver : MonoBehaviour
{
    [Header("Auto Dissolve on Destroy")]
    public bool dissolveOnDestroy = true;
    public float dissolveDuration = 0.18f;

    [Header("Optional Lifetime (sec)")]
    public float lifeTime = -1f; // <=0 이면 미사용

    private SpriteRenderer sr;
    private MaterialPropertyBlock mpb;
    private float spawnTime;
    private float dissolveT = 0f;
    private bool isDissolving = false;

    static readonly int AgeID = Shader.PropertyToID("_Age");
    static readonly int DissolveID = Shader.PropertyToID("_Dissolve");

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        mpb = new MaterialPropertyBlock();
        spawnTime = Time.time;
        UpdateMPB(0f, 0f);
    }

    void Update()
    {
        float age = Time.time - spawnTime;

        if (lifeTime > 0f && age >= lifeTime && !isDissolving && dissolveOnDestroy)
        {
            // 수명 끝 → 디졸브 시작
            isDissolving = true;
            dissolveT = 0f;
        }

        if (isDissolving)
        {
            dissolveT += Time.deltaTime / Mathf.Max(0.0001f, dissolveDuration);
            float d = Mathf.Clamp01(dissolveT);
            UpdateMPB(age, d);

            if (d >= 1f)
            {
                Destroy(gameObject);
                return;
            }
        }
        else
        {
            UpdateMPB(age, 0f);
        }
    }

    private void UpdateMPB(float age, float dissolve)
    {
        sr.GetPropertyBlock(mpb);
        mpb.SetFloat(AgeID, age);
        mpb.SetFloat(DissolveID, dissolve);
        sr.SetPropertyBlock(mpb);
    }

    // 외부에서 맞거나 충돌 시 강제로 디졸브 종료 시킬 때 호출
    public void TriggerDissolveAndDie(float duration = 0.18f)
    {
        if (isDissolving) return;
        dissolveDuration = duration;
        isDissolving = true;
        dissolveT = 0f;
    }
}

using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("배경음악")]
    public AudioSource bgmSource;

    [Header("효과음")]
    public AudioSource sfxSource;

    [Header("효과음 리스트")]
    public AudioClip clickSound;
    public AudioClip attackSound;
    public AudioClip hitSound;
    public AudioClip arrowHit;
    public AudioClip arrowSound;
    public AudioClip arrowWall;
    public AudioClip coin;
    public AudioClip jumpSound;
    public AudioClip land;
    public AudioClip portalSpawnSound;

    void Awake()
    {
        // 싱글톤 패턴 적용
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시 유지
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (bgmSource == null || clip == null) return;

        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null)
            bgmSource.Stop();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
            sfxSource.PlayOneShot(clip);
    }

    // 편의 함수 예시
    public void PlayClickSound() => PlaySFX(clickSound);
    public void PlayAttackSound() => PlaySFX(attackSound);
    public void PlayHitSound() => PlaySFX(hitSound);

    public void PlayArrowHitSound() => PlaySFX(arrowHit);
    public void PlayArrowSound() => PlaySFX(arrowSound);

    public void PlayArrowWallSound() => PlaySFX(arrowWall);

    public void PlayCoinSound() => PlaySFX(coin);

    public void PlayJumpSound() => PlaySFX(jumpSound);

    public void PlayLandSound() => PlaySFX(land);

    public void PlayPortalSpawnSound() => PlaySFX(portalSpawnSound);
}

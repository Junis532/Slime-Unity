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
    public AudioClip dash;
    public AudioClip jumpSound;
    public AudioClip land;
    public AudioClip stoneFalling;

    [Header("터렛 발사")]
    public AudioClip turretShooting;

    [Header("보스 효과음 리스트")]
    public AudioClip bossSword;
    public AudioClip bossSkill1;
    public AudioClip bossSkill2;
    public AudioClip bossSkill3;
    public AudioClip bossSkill3Fire;

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

    // 🎵 배경음 재생
    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (bgmSource == null || clip == null) return;

        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.Play();
    }

    // 🎵 배경음 정지
    public void StopBGM()
    {
        if (bgmSource != null)
            bgmSource.Stop();
    }

    // 🎵 효과음 재생 (볼륨 조절 가능)
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (sfxSource != null && clip != null)
        {
            // sfxSource.volume * volumeScale 로 최종 볼륨 결정
            sfxSource.PlayOneShot(clip, volume);
        }
    }

    // ────────────── 편의 함수 ──────────────
    public void PlayClickSound(float volume = 1f) => PlaySFX(clickSound, volume);

    // 🔊 공격 사운드 (기본 1.3배 크게)
    public void PlayAttackSound(float volume = 1.3f) => PlaySFX(attackSound, volume);

    // 💥 피격 사운드
    public void PlayHitSound(float volume = 1f) => PlaySFX(hitSound, volume);

    // 🏹 화살 관련
    public void PlayArrowHitSound(float volume = 1f) => PlaySFX(arrowHit, volume);
    public void PlayArrowSound(float volume = 1.1f) => PlaySFX(arrowSound, volume);
    public void PlayArrowWallSound(float volume = 0.9f) => PlaySFX(arrowWall, volume);

    // 💰 코인 사운드
    public void PlayCoinSound(float volume = 0.8f) => PlaySFX(coin, volume);

    // 🦘 점프/착지
    public void PlayJumpSound(float volume = 1f) => PlaySFX(jumpSound, volume);
    public void PlayLandSound(float volume = 0.8f) => PlaySFX(land, volume);

    // 🌀 포탈 생성
    public void StoneFalling(float volume = 1.2f) => PlaySFX(stoneFalling, volume);

    public void PlayDashSound(float volume = 1f) => PlaySFX(dash, volume);

    // 터렛 공격
    public void TurretShootingSound(float volume = 1f) => PlaySFX(turretShooting, volume);

    // 보스 스킬
    public void PlayBossSwordSound(float volume = 1f) => PlaySFX(bossSword, volume);
    public void PlayBossSkill1Sound(float volume = 1f) => PlaySFX(bossSkill1, volume);
    public void PlayBossSkill2Sound(float volume = 1f) => PlaySFX(bossSkill2, volume);
    public void PlayBossSkill3Sound(float volume = 1f) => PlaySFX(bossSkill3, volume);
    public void PlayBossSkill3FireSound(float volume = 1f) => PlaySFX(bossSkill3Fire, volume);
}

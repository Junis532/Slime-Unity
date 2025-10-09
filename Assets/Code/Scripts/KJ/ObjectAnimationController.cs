using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class FrameEvent
{
    public int frameIndex;
    public string eventName;
    public string parameter;
}

[System.Serializable]
public class SimpleAnimationClip
{
    public string name;
    public Sprite[] frames;
    [Range(0.1f, 60f)]
    public float frameRate = 12f;
    public bool loop = true;
    public FrameEvent[] frameEvents;
}

public class ObjectAnimationController : MonoBehaviour
{
    [Header("Animation Settings")]
    public SimpleAnimationClip[] animationClips;
    public string defaultAnimation = "Idle";
    public bool playOnStart = true;
    
    // 디버깅용 속성
    [Header("Debug")]
    public bool debugMode = true;
    
    [Header("Components")]
    public SpriteRenderer spriteRenderer;
    public Image uiImage;
    
    private Dictionary<string, SimpleAnimationClip> animationDict;
    private SimpleAnimationClip currentClip;
    private int currentFrame = 0;
    private float frameTimer = 0f;
    private bool isPlaying = false;
    
    // 간단한 이벤트
    public System.Action<string> OnAnimationComplete;
    
    // 프레임 이벤트
    public System.Action<string, string> OnFrameEvent; // eventName, parameter
    
    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            
        if (uiImage == null)
            uiImage = GetComponentInChildren<Image>();
            
        InitializeAnimations();
    }
    
    private void Start()
    {
        if (playOnStart && !string.IsNullOrEmpty(defaultAnimation))
        {
            PlayAnimation(defaultAnimation);
        }
    }
    
    private void Update()
    {
        if (!isPlaying || currentClip == null)
        {
            return;
        }
            
        frameTimer += Time.deltaTime;
        
        if (frameTimer >= 1f / currentClip.frameRate)
        {
            frameTimer = 0f;
            currentFrame++;
            
            if (currentFrame >= currentClip.frames.Length)
            {
                if (currentClip.loop)
                {
                    currentFrame = 0;
                }
                else
                {
                    // 루프가 아닌 애니메이션이 완료되면 완료 이벤트 발생
                    string completedAnimName = currentClip.name;
                    isPlaying = false;
                    
                    // 이벤트 호출
                    if (OnAnimationComplete != null)
                    {
                        OnAnimationComplete.Invoke(completedAnimName);
                    }
                    return;
                }
            }
            
            UpdateSprite();
            ProcessFrameEvents(); // 프레임 이벤트 처리
        }
    }
    
    private void InitializeAnimations()
    {
        animationDict = new Dictionary<string, SimpleAnimationClip>();
        
        foreach (var clip in animationClips)
        {
            if (!string.IsNullOrEmpty(clip.name) && clip.frames != null && clip.frames.Length > 0)
            {
                animationDict[clip.name] = clip;
            }
        }
    }
    
    private void UpdateSprite()
    {
        if (currentClip != null && currentFrame < currentClip.frames.Length)
        {
            Sprite currentSprite = currentClip.frames[currentFrame];
            
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = currentSprite;
            }
            
            if (uiImage != null)
            {
                uiImage.sprite = currentSprite;
            }
        }
    }
    
    public void PlayAnimation(string animationName)
    {
        // animationDict가 초기화되지 않은 경우 처리
        if (animationDict == null)
        {
            Debug.LogWarning($"ObjectAnimationController: animationDict가 초기화되지 않았습니다. {gameObject.name}");
            InitializeAnimations(); // 강제 초기화 시도
            if (animationDict == null)
            {
                Debug.LogError($"ObjectAnimationController: animationDict 초기화 실패. {gameObject.name}");
                return;
            }
        }
        
        if (!animationDict.ContainsKey(animationName))
        {
            Debug.LogWarning($"Animation '{animationName}' not found in {gameObject.name}!");
            return;
        }
        
        SimpleAnimationClip newClip = animationDict[animationName];
        
        // 같은 애니메이션이 이미 재생 중이면 무시 (이름으로 비교)
        if (currentClip != null && currentClip.name == animationName && isPlaying)
        {
            return;
        }
        
        // 현재 재생 중인 애니메이션이 있으면 완료 이벤트 발생
        if (currentClip != null && isPlaying)
        {
            // 이전 애니메이션이 완료되지 않았지만 강제로 새 애니메이션으로 전환할 때 이벤트 발생
            OnAnimationComplete?.Invoke(currentClip.name);
        }
            
        currentClip = newClip;
        currentFrame = 0;
        frameTimer = 0f;
        isPlaying = true;
        
        UpdateSprite();
    }
    
    public void StopAnimation()
    {
        isPlaying = false;
    }
    
    public bool IsAnimationPlaying(string animationName)
    {
        return currentClip != null && currentClip.name == animationName && isPlaying;
    }
    
    public bool IsPlaying()
    {
        return isPlaying;
    }
    
    public string GetCurrentAnimationName()
    {
        return currentClip?.name ?? "";
    }
    
    // 스프라이트 플립 설정
    public void SetFlipX(bool flip)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = flip;
        }
        
        // UI Image는 RectTransform의 scale을 사용하여 플립
        if (uiImage != null)
        {
            Vector3 scale = uiImage.rectTransform.localScale;
            scale.x = flip ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            uiImage.rectTransform.localScale = scale;
        }
    }
    
    public void SetFlipY(bool flip)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipY = flip;
        }
        
        // UI Image는 RectTransform의 scale을 사용하여 플립
        if (uiImage != null)
        {
            Vector3 scale = uiImage.rectTransform.localScale;
            scale.y = flip ? -Mathf.Abs(scale.y) : Mathf.Abs(scale.y);
            uiImage.rectTransform.localScale = scale;
        }
    }
    
    // 애니메이션 클립 설정 조정
    public void SetAnimationClipLoop(string animationName, bool loop)
    {
        if (animationDict.ContainsKey(animationName))
        {
            SimpleAnimationClip clip = animationDict[animationName];
            clip.loop = loop;
        }
    }
    
    // 애니메이션 클립 가져오기
    public SimpleAnimationClip GetAnimationClip(string animationName)
    {
        if (animationDict.ContainsKey(animationName))
        {
            return animationDict[animationName];
        }
        return null;
    }
    
    // 프레임 이벤트 처리
    private void ProcessFrameEvents()
    {
        if (currentClip == null || currentClip.frameEvents == null) return;
        
        foreach (var frameEvent in currentClip.frameEvents)
        {
            if (frameEvent.frameIndex == currentFrame)
            {
                if (debugMode)
                {
                    Debug.Log($"Frame Event: {frameEvent.eventName} with parameter: {frameEvent.parameter} at frame {currentFrame}");
                }
                
                // OffVFX 이벤트 처리
                if (frameEvent.eventName == "OffVFX")
                {
                    HandleOffVFXEvent();
                }
                
                OnFrameEvent?.Invoke(frameEvent.eventName, frameEvent.parameter);
            }
        }
    }
    
    // OffVFX 이벤트 처리 메서드
    private void HandleOffVFXEvent()
    {
        // Key Collect Effect 오브젝트 찾아서 제거
        GameObject[] keyEffects = GameObject.FindGameObjectsWithTag("KeyCollectEffect");
        
        foreach (GameObject effect in keyEffects)
        {
            if (effect != null)
            {
                if (debugMode)
                {
                    Debug.Log($"OffVFX Event: Destroying Key Collect Effect '{effect.name}'");
                }
                
                Destroy(effect);
            }
        }
        
        // 태그가 없는 경우를 위해 이름으로도 검색
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("KeyCollectEffect") || obj.name.Contains("Key Collect Effect"))
            {
                if (debugMode)
                {
                    Debug.Log($"OffVFX Event: Destroying Key Collect Effect by name '{obj.name}'");
                }
                
                Destroy(obj);
            }
        }
    }
}

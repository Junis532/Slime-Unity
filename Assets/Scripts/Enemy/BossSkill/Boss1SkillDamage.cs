using System.Collections;
using UnityEngine;

public class Boss1SkillDamage : MonoBehaviour
{

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {

            if (GameManager.Instance.joystickDirectionIndicator.IsUsingSkill)
            {
                Debug.Log("스킬 사용 중이라 몬스터 데미지 무시");
                return;
            }

            GameManager.Instance.playerStats.currentHP -= 100;
            GameManager.Instance.playerDamaged.PlayDamageEffect(); // 플레이어 데미지 이펙트 재생

            if (GameManager.Instance.playerStats.currentHP <= 0)
            {
                GameManager.Instance.playerStats.currentHP = 0;
            }
        }
    }
}

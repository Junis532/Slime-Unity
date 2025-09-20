using UnityEngine;
using DG.Tweening;

public class ZacSkill : MonoBehaviour
{
    public GameObject piecePrefab;     // 떨어질 오브젝트 프리팹
    public int pieceCount = 5;         // 튕겨나올 조각 수
    public float radius = 1.5f;        // 튕겨나올 방향 범위 반경
    public float jumpPower = 1f;       // 튕기는 높이
    public float jumpDuration = 0.5f;  // 점프 지속 시간

    public void SpawnPieces()
    {
        for (int i = 0; i < pieceCount; i++)
        {
            Vector3 spawnPos = transform.position;

            // Instantiate → PoolManager로 변경
            GameObject piece = PoolManager.Instance.SpawnFromPool(piecePrefab.name, spawnPos, Quaternion.identity);
            if (piece == null) continue;

            // Collider2D 비활성화
            Collider2D col = piece.GetComponent<Collider2D>();
            if (col != null)
                col.enabled = false;

            Vector2 randomDir = Random.insideUnitCircle.normalized;
            Vector3 targetPos = spawnPos + (Vector3)randomDir * radius;

            // DOTween으로 점프 연출 (트윈 중복 방지용 DOKill 권장)
            piece.transform.DOKill();
            piece.transform.DOJump(
                targetPos,
                jumpPower,
                1,
                jumpDuration
            )
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                // 점프 완료 후 Collider 활성화
                if (col != null)
                    col.enabled = true;
            });
        }
    }
}

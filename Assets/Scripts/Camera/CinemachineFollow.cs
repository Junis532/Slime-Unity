using UnityEngine;
using Unity.Cinemachine;

public class CinemachineFollowController : MonoBehaviour
{
    public CinemachineCamera cineCamera;
    public Vector3 customOffset = new Vector3(1000000, 1000000, 0.5f); // 원하는 오프셋

    void Start()
    {
        if (cineCamera != null)
        {
            var followComponent = cineCamera.GetComponent<CinemachineFollow>();
            if (followComponent != null)
            {
                followComponent.FollowOffset = customOffset;
            }
        }
    }
}

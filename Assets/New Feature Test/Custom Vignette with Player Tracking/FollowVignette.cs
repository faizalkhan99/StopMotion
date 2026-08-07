using UnityEngine;

public class FollowVignette : MonoBehaviour
{
    public Camera cam;
    public Transform player;
    public Material vignetteMaterial;
    public Vector3 worldOffset;

    // void Update()
    // {
    //     Debug.Log($" hi ");
    //     Vector3 worldPos = player.position + worldOffset;
    //     Vector3 screenPos = cam.WorldToViewportPoint(worldPos);
    //     Debug.Log($"calculated player pos - {screenPos}");

    //     // Vector3 screenPos = cam.WorldToViewportPoint(player.position);

    //     vignetteMaterial.SetVector(
    //         "_Center",
    //         new Vector4(screenPos.x, screenPos.y, 0, 0)
    //     );
    // }
}

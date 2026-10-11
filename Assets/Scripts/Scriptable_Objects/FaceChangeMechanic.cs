using UnityEngine;

[CreateAssetMenu(fileName = "PlayerFaceSet", menuName = "StopMotion/Player Face Set")]
public class FaceChangeMechanic : ScriptableObject
{
    public Sprite idle;
    public Sprite moving;
    public Sprite jumpUp;
    public Sprite jumpDown;
    public Sprite dash;
    public Sprite fallDownImpact;
    public Sprite die;
    public Sprite blink;
}

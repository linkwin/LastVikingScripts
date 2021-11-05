using UnityEngine;
using System.Collections;
using AC;
public class SUC_FrameFlip : MonoBehaviour
{
    [SerializeField] private AC.Char character;
    [SerializeField] private Transform transformToFlip;
    [SerializeField] private AC_2DFrameFlipping frameFlipping;
    [SerializeField] private float topAngleFreedom = 0f;
    private void Update()
    {
        if (frameFlipping == AC_2DFrameFlipping.None || character == null || transformToFlip == null)
        {
            return;
        }
        bool doFlip = false;
        float spriteAngle = character.GetSpriteAngle();
        switch (frameFlipping)
        {
            case AC_2DFrameFlipping.LeftMirrorsRight:
                doFlip = (spriteAngle >= 0f && spriteAngle < 180f);
                break;
            case AC_2DFrameFlipping.RightMirrorsLeft:
                doFlip = (spriteAngle > 180f && spriteAngle <= 360f);
                break;
            default:
                return;
        }
        if (spriteAngle < topAngleFreedom || spriteAngle > (360f - topAngleFreedom) || (spriteAngle > (180f - topAngleFreedom) && spriteAngle < (180f + topAngleFreedom)))
        {
            return;
        }
        if ((doFlip && transformToFlip.localScale.x > 0f) || (!doFlip && transformToFlip.localScale.x < 0f))
        {
            transformToFlip.localScale = new Vector3(-transformToFlip.localScale.x, transformToFlip.localScale.y, transformToFlip.localScale.z);
        }
    }

}
using UnityEngine;

public class XYJoystickController : MonoBehaviour
{
    public Transform controlDot;   // 拖入小球
    public float radius = 0.05f;    // 圆盘显示半径（与你建模缩放一致）

    // 来自控制逻辑的输入（已归一化）
    public float handleX = 0f;
    public float handleY = 0f;

    void Update()
    {
        // 直接使用归一化坐标乘以半径
        controlDot.localPosition = new Vector3(handleX * radius, handleY * radius, -0.003f);
    }
}

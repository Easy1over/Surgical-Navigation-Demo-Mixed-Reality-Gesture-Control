using UnityEngine;

public class RobotHeadController : MonoBehaviour
{
    [Header("控制方向：设置为局部Y轴 (x, y, 1)")]
    public Vector2 globalXY = Vector2.zero;

    [Header("是否前进（由外部控制）")]
    public int forwardCmd = 0;  // 1=前进，0=不动

    private float forwardStep = 0.00001f;

    void Update()
    {
        // === 方向控制部分 ===
        Vector3 desiredY = new Vector3(2 * globalXY.x, -2 * globalXY.y, 1f).normalized;

        Vector3 rightRef = Vector3.right;
        if (Mathf.Abs(Vector3.Dot(desiredY, rightRef)) > 0.99f)
            rightRef = Vector3.forward;

        Vector3 forward = Vector3.Cross(rightRef, desiredY).normalized;
        Vector3 finalForward = Vector3.Cross(desiredY, forward);

        transform.localRotation = Quaternion.LookRotation(finalForward, desiredY);

        // === 位置移动部分 ===
        if (forwardCmd == 1)
        {
            transform.position += -transform.up * forwardStep;
        }
    }

    // 控制方向（由左手控制传入）
    public void SetControlDirection(Vector2 xy)
    {
        globalXY = xy;
    }

    // 控制前进（由右手是否握拳控制）
    public void SetForwardCommand(int cmd)
    {
        forwardCmd = cmd;
    }
}

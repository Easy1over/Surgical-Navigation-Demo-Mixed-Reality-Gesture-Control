using UnityEngine;

public class JointKalmanFilter
{
    private Vector3 position;   // 估计的位置   维护这个
    private Vector3 velocity;   // 估计的速度  维护这个
    private bool initialized = false;

    //private float processNoise = 0.001f;  // 过程噪声 Q预测的不信任（越大越敏感）
    //private float measurementNoise = 0.05f; // 观测噪声 R测量的不信任（越大越抗抖）

    private float processNoise = 0.0001f;  // 过程噪声 Q预测的不信任（越大越敏感）
    private float measurementNoise = 0.01f; // 观测噪声 R测量的不信任（越大越抗抖）

    public Vector3 Update(Vector3 measurement, float dt)
    {
        if (!initialized)
        {
            position = measurement;
            velocity = Vector3.zero;
            initialized = true;
            return position;
        }

        // === 1. 预测步骤 ===
        Vector3 predictedPosition = position + velocity * dt;

        // === 2. 校正步骤 ===
        Vector3 residual = measurement - predictedPosition; // 残差

        // 简化版卡尔曼增益（固定值近似）
        float gain = processNoise / (processNoise + measurementNoise);

        // 更新状态估计
        position = predictedPosition + gain * residual;
        velocity = velocity + (gain * residual) / dt;

        return position;
    }
}

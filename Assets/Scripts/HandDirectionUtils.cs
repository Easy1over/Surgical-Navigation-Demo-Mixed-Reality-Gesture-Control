using System.Collections.Generic;
using UnityEngine;

public static class HandDirectionUtils
{
    /// <summary>
    /// 计算一组方向向量的球面几何平均方向。
    /// </summary>
    // <param name="directions">输入的一组方向向量（应已归一或近似单位）</param>
    /// <returns>球面上最能代表这组方向的单位向量</returns>
    public static Vector3 ComputeSphericalMeanDirection(List<Vector3> directions)
    {
        if (directions == null || directions.Count == 0)
            return Vector3.forward;  // fallback

        int n = directions.Count;

        // Step 1: 初始方向为线性平均再归一化
        Vector3 mean = Vector3.zero;
        foreach (var dir in directions)
        {
            if (dir.sqrMagnitude > 1e-6f)   
                mean += dir.normalized;
        }
        mean = mean.normalized;

        // Step 2–3: 进行两轮 Slerp 平均迭代
        for (int iter = 0; iter < 2; iter++)
        {
            Vector3 sum = Vector3.zero;
            foreach (var dir in directions)
            {
                Vector3 v = dir.normalized;
                sum += Vector3.Slerp(mean, v, 1.0f / n);
            }
            mean = sum.normalized;
        }

        return mean;
    }
}

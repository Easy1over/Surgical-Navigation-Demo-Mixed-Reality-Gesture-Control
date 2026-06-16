using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class TcpClientSender : MonoBehaviour
{
    [Header("TCP连接参数")]
    public string serverIP = "192.168.43.123";  // 上位机固定IP
    public int serverPort = 12345;              // 上位机监听端口

    [Header("控制输入")]
    public int rightHandCmd = 0;                // 控制命令
    public Vector2 normXY = Vector2.zero;       // 控制方向

    private TcpClient client;
    private NetworkStream stream;

    private float reconnectTimer = 0f;
    private float reconnectInterval = 1f;

    public bool isConnected = false;            // 建立通讯标识符
    public bool enableTcpModule = true;         // 控制模块启用

    private bool isConnecting = false;          // 防止重复开线程//

    private float sendInterval = 0.5f; // 每5ms发送一次（即2Hz）
    private float lastSendTime = 0f;//计时器

    void Start()
    {
        if (!enableTcpModule) return;
        TryConnectAsync(); // 启动时尝试连接
    }

    void Update()
    {
        if (!enableTcpModule) return;

        if (!isConnected)
        {
            reconnectTimer += Time.deltaTime;
            if (reconnectTimer > reconnectInterval && !isConnecting)
            {
                reconnectTimer = 0f;
                TryConnectAsync();
            }
        }
        else
        {
            if (Time.time - lastSendTime >= sendInterval)
            {
                lastSendTime = Time.time;
                SendControlData(); // ✅ 只有每隔固定时间才发送一次
            }
        }
    }

    void TryConnectAsync()
    {
        isConnecting = true;

        Thread connectThread = new Thread(() =>
        {
            try
            {
                // 🧹 清理残留连接
                try { stream?.Close(); } catch { }
                try { client?.Close(); } catch { }
                stream = null;
                client = null;

                client = new TcpClient();
                client.Connect(serverIP, serverPort);
                stream = client.GetStream();
                isConnected = true;
                Debug.Log("✅ TCP连接成功");
            }
            catch (Exception ex)
            {
                isConnected = false;
                Debug.LogWarning("⚠️ TCP连接失败: " + ex.Message);
            }
            finally
            {
                isConnecting = false;
            }
        });

        connectThread.IsBackground = true;
        connectThread.Start();
    }

    void SendControlData()
    {
        if (stream == null || !stream.CanWrite) return;

        string message = $"{rightHandCmd},{normXY.x:F4},{normXY.y:F4}\n";
        byte[] bytes = Encoding.UTF8.GetBytes(message);
        Debug.Log("message"+message);

        try
        {
            stream.Write(bytes, 0, bytes.Length);
        }
        catch (Exception ex)
        {
            Debug.LogError("❌ 发送失败，准备重连: " + ex.Message);
            isConnected = false;
            isConnecting = false; // ✅ 关键：标记可重连

            try { stream?.Close(); } catch { }
            try { client?.Close(); } catch { }
            stream = null;
            client = null;
        }
    }
}

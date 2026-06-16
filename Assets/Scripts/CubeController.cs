using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using TMPro;  // 用于 UI 文本组件
using System.Text;
using System.IO;
using System.Collections.Generic;

//手部关键点类
public class HandJointsData
{
    public Vector3 wrist;
    public Vector3 palm;
    public Vector3 thumbTip;
    public Vector3 indexTip;
    public Vector3 middleTip;
    public Vector3 ringTip;
    public Vector3 pinkyTip;
    public bool isValid = false;
}

//表示机器人初始位姿的结构体

[System.Serializable]
public struct RobotPose
{
    public Vector3 position;
    public Vector2 handle;

    public RobotPose(Vector3 pos, Vector2 h)
    {
        position = pos;
        handle = h;
    }
}


//主要模块在这里
public class CubeController : MonoBehaviour
{
    public GameObject cube;   // 用于绑定立方体
    //private int handState = 0;  //旧的手势分类表示
    
    public TextMeshProUGUI infoText; //用于绑定显示板

    private bool isDetecting = false;  //按钮1触发标志物

    private StringBuilder csvBuffer = new StringBuilder(); //成员 创建一个csv的列表
    private JointKalmanFilter middleTipFilter;  // 中指指尖滤波器类  每一个类维护一组这个关键点的速度与位置
    private Vector3 baseDirection = Vector3.zero; // 成员 用于保存基础方向
    public bool hasBaseDirection = false; //标志 用于确定是否已经保存了合理的方向向量

    private int cmd; //控制指令

    // 成员变量：表示当前手柄位置
    public float handleX = 0f;
    public float handleY = 0f;
    public Vector2 normXY = Vector2.zero;
    // 每步的移动增量
    private const float stepSize = 0.01f;
    public XYJoystickController joystickController;

    //机器人控制量
    public RobotHeadController robotHead;
    public int rightHandCmd = 0; // 右手控制量：1 = 握拳，0 = 放松

    //保存机器人初始位置
    private Vector3 robotInitPos = new Vector3(-16.58f, -140.2f, -15.36f);

    //tcp通讯的类
    public TcpClientSender tcpSender;

    //控制机器人位置的flag
    public int robotPositionFlag = -1;
    private List<RobotPose> savedPoses = new List<RobotPose>();

    //支气管树模型显示的flag
    private bool isVisible = true;
    public GameObject airwayModel;  // 拖入你的支气管树模型
    public GameObject centerlineModel; // 中心线
    public GameObject centerlineRModel; // 中心线

    //点击按钮1后触发 将标志置为true
    public void StartDetectHand()
    {

        isDetecting = true;
        Debug.Log("开始识别手部！");
        Debug.Log("✅ StartDetectHand 已触发！");
        
        robotPositionFlag = robotPositionFlag + 1;

        if (robotPositionFlag == 8)
            robotPositionFlag = 0;

        RobotPose pose = savedPoses[robotPositionFlag];


        if (robotHead != null)
        {

            //robotHead.transform.localPosition = robotInitPos;  // ✅ 父坐标系下归位
            //handleX = 0f;
            //handleY = 0f;

            robotHead.transform.localPosition = pose.position;
            handleX = pose.handle.x;
            handleY = pose.handle.y;

            normXY = NormalizeToUnitCircle(handleX, handleY);//映射到单位圆
            robotHead.SetControlDirection(normXY);
            robotHead.SetForwardCommand(rightHandCmd);
            rightHandCmd = 0;

            joystickController.handleX = normXY.x; //更新板子
            joystickController.handleY = normXY.y;


            Debug.Log("✅ 已复位");
        }
        //robotHead.SetControlDirection(normXY);  // 更新方向归0

    }

    //获取左手关键点  用于得到我需要的几个关键点，并判断是否合法 都合法才会进行操作，否则会有一个false 左手
    private HandJointsData GetLeftHandJoints()
    {
        HandJointsData data = new HandJointsData();

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.Wrist, Handedness.Left, out MixedRealityPose wristPose) &&
            HandJointUtils.TryGetJointPose(TrackedHandJoint.Palm, Handedness.Left, out MixedRealityPose palmPose) &&
            HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, Handedness.Left, out MixedRealityPose thumbPose) &&
            HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Left, out MixedRealityPose indexPose) &&
            HandJointUtils.TryGetJointPose(TrackedHandJoint.MiddleTip, Handedness.Left, out MixedRealityPose middlePose) &&
            HandJointUtils.TryGetJointPose(TrackedHandJoint.RingTip, Handedness.Left, out MixedRealityPose ringPose) &&
            HandJointUtils.TryGetJointPose(TrackedHandJoint.PinkyTip, Handedness.Left, out MixedRealityPose pinkyPose))
        {
            data.wrist = wristPose.Position;
            data.palm = palmPose.Position;
            data.thumbTip = thumbPose.Position;
            data.indexTip = indexPose.Position;
            data.middleTip = middlePose.Position;
            data.ringTip = ringPose.Position;
            data.pinkyTip = pinkyPose.Position;
            data.isValid = true;
        }

        return data;
    }

    //获取右手关键点
    private HandJointsData GetRightHandJoints()
    {
        HandJointsData data = new HandJointsData();

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.Wrist, Handedness.Right, out MixedRealityPose wristPose) &&
            HandJointUtils.TryGetJointPose(TrackedHandJoint.Palm, Handedness.Right, out MixedRealityPose palmPose) &&
            HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, Handedness.Right, out MixedRealityPose thumbPose) &&
            HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Right, out MixedRealityPose indexPose) &&
            HandJointUtils.TryGetJointPose(TrackedHandJoint.MiddleTip, Handedness.Right, out MixedRealityPose middlePose) &&
            HandJointUtils.TryGetJointPose(TrackedHandJoint.RingTip, Handedness.Right, out MixedRealityPose ringPose) &&
            HandJointUtils.TryGetJointPose(TrackedHandJoint.PinkyTip, Handedness.Right, out MixedRealityPose pinkyPose))
        {
            data.wrist = wristPose.Position;
            data.palm = palmPose.Position;
            data.thumbTip = thumbPose.Position;
            data.indexTip = indexPose.Position;
            data.middleTip = middlePose.Position;
            data.ringTip = ringPose.Position;
            data.pinkyTip = pinkyPose.Position;
            data.isValid = true;
        }

        return data;
    }

    //用于判断是否手伸直 返回一个bool值 如果是false则不要求进入更新方向列表的循环
    private bool IsHandStraight(HandJointsData joints)
    {
        // 提取6个向量
        //Vector3 v0 = joints.palm - joints.wrist;
        Vector3 v1 = joints.thumbTip - joints.palm;
        Vector3 v2 = joints.indexTip - joints.palm;
        Vector3 v3 = joints.middleTip - joints.palm;
        Vector3 v4 = joints.ringTip - joints.palm;
        Vector3 v5 = joints.pinkyTip - joints.palm;

        // 构造一个法向量（以中指和无名指为主平面）
        Vector3 normal = Vector3.Cross(v3, v4).normalized;

        // 判断每个向量是否与法向垂直（点乘 ≈ 0）
        float threshold = 0.4f; // 越小越严格，可以调试

        bool isFlat =
            //Mathf.Abs(Vector3.Dot(v0.normalized, normal)) < threshold &&
            Mathf.Abs(Vector3.Dot(v1.normalized, normal)) < threshold &&
            Mathf.Abs(Vector3.Dot(v2.normalized, normal)) < threshold &&
            Mathf.Abs(Vector3.Dot(v3.normalized, normal)) < threshold &&
            Mathf.Abs(Vector3.Dot(v4.normalized, normal)) < threshold &&
            Mathf.Abs(Vector3.Dot(v5.normalized, normal)) < threshold;

        return isFlat;
    }

    //用于判断是否握拳 右手
    private int GetRightHandFistCommand(HandJointsData joints)
    {
        if (!joints.isValid) return 0;

        float threshold = 0.04f; // 5cm

        int closeCount = 0;

        Vector3 thumb = joints.thumbTip;

        if (Vector3.Distance(joints.indexTip, thumb) < threshold) closeCount++;
        if (Vector3.Distance(joints.middleTip, thumb) < threshold) closeCount++;
        if (Vector3.Distance(joints.ringTip, thumb) < threshold) closeCount++;
        if (Vector3.Distance(joints.pinkyTip, thumb) < threshold) closeCount++;

        return (closeCount >= 3) ? 1 : 0;
    }

    //保存csv文件
    private void SaveCSV()
    {
        // 1. 生成带时间戳的文件名
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"middle_tip_data_{timestamp}.csv";

        // 2. 合成路径
        string path = Path.Combine(Application.persistentDataPath, fileName);

        // 3. 写入内容
        File.WriteAllText(path, csvBuffer.ToString());

        // 4. 清空缓存
        csvBuffer.Clear();

        // 5. 输出日志
        Debug.Log("✅ 已保存 CSV 至：" + path);

    }

    //按键触发保存
    public void OnClickSaveCSV()
    {
        //SaveCSV();

        ToggleAirway();//

    }

    //注意！ csv保存按钮现在还用于显示支气管树！

    //显示或消失支气管树
    public void ToggleAirway()
    {
        isVisible = !isVisible;
        airwayModel.SetActive(isVisible);
        centerlineModel.SetActive(isVisible);
        centerlineRModel.SetActive(isVisible);
        Debug.Log("支气管树状态: " + (isVisible ? "显示" : "隐藏"));
    }

    //计算当前方向 返回一个向量
    private Vector3 ComputeHandDirection(HandJointsData joints)
    {
        // 输入关键点集
        // 示例：palm → middleTip 方向
        List<Vector3> dirs = new List<Vector3>
        {
        joints.indexTip - joints.palm,
        joints.middleTip - joints.palm,
        joints.ringTip - joints.palm,
        joints.pinkyTip - joints.palm,
        joints.thumbTip - joints.palm,

        joints.indexTip - joints.wrist,
        joints.middleTip - joints.wrist,
        joints.ringTip - joints.wrist,
        joints.pinkyTip - joints.wrist,
        joints.thumbTip - joints.wrist
        };

        return HandDirectionUtils.ComputeSphericalMeanDirection(dirs);
    }

    //动作映射算法 注意，逻辑需要修改 输入是实时和基础的vec
    private int GetControlCommand(Vector3 currentDir, Vector3 baseDir)
    {
        // 1. 单位化
        Vector3 currentNorm = currentDir.normalized;
        Vector3 baseNorm = baseDir.normalized;

        // 2. 判断夹角是否过小（静止判断）
        float angle = Vector3.Angle(currentNorm, baseNorm);
        if (angle < 10f) 
            return 5;  // 小于8度，视为无动作

        // 3. 构建以 baseNorm 为 x+、Z+ 为上、Y+ 为左 的局部坐标系
        Vector3 xAxis = baseNorm;
        Vector3 zAxis = Vector3.up;  // Z+ 为上
        Vector3 yAxis = Vector3.Cross(zAxis, xAxis).normalized;  // Y+ 为左

        // 4. 将 currentNorm 投影到 Y+ / Z+ 方向
        float y = Vector3.Dot(currentNorm, yAxis);  // >0为左，<0为右
        float z = Vector3.Dot(currentNorm, zAxis);  // >0为上，<0为下

        // 计算方向角（相对于 Y/Z 平面），使用 atan2(z, y)
        float thetaRad = Mathf.Atan2(y, z);           // 以 z 为纵轴，y 为横轴
        float thetaDeg = thetaRad * Mathf.Rad2Deg;
        if (thetaDeg < 0) thetaDeg += 360f;            // 转为 [0, 360) 区间

        // 5. 判断落在哪个方向区间
        if (thetaDeg >= 65f && thetaDeg <= 115f) return 6;   // 右
        if (thetaDeg >= 245f && thetaDeg <= 295f) return 4;  // 左
        if (thetaDeg >= 150f && thetaDeg <= 210f) return 2;  // 下

        // 注意右方向跨越 360 → 0
        if (thetaDeg >= 335f || thetaDeg <= 25f) return 8;   // 上

        // 其余方向认为是斜向区域，返回不动
        return 5;
    }

    //在合适的情况下update调用！用于更新X和Y
    private void UpdateHandlePosition(int cmd)
    {
        switch (cmd)
        {
            case 2:  // 下：Y--
                handleY = Mathf.Max(handleY - stepSize, -1f);
                break;
            case 8:  // 上：Y++
                handleY = Mathf.Min(handleY + stepSize, 1f);
                break;
            case 4:  // 左：X--
                handleX = Mathf.Max(handleX - stepSize, -1f);
                break;
            case 6:  // 右：X++
                handleX = Mathf.Min(handleX + stepSize, 1f);
                break;
            case 5:  // 中：不动
            default:
                // do nothing
                break;
        }
    }

    //限制XY到单位圆！
    private Vector2 NormalizeToUnitCircle(float x, float y)
    {
        float lengthSquared = x * x + y * y;

        if (lengthSquared <= 1f)
        {
            // 在单位圆内，无需修改
            return new Vector2(x, y);
        }
        else
        {
            // 超出单位圆，归一化
            float length = Mathf.Sqrt(lengthSquared);
            return new Vector2(x / length, y / length);
        }
    }

    //按键保存当前方向向量
    public void SaveBaseDirection()
    {
        HandJointsData joints = GetLeftHandJoints();

        if (joints.isValid)
        {
            if (IsHandStraight(joints)) //手伸直才会保存
            {
                baseDirection = ComputeHandDirection(joints);
                Debug.Log("✅ 基准方向已保存: " + baseDirection.ToString("F3"));
                hasBaseDirection = true; //将标志置为1
            }
        }
        else
        {
            Debug.LogWarning("⚠️ 当前手势不可用，无法保存基准方向");
        }
    }

    //开始
    void Start()
    {
        if (infoText != null)
        {
            infoText.text = "stata:" ;
        }

        //创建初始位置点列 // 理论上8个

        savedPoses = new List<RobotPose>()
        {
            //new RobotPose(new Vector3(-16.58f, -140.2f, -15.36f), new Vector2(0f, 0f)),
            //new RobotPose(new Vector3(-6.58f, -130.2f, -69.36f), new Vector2(-0.2f, 0.2f)),
            //new RobotPose(new Vector3(-0.58f, -125.2f, -89.36f), new Vector2(-0.2f, 0.2f)),
            //new RobotPose(new Vector3(14.58f, -119.2f, -109.36f), new Vector2(-0.3f, 0.2f)),
            //new RobotPose(new Vector3(-14.58f, -140.2f, -17.36f), new Vector2(0f, 0f)),
            //new RobotPose(new Vector3(-10.58f, -132.2f, -67.36f), new Vector2(0f, 0.3f)),
            //new RobotPose(new Vector3(-14.58f, -123.2f, -90.36f), new Vector2(0.2f, 0.1f)),
            //new RobotPose(new Vector3(-29.58f, -119.2f, -111.36f), new Vector2(0.5f, 0.2f)),

            new RobotPose(new Vector3(-16.58f, -140.2f, -15.36f), new Vector2(0f, 0f)),
            new RobotPose(new Vector3(-16.58f, -140.2f, -15.36f), new Vector2(0f, 0.8f)),
            new RobotPose(new Vector3(-16.58f, -140.2f, -15.36f), new Vector2(0.8f, 0f)),
            new RobotPose(new Vector3(-16.58f, -140.2f, -15.36f), new Vector2(0f, -0.8f)),
            new RobotPose(new Vector3(-16.58f, -140.2f, -15.36f), new Vector2(-0.8f, 0f)),
            new RobotPose(new Vector3(-16.58f, -140.2f, -15.36f), new Vector2(0.5f, 0.5f)),
            new RobotPose(new Vector3(-16.58f, -140.2f, -15.36f), new Vector2(-0.4f, 0.6f)),
            new RobotPose(new Vector3(-16.58f, -140.2f, -15.36f), new Vector2(0.5f, -0.4f)),





        };



        middleTipFilter = new JointKalmanFilter();  // 初始化滤波器
        //csvBuffer.AppendLine("Time,Raw_X,Raw_Y,Raw_Z,Filtered_X,Filtered_Y,Filtered_Z");
    }

    //更新
    void Update()
    {
       
        // ✅ 获取关键点数据
        HandJointsData joints = GetLeftHandJoints();
        HandJointsData rightJoints = GetRightHandJoints();
        //if (joints.isValid) //如果所有关键点合法
        //{
            //无事发生
        //}

        //下面四句调试时开启
        //robotHead.SetControlDirection(normXY);//给实际机器人去更新姿态
        //robotHead.SetForwardCommand(rightHandCmd); //给实际机器人去更新位置
        //joystickController.handleX = normXY.x;
        //joystickController.handleY = normXY.y;


        if (!isDetecting) return;//按钮1触发器

        //调试版本和普通版本的区别在于需要将 joints.isValid(左手点合法)+IsHandStraight(joints)(左手伸直)打开保证左手有效 可更新左手
        //当rightJoints.isValid(右手点合法) 可更新右手控制量
        //只要左手合法 就会更新tcp控制量发送



        //if (joints.isValid) //所有点合法
        if (false) //所有点合法
            {

            //首先是中指滤波
            //无论有没有基础向量，只要有观测点都可以有滤波
            Vector3 rawMiddle = joints.middleTip;  //中指坐标
            Vector3 filteredMiddle = middleTipFilter.Update(rawMiddle, Time.deltaTime); //滤波坐标

            // 计入到列表里
            csvBuffer.AppendLine(
            $"{rawMiddle.x:F4},{rawMiddle.y:F4},{rawMiddle.z:F4}," +
            $"{filteredMiddle.x:F4},{filteredMiddle.y:F4},{filteredMiddle.z:F4}"
                );

            //当有基础向量的情况下才进行下列操作
            if(hasBaseDirection)
            {
                //只有当手伸直才认为动作有效 才进入更新
                //if (IsHandStraight(joints))
                if (true)
                    {

                    if (infoText != null) //弹窗滤波后结果 不影响控制
                    {
                        infoText.text =
                            //"Raw:\n" + rawMiddle.ToString("F3") + "\n" +
                            //"Filtered:\n" + filteredMiddle.ToString("F3") + "\n"
                            "BaseDirection\n" + baseDirection.ToString("F3") + "\n"
                            + "cmd\n" + cmd+ " motion : ("+ rightHandCmd+ ","+ normXY.x +"," + normXY.y+")" + "\n"
                            + robotPositionFlag
                            ;
                    }

                    Vector3 currentDir = ComputeHandDirection(joints);//获取当前方向向量

                    cmd = GetControlCommand(currentDir, baseDirection);//获取控制量

                    UpdateHandlePosition(cmd);//维护更新全局XY

                    normXY = NormalizeToUnitCircle(handleX, handleY);//映射到单位圆

                    joystickController.handleX = normXY.x; //更新板子
                    joystickController.handleY = normXY.y;
             
                    //if (rightJoints.isValid) //更新位置
                    if (true) //更新位置
                        {
                        //rightHandCmd = GetRightHandFistCommand(rightJoints);
                        //rightHandCmd = 1;
                    }
                    else
                    {
                        rightHandCmd = 0; // 无效帧则默认为不动
                    }

                    Debug.LogWarning("更新");
                    robotHead.SetControlDirection(normXY);//给实际机器人去更新位置
                    robotHead.SetForwardCommand(rightHandCmd);//给实际机器人去更新姿态

                    //真正的控制量是normXY和rightHandCmd
                    
                    if (tcpSender != null) //tcp建立好后
                    {
                        tcpSender.rightHandCmd = rightHandCmd;
                        tcpSender.normXY = normXY;
                    }

                }
            }
        }
        else
        {
            //Debug.Log("❌ 无法获取左手关键点，跳过此帧！")
            return;
            //可以改成：视为：不变 这个动作
        }
    }
}

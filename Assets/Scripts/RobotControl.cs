//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Threading;
//using UnityEngine;
////using ECAN;
////using ECanTest;

//public class RobControl : MonoBehaviour
//{
//    ComProc mCan = new ComProc();
//    INIT_CONFIG init_config = new INIT_CONFIG();
//    CAN_OBJ frameinfo = new CAN_OBJ();
//    int temp_pwm, temp_velocity, temp_position;
//    float forward_position;

//    // Start is called before the first frame update
//    void Start()
//    {

//        init_config.AccCode = 0;
//        init_config.AccMask = 0xffffff;
//        init_config.Filter = 0;

//        init_config.Timing0 = 0;
//        init_config.Timing1 = 0x14;

//        init_config.Mode = 0;

//        if (ECANDLL.OpenDevice(4, 0, 0) != ECAN.ECANStatus.STATUS_OK)
//        {

//            Debug.Log("Open device fault!");

//        }

//        //Set can1 baud
//        if (ECANDLL.InitCAN(4, 0, 0, ref init_config) != ECAN.ECANStatus.STATUS_OK)
//        {

//            Debug.Log("Init can fault!");

//            ECANDLL.CloseDevice(4, 0);
//        }
//        mCan.EnableProc = true;
//        Debug.Log("Open success!");

//        if (ECANDLL.StartCAN(4, 0, 0) == ECAN.ECANStatus.STATUS_OK)
//        {
//            Debug.Log("Start CAN1 Success");

//        }
//        else
//        {
//            Debug.Log("Start Fault");
//        }

//        frameinfo.SendType = 0;
//        frameinfo.RemoteFlag = 0;
//        frameinfo.ExternFlag = 0;
//        frameinfo.DataLen = 8;
//        frameinfo.data = new byte[8];

//        frameinfo.ID = 0x000;
//        frameinfo.data[0] = 0x55;
//        frameinfo.data[1] = 0x55;
//        frameinfo.data[2] = 0x55;
//        frameinfo.data[3] = 0x55;
//        frameinfo.data[4] = 0x55;
//        frameinfo.data[5] = 0x55;
//        frameinfo.data[6] = 0x55;
//        frameinfo.data[7] = 0x55;
//        sendmsg();
//        Thread.Sleep(500);

//        frameinfo.ID = 0x001;
//        frameinfo.data[0] = 0x05;
//        frameinfo.data[1] = 0x55;
//        frameinfo.data[2] = 0x55;
//        frameinfo.data[3] = 0x55;
//        frameinfo.data[4] = 0x55;
//        frameinfo.data[5] = 0x55;
//        frameinfo.data[6] = 0x55;
//        frameinfo.data[7] = 0x55;
//        sendmsg();
//        Thread.Sleep(500);
//    }

//    // Update is called once per frame
//    void Update()
//    {
//        float hl = Input.GetAxis("Horizontal_Left");
//        float vl = Input.GetAxis("Vertical_Left");

//        float vr = Input.GetAxis("Vertical_Right");
//        //Debug.Log(vr);
//        forward_position = forward_position - 80 * vr * vr * vr;
//        rob_forward((int)forward_position);
//        //rob_radius((int)forward_position);
//        //Debug.Log(forward_position);

//        //Debug.Log(hl + "," + vl);
//        float theta, r;
//        if (hl < 0)
//        {
//            theta = -(float)(System.Math.Atan2(hl, vl) + Math.PI / 2);
//            r = -(float)System.Math.Sqrt(hl * hl + vl * vl);
//            if (r < -1)
//            {
//                r = -1;
//            }
//        }
//        else if (hl > 0)
//        {
//            theta = (float)(Math.PI / 2 - System.Math.Atan2(hl, vl));
//            r = (float)System.Math.Sqrt(hl * hl + vl * vl);
//            if (r > 1)
//            {
//                r = 1;
//            }
//        }
//        else
//        {
//            theta = 0f;
//            //r = -vl;
//            r = 0f;
//        }

//        //Debug.Log(r+"," +theta);
//        int position = (int)(12000 * r * r * r);
//        rob_bend(position);
//        //Debug.Log(position);
//        int angle = (int)(1500 * theta / (Math.PI / 2));
//        //Debug.Log(angle);
//        rob_rotation(angle);
//    }

//    public void robot_stop()
//    {
//        Debug.Log("Stop ...");
//        frameinfo.ID = 0x000;
//        frameinfo.data[0] = 0x55;
//        frameinfo.data[1] = 0x55;
//        frameinfo.data[2] = 0x55;
//        frameinfo.data[3] = 0x55;
//        frameinfo.data[4] = 0x55;
//        frameinfo.data[5] = 0x55;
//        frameinfo.data[6] = 0x55;
//        frameinfo.data[7] = 0x55;
//        sendmsg();
//    }

//    public void rob_forward(int position)
//    {
//        // 正前负后 MAX 100000
//        //Debug.Log("Forward ...");
//        temp_pwm = 2000;
//        temp_position = position;
//        //Debug.Log(temp_position);
//        temp_velocity = 2000;
//        frameinfo.ID = 0x036;
//        cal();
//        sendmsg();
//    }

//    public void rob_rotation(int angle)
//    {
//        //正左负右 MAX 左右1500
//        //Debug.Log("Rotate ...");
//        temp_pwm = 1000;
//        temp_position = angle;
//        //Debug.Log(temp_position);
//        temp_velocity = 700;
//        frameinfo.ID = 0x016;
//        cal();
//        sendmsg();
//    }

//    public void rob_bend(int position)
//    {
//        //正左负右， MAX 10000
//        //Debug.Log("Bend ...");
//        temp_pwm = 1000;
//        temp_position = position;
//        //Debug.Log(temp_position);
//        temp_velocity = 700;
//        frameinfo.ID = 0x046;
//        cal();
//        sendmsg();
//    }

//    public void rob_radius(int position)
//    {
//        // 负进， 正退 MAX 44000
//        Debug.Log("Change radius ...");
//        temp_pwm = 1000;
//        temp_position = position;
//        //Debug.Log(temp_position);
//        temp_velocity = 1000;
//        frameinfo.ID = 0x026;
//        cal();
//        sendmsg();
//    }

//    private void cal()
//    {
//        frameinfo.data[0] = (byte)((temp_pwm >> 8) & 0xff);
//        frameinfo.data[1] = (byte)((temp_pwm) & 0xff);
//        frameinfo.data[2] = (byte)((temp_velocity >> 8) & 0xff);
//        frameinfo.data[3] = (byte)(temp_velocity & 0xff);
//        frameinfo.data[4] = (byte)((temp_position >> 24) & 0xff);
//        frameinfo.data[5] = (byte)((temp_position >> 16) & 0xff);
//        frameinfo.data[6] = (byte)((temp_position >> 8) & 0xff);
//        frameinfo.data[7] = (byte)(temp_position & 0xff);
//    }

//    private void sendmsg()
//    {
//        mCan.gSendMsgBuf[mCan.gSendMsgBufHead].ID = frameinfo.ID;
//        mCan.gSendMsgBuf[mCan.gSendMsgBufHead].DataLen = frameinfo.DataLen;
//        mCan.gSendMsgBuf[mCan.gSendMsgBufHead].data = frameinfo.data;
//        mCan.gSendMsgBuf[mCan.gSendMsgBufHead].ExternFlag = frameinfo.ExternFlag;
//        mCan.gSendMsgBuf[mCan.gSendMsgBufHead].RemoteFlag = frameinfo.RemoteFlag;
//        mCan.gSendMsgBufHead += 1;
//        if (mCan.gSendMsgBufHead >= ComProc.SEND_MSG_BUF_MAX)
//        {
//            mCan.gSendMsgBufHead = 0;
//        }
//        mCan.SendMessages();
//    }

//    void OnDestroy()
//    {
//        Debug.Log("Back to zero");
//        temp_pwm = 1000;
//        temp_position = 0;
//        temp_velocity = 1000;
//        frameinfo.ID = 0x016;
//        cal();
//        sendmsg();

//        temp_pwm = 1000;
//        temp_position = 0;
//        temp_velocity = 1000;
//        frameinfo.ID = 0x026;
//        cal();
//        sendmsg();

//        temp_pwm = 2500;
//        temp_position = 0;
//        temp_velocity = 2500;
//        frameinfo.ID = 0x036;
//        cal();
//        sendmsg();

//        temp_pwm = 1000;
//        temp_position = 0;
//        temp_velocity = 1000;
//        frameinfo.ID = 0x046;
//        cal();
//        sendmsg();

//        Thread.Sleep(1000);

//        ECANDLL.CloseDevice(4, 0);
//        Debug.Log("Close Device");
//    }
//}

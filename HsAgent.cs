using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

struct sUser
{
    public float Users_Task;
    public float OffloadArray;
    public float BandwidthArray;
    public float Users_AllocatedDatarate;
    public float Users_LocalTask;
    public float TransmitTime;
    public float UAVTime;
    public float LocalTime;
    public float TotalTime;
}

public class HsAgent : Agent
{
    EnvironmentParameters m_ResetParams;

    public GameObject mUAV;
    public GameObject[] mUsers;
    sUser[] sUsers;

    int USER_NUM;
    float UAV_Task;
    float UAV_Cycle;
    float Local_Cycle;

    float CurResult;
    int CurStep;

    //환경이 처음실행될 때 한번만 호출되는 초기화 함수로 각 오브젝트들과 파라미터를 초기화하는 내용이 구성
    public override void Initialize()
    {   
        USER_NUM = 4;
        sUsers = new sUser[USER_NUM];
        UAV_Task = 40.0f;
        UAV_Cycle = 10.0f;
        Local_Cycle = 2.0f;

        m_ResetParams = Academy.Instance.EnvironmentParameters;
        SetResetParameters();
    }
    
    //에이전트에게 전달할  관측의 요소를 결정하는 역할 수행
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(new Vector2(mUAV.transform.position.x, mUAV.transform.position.z));

        for(int i = 0 ; i < USER_NUM ; ++i)
        {
            sensor.AddObservation(new Vector2(mUsers[i].transform.position.x, mUsers[i].transform.position.z));
            sensor.AddObservation(sUsers[i].Users_Task);
        }    
    }
    
    //알고리즘을 통해 결정된 행동에 따라 에이전트 제어, 보상결정, 에피소드 종료 조건 설정
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {   
        //움직이고 시간 구하기
        var actionX = Mathf.Clamp(actionBuffers.ContinuousActions[0], -1.0f, 1.0f);
        var actionZ = Mathf.Clamp(actionBuffers.ContinuousActions[1], -1.0f, 1.0f);
        mUAV.transform.position = new Vector3(mUAV.transform.position.x + actionX, 0f, mUAV.transform.position.z + actionZ);

        float CurOffloadSum = 0f;
        float CurBandwidthSum = 0f;
        for (int i = 0; i < USER_NUM; ++i)
        {          
            
            /*var OffloadPercentage = 0.2f * Mathf.Clamp(actionBuffers.ContinuousActions[2 i], -1.0f, 1.0f) + 0.2f;
            //var OffloadPercentageAbs = Mathf.Abs(OffloadPercentage);
            sUsers[i].OffloadArray = OffloadPercentage;*/
            
            var BandwidthPercentage = 0.5f * Mathf.Clamp(actionBuffers.ContinuousActions[2+i], -1.0f, 1.0f) + 0.5f;
            //var BandwidthPercentageAbs = Mathf.Abs(BandwidthPercentage);
            sUsers[i].BandwidthArray = BandwidthPercentage;

            //CurOffloadSum += OffloadPercentage;
            CurBandwidthSum += BandwidthPercentage;
        }

        /*if (CurOffloadSum > 1.0f)
        {
            SetReward(-0.01f);
            Debug.Log("FAIL1");
            //EndEpisode();
        }*/
        if (CurBandwidthSum > 1.0f)
        {
            SetReward(1.0f - CurBandwidthSum );
            Debug.Log("FAIL2");
            //EndEpisode();
        }
        else
        {
            CurStep++;
            Max_Time();
            float TMPSUM = 0;
            for (int i=0; i < USER_NUM; ++i)
            {
                if (sUsers[i].Users_Task <= 0) continue;

                if (sUsers[i].Users_AllocatedDatarate != 0) 
                {
                    TMPSUM += sUsers[i].Users_AllocatedDatarate;
                    CurResult += sUsers[i].Users_AllocatedDatarate;
                }
            }

            for (int i=0; i < USER_NUM; ++i)
            {
                if (sUsers[i].Users_Task <= 0) continue;

                if (sUsers[i].Users_AllocatedDatarate == 0) 
                {
                    sUsers[i].Users_Task -= sUsers[i].Users_LocalTask;
                }
                else
                {
                    sUsers[i].OffloadArray =0.2f; //sUsers[i].Users_AllocatedDatarate/TMPSUM; 0.2 
                    sUsers[i].Users_Task -= (UAV_Task * sUsers[i].OffloadArray) + sUsers[i].Users_LocalTask;
                }
            }

            bool isDone = true;
            for (int i = 0; i < USER_NUM; ++i)
            {
                if (sUsers[i].Users_Task > 0)
                {
                    isDone = false;
                    break;
                }
            }
            
            if (isDone)
            {
                SetReward(100f * CurResult /(CurStep*USER_NUM));
                Debug.Log(100f * CurResult /(CurStep*USER_NUM));
                EndEpisode();
            }
            else
            {
                SetReward(-0.01f);
                Debug.Log("ING");
            }
        }
    }
    
    //각 에피소드가 시작할 때 마다 호출되는 함수로 환경의 상태를 초기화하도록 구성
    public override void OnEpisodeBegin()
    {
        SetResetParameters();
    }

    // UAV & 유저 위치, 데이터양 초기화
    public void SetResetParameters()
    {
        CurStep =0;
        CurResult = 0f;
        //UAV 위치, Task 초기화
        mUAV.transform.position = new Vector3(Random.Range(-500.0f, 500.0f), 0f, Random.Range(-500.0f, 500.0f));

        for (int i = 0; i < USER_NUM; ++i)
        {
            mUsers[i].transform.position = new Vector3(Random.Range(-500.0f, 500.0f), 0f, Random.Range(-500.0f, 500.0f));

            sUsers[i].Users_Task = Random.Range(300.0f, 400.0f);
            sUsers[i].TotalTime = 0.0f;
            sUsers[i].TransmitTime = 0.0f;
            sUsers[i].UAVTime = 0.0f;
            sUsers[i].LocalTime = 0.0f;
            sUsers[i].OffloadArray = UAV_Task * 0.25f;
            sUsers[i].Users_LocalTask = 8.0f;
            sUsers[i].BandwidthArray = 0.25f;

        }
    }
    
    //각 유저에게 할당된 Bandwidth에 의한 Datarate 계산
    public void GetDataRate()
    {
        float p_0 = 4.0f; 
        float g_0 = 10.0f;
        float sigma = 174f;
        float w_0 = 20e2F;
        float alpha = 0.7f;
        
        for (int i = 0; i < USER_NUM; ++i)
        {
            float dist = Vector3.Distance(new Vector3(mUAV.transform.position.x, 50.0f, mUAV.transform.position.z), new Vector3(mUsers[i].transform.position.x, 0.0f, mUsers[i].transform.position.z));
            float gain = g_0 / Mathf.Pow(dist, alpha);
            float p = p_0 / 10;
            float sinr = (p * gain) / sigma;
            float datarate = w_0 * Mathf.Log(1 + sinr);
            sUsers[i].Users_AllocatedDatarate = sUsers[i].BandwidthArray * datarate;
        }
        return;
    }
    
    //전송시간
    public void Transmit_Time()
    {   
        GetDataRate();
        for (int i=0; i < USER_NUM; ++i)
        {
            sUsers[i].TransmitTime = (UAV_Task * sUsers[i].OffloadArray) / sUsers[i].Users_AllocatedDatarate;
        }
        return;
    }

    //UAV 계산시간
    public void UAV_Time()
    {   
        for (int i=0; i < USER_NUM; ++i)
        {
            sUsers[i].UAVTime = (UAV_Task * sUsers[i].OffloadArray) / UAV_Cycle;
        }
        return;
    }

    //Local 계산시간
    public void Local_Time()
    {   
        for (int i=0; i < USER_NUM; ++i)
        {
            if (sUsers[i].Users_Task <= 0)
            {
                sUsers[i].LocalTime = 0f;
                continue;
            }
            sUsers[i].Users_LocalTask = Mathf.Min(8.0f, sUsers[i].Users_Task - (UAV_Task * sUsers[i].OffloadArray));
            sUsers[i].LocalTime = sUsers[i].Users_LocalTask / Local_Cycle;
        }
        return;
    }

    //전체 걸리는 시간
    public float Max_Time()
    {   
        Transmit_Time();
        UAV_Time();
        Local_Time();
        var MaxTime = 0f;
        for (int i=0; i < USER_NUM; ++i)
        {
            sUsers[i].TotalTime = sUsers[i].TransmitTime + sUsers[i].UAVTime + sUsers[i].LocalTime;
            if (sUsers[i].TotalTime >= MaxTime) MaxTime = sUsers[i].TotalTime;
        }
        return MaxTime;    
    }
}

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
    
   
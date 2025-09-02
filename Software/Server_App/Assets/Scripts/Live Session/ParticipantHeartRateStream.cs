using UnityEngine;

public class ParticipantHeartRateStream
{
    public event System.Action<int> OnHeartRateReceived;
    public event System.Action<float> OnExertionReceived;

    private float baselineHR = -1;
    private int maxHR = -1;

    public void ReceiveNewHR(int newHr)
    {
        //May need to do other stuff
        AnnounceNewHeartRateReceived(newHr);
    }

    public void AnnounceNewHeartRateReceived(int hr)
    {
        OnHeartRateReceived(hr);

        if(baselineHR != -1 && maxHR != -1) 
        {
            float exertion = GetHRR(hr);
            OnExertionReceived(exertion);
        }
    }

    public void SetBaseline(float baseline)
    {
        baselineHR = baseline;
    }

    public void SetMaxHR(int age)
    {
        maxHR = 220 - age;
    }

    private float GetHRR(int curHR) 
    {
        return (float)(curHR-baselineHR)/(maxHR-baselineHR) * 100;
    }
}

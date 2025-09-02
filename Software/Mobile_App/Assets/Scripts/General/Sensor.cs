using UnityEngine;

public class Sensor
{
    protected string name = "NaN";

    public Sensor(string name)
    {
        this.name = name;
    }

    public void SetName(string name)
    {
        this.name = name;
    }

    public virtual void ConnectToSensor()
    {
    }
}

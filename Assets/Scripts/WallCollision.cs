using UnityEngine;

public class WallCollision : MonoBehaviour
{
    public enum HazardType
    {
        Laser = 0,
        Mine = 1
    }

    [SerializeField] private HazardType hazardType = HazardType.Laser;

    public HazardType Type => hazardType;

    public bool ShouldSpawnExplosion => hazardType == HazardType.Mine;

    public void SetType(HazardType value)
    {
        hazardType = value;
    }
}

using UnityEngine;

public class Table : MonoBehaviour
{
    public bool IsOccupied;

    public Vector3 SeatPosition
    {
        get { return transform.position + new Vector3(0f, 0f, -1.5f); }
    }
}
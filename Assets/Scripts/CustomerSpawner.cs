using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    public GameObject CustomerPrefab;
    public Table[] Tables;
    public Transform SpawnPoint;
    public Transform ExitPoint;
    public float Interval = 5f;

    float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < Interval) return;
        timer = 0f;

        Table freeTable = null;
        foreach (Table t in Tables)
        {
            if (!t.IsOccupied) { freeTable = t; break; }
        }
        if (freeTable == null) return;

        freeTable.IsOccupied = true;
        GameObject go = Instantiate(CustomerPrefab, SpawnPoint.position, Quaternion.identity);
        Customer c = go.GetComponent<Customer>();
        c.TargetTable = freeTable;
        c.ExitPoint = ExitPoint.position;
    }
}
using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    public GameObject CustomerPrefab;
    public Table[] Tables;
    public Transform SpawnPoint;
    public Transform ExitPoint;
    public Dish[] Menu;
    public float Interval = 3f;

    float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < Interval) return;
        timer = 0f;

        Table freeTable = null;
        foreach (Table t in Tables)
        {
            if (!t.gameObject.activeInHierarchy) continue;
            if (!t.IsOccupied) { freeTable = t; break; }
        }
        if (freeTable == null) return;

        freeTable.IsOccupied = true;
        GameObject go = Instantiate(CustomerPrefab, SpawnPoint.position, Quaternion.identity);
        Customer c = go.GetComponent<Customer>();
        c.TargetTable = freeTable;
        c.ExitPoint = ExitPoint.position;

        if (Menu != null && Menu.Length > 0)
        {
            c.Order = Menu[Random.Range(0, Menu.Length)];
        }
    }
}
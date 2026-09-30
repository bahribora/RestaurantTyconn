using System.Collections.Generic;
using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    public static CustomerSpawner Instance { get; private set; }

    public GameObject CustomerPrefab;
    public Table[] Tables;
    public Transform SpawnPoint;
    public Transform ExitPoint;
    public Dish[] Menu;
    public float Interval = 4f;
    public int MaxQueue = 4;

    public List<Customer> Queue = new List<Customer>();
    float timer;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float interval = Interval / GameManager.Instance.SpawnRateMultiplier;
        if (timer < interval) return;
        timer = 0f;

        if (Queue.Count >= MaxQueue) return;

        GameObject go = Instantiate(CustomerPrefab, SpawnPoint.position, Quaternion.identity);
        Customer c = go.GetComponent<Customer>();
        c.ExitPoint = ExitPoint.position;

        if (Menu != null && Menu.Length > 0)
        {
            int count = Mathf.Min(Menu.Length, GameManager.Instance.UnlockedDishes);
            c.Order = Menu[Random.Range(0, count)];
        }

        Queue.Add(c);
    }

    public Table GetFreeTable()
    {
        foreach (Table t in Tables)
        {
            if (t == null || !t.gameObject.activeInHierarchy) continue;
            if (!t.IsOccupied) return t;
        }
        return null;
    }

    public Vector3 GetQueuePosition(Customer c)
    {
        int i = Queue.IndexOf(c);
        if (i < 0) i = 0;
        return SpawnPoint.position + Vector3.forward * (4f + i * 1.2f);
    }
}
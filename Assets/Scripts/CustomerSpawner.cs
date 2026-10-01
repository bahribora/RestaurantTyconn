using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CustomerSpawner : MonoBehaviour
{
    public static CustomerSpawner Instance;

    [Header("Spawner Settings")]
    public GameObject customerPrefab;
    public List<Table> tables = new List<Table>();
    public Transform spawnPoint;
    public Transform exitPoint;

    [Header("Menu & Upgrades")]
    public Dish[] Menu;

    [Header("Timing & Queue")]
    public float interval = 3f;
    public int maxQueue = 4;
    public List<Customer> Queue = new List<Customer>();

    private float timer = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (customerPrefab == null || spawnPoint == null || tables == null || tables.Count == 0)
        {
            return;
        }

        timer += Time.deltaTime;

        if (timer >= interval)
        {
            timer = 0f;
            TrySpawnCustomer();
        }
    }

    void TrySpawnCustomer()
    {
        if (Queue.Count >= maxQueue) return;

        GameObject newCustomer = Instantiate(customerPrefab, spawnPoint.position, spawnPoint.rotation);

        NavMeshAgent agent = newCustomer.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(spawnPoint.position, out hit, 2.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
        }

        Customer customerComp = newCustomer.GetComponent<Customer>();
        if (customerComp != null && !Queue.Contains(customerComp))
        {
            Queue.Add(customerComp);
        }
    }

    public Vector3 GetQueuePosition(Customer customer)
    {
        int index = Queue.IndexOf(customer);
        if (index < 0) index = 0;

        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;
        Vector3 forward = spawnPoint != null ? spawnPoint.forward : transform.forward;

        return spawnPos + forward * (index * 1.5f);
    }

    public Table GetFreeTable()
    {
        foreach (Table table in tables)
        {
            if (table != null && !table.IsOccupied)
            {
                return table;
            }
        }
        return null;
    }

    public Table GetAvailableTable()
    {
        return GetFreeTable();
    }
}
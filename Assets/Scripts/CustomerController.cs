using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class CustomerController : MonoBehaviour
{
    public enum State { Spawning, WalkingToTable, WaitingForFood, Eating, Leaving }
    public State CurrentState { get; private set; } = State.Spawning;

    public Dish OrderedDish { get; private set; }

    NavMeshAgent agent;
    Animator animator;
    Table targetTable;
    Transform exitPoint;

    float patienceTimer;
    float maxPatience = 20f;
    float eatTimer;
    float eatDuration = 5f;

    // Animasyon Parametre Adları
    readonly int animSpeed = Animator.StringToHash("Speed");
    readonly int animIsSitting = Animator.StringToHash("IsSitting");

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Animator varsa yürüme hızına göre animasyon parametresini güncelle
        if (animator != null && agent != null)
        {
            float speed = agent.velocity.magnitude;
            animator.SetFloat(animSpeed, speed);
        }

        switch (CurrentState)
        {
            case State.WalkingToTable:
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    OnArrivedAtTable();
                }
                break;

            case State.WaitingForFood:
                patienceTimer -= Time.deltaTime;
                if (patienceTimer <= 0f)
                {
                    OnGetAngryAndLeave();
                }
                break;

            case State.Eating:
                eatTimer -= Time.deltaTime;
                if (eatTimer <= 0f)
                {
                    OnFinishEating();
                }
                break;

            case State.Leaving:
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    Destroy(gameObject);
                }
                break;
        }
    }

    public void Setup(Table table, Transform exit, Dish dish, float patienceBonus, float eatSpeedBonus)
    {
        targetTable = table;
        exitPoint = exit;
        OrderedDish = dish;

        maxPatience = 20f * patienceBonus;
        patienceTimer = maxPatience;

        eatDuration = 5f * eatSpeedBonus;

        if (targetTable != null)
        {
            targetTable.IsOccupied = true;
            agent.SetDestination(targetTable.transform.position);
            CurrentState = State.WalkingToTable;
        }
    }

    void OnArrivedAtTable()
    {
        CurrentState = State.WaitingForFood;
        if (targetTable != null)
        {
            transform.rotation = targetTable.transform.rotation;
        }

        if (animator != null)
        {
            animator.SetBool(animIsSitting, true);
        }
    }

    public void ServeFood()
    {
        if (CurrentState != State.WaitingForFood) return;

        CurrentState = State.Eating;
        eatTimer = eatDuration;
    }

    void OnFinishEating()
    {
        float price = OrderedDish != null ? OrderedDish.Price : 2f;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RecordServed(price * GameManager.Instance.PriceFactor, transform.position);
        }

        LeaveRestaurant();
    }

    void OnGetAngryAndLeave()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RecordAngry();
        }

        LeaveRestaurant();
    }

    void LeaveRestaurant()
    {
        if (targetTable != null)
        {
            targetTable.IsOccupied = false;
            targetTable = null;
        }

        if (animator != null)
        {
            animator.SetBool(animIsSitting, false);
        }

        CurrentState = State.Leaving;
        if (exitPoint != null)
        {
            agent.SetDestination(exitPoint.position);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
using UnityEngine;

public class Customer : MonoBehaviour
{
    public Table TargetTable;
    public Vector3 ExitPoint;
    public float Speed = 3f;
    public float EatTime = 3f;
    public int Payment = 50;

    enum State { GoingToTable, Eating, Leaving }
    State state = State.GoingToTable;
    float timer;

    void Update()
    {
        switch (state)
        {
            case State.GoingToTable:
                if (MoveTo(TargetTable.SeatPosition))
                {
                    state = State.Eating;
                    timer = EatTime;
                }
                break;

            case State.Eating:
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    GameManager.Instance.AddMoney(Payment);
                    TargetTable.IsOccupied = false;
                    state = State.Leaving;
                }
                break;

            case State.Leaving:
                if (MoveTo(ExitPoint)) Destroy(gameObject);
                break;
        }
    }

    bool MoveTo(Vector3 target)
    {
        target.y = transform.position.y;
        transform.position = Vector3.MoveTowards(transform.position, target, Speed * Time.deltaTime);
        return Vector3.Distance(transform.position, target) < 0.05f;
    }
}
using UnityEngine;

public class Customer : MonoBehaviour
{
    public Table TargetTable;
    public Vector3 ExitPoint;
    public Dish Order;
    public float Speed = 3f;
    public float EatTime = 3f;
    public float FoodScale = 2f;

    enum State { GoingToTable, Eating, Leaving }
    State state = State.GoingToTable;
    float timer;
    GameObject food;

    void Start()
    {
        Renderer body = GetComponent<Renderer>();
        if (body != null)
        {
            body.material.color = Color.HSVToRGB(Random.value, 0.6f, 0.9f);
        }
    }

    void Update()
    {
        switch (state)
        {
            case State.GoingToTable:
                if (MoveTo(TargetTable.SeatPosition))
                {
                    state = State.Eating;
                    timer = EatTime * GameManager.Instance.EatTimeMultiplier;
                    ServeFood();
                }
                break;

            case State.Eating:
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    if (food != null) Destroy(food);
                    int price = (Order != null && Order.Price > 0) ? Order.Price : 50;
                    price = Mathf.RoundToInt(price * GameManager.Instance.PriceMultiplier);
                    GameManager.Instance.AddMoney(price);
                    TargetTable.IsOccupied = false;
                    state = State.Leaving;
                }
                break;

            case State.Leaving:
                if (MoveTo(ExitPoint)) Destroy(gameObject);
                break;
        }
    }

    void ServeFood()
    {
        if (Order == null || Order.Prefab == null) return;
        Vector3 pos = TargetTable.transform.position + Vector3.up * 0.55f;
        food = Instantiate(Order.Prefab, pos, Quaternion.identity);
        food.transform.localScale *= FoodScale;
    }

    bool MoveTo(Vector3 target)
    {
        target.y = transform.position.y;
        transform.position = Vector3.MoveTowards(transform.position, target, Speed * Time.deltaTime);
        return Vector3.Distance(transform.position, target) < 0.05f;
    }
}
using UnityEngine;

public class Customer : MonoBehaviour
{
    public Table TargetTable;
    public Vector3 ExitPoint;
    public Dish Order;
    public float Speed = 3f;
    public float EatTime = 3f;
    public float FoodScale = 2f;
    public float Patience = 20f;

    enum State { Queueing, GoingToTable, Eating, Leaving }
    enum CustomerType { Normal, VIP, Aceleci }

    State state = State.Queueing;
    CustomerType type = CustomerType.Normal;
    float payMultiplier = 1f;
    float timer;
    float patienceLeft;
    float patienceMax;
    GameObject food;
    Renderer body;

    void Start()
    {
        body = GetComponent<Renderer>();

        float roll = Random.value;
        float patienceMult = 1f;
        Color color = Color.HSVToRGB(Random.value, 0.6f, 0.9f);

        if (roll < GameManager.Instance.VipChance)
        {
            type = CustomerType.VIP;
            payMultiplier = 2f;
            patienceMult = 1.5f;
            color = new Color(1f, 0.84f, 0f);
        }
        else if (roll < GameManager.Instance.VipChance + 0.2f)
        {
            type = CustomerType.Aceleci;
            payMultiplier = 1.3f;
            patienceMult = 0.5f;
            color = new Color(0.6f, 0.3f, 1f);
        }

        patienceLeft = Patience * GameManager.Instance.PatienceBonus * patienceMult;
        patienceMax = patienceLeft;

        if (body != null) body.material.color = color;
    }

    void Update()
    {
        switch (state)
        {
            case State.Queueing:
                UpdateQueue();
                break;

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
                    price = Mathf.RoundToInt(price * GameManager.Instance.PriceFactor * payMultiplier);
                    GameManager.Instance.RecordServed(price, transform.position);
                    TargetTable.IsOccupied = false;
                    state = State.Leaving;
                }
                break;

            case State.Leaving:
                if (MoveTo(ExitPoint)) Destroy(gameObject);
                break;
        }
    }

    void UpdateQueue()
    {
        CustomerSpawner spawner = CustomerSpawner.Instance;
        MoveTo(spawner.GetQueuePosition(this));

        patienceLeft -= Time.deltaTime;
        if (patienceLeft <= 0f)
        {
            spawner.Queue.Remove(this);
            GameManager.Instance.RecordAngry();
            if (body != null) body.material.color = Color.red;
            state = State.Leaving;
            return;
        }

        if (spawner.Queue.Count > 0 && spawner.Queue[0] == this)
        {
            Table t = spawner.GetFreeTable();
            if (t != null)
            {
                t.IsOccupied = true;
                TargetTable = t;
                spawner.Queue.RemoveAt(0);
                state = State.GoingToTable;
            }
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
        float step = Speed * GameManager.Instance.SpeedBonus * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, target, step);
        return Vector3.Distance(transform.position, target) < 0.05f;
    }

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint) return;
        if (type == CustomerType.Normal && state != State.Queueing) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 sp = cam.WorldToScreenPoint(transform.position + Vector3.up * 1.9f);
        if (sp.z < 0f) return;

        float x = sp.x;
        float y = Screen.height - sp.y;

        if (type != CustomerType.Normal)
        {
            GUIStyle s = new GUIStyle(GUI.skin.label);
            s.fontSize = 18;
            s.fontStyle = FontStyle.Bold;
            s.alignment = TextAnchor.MiddleCenter;
            s.normal.textColor = type == CustomerType.VIP ? new Color(1f, 0.84f, 0f) : new Color(0.75f, 0.5f, 1f);
            GUI.Label(new Rect(x - 60f, y - 26f, 120f, 24f), type.ToString(), s);
        }

        if (state == State.Queueing && patienceMax > 0f)
        {
            float ratio = Mathf.Clamp01(patienceLeft / patienceMax);
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(x - 25f, y, 50f, 7f), Texture2D.whiteTexture);
            GUI.color = Color.Lerp(Color.red, Color.green, ratio);
            GUI.DrawTexture(new Rect(x - 25f, y, 50f * ratio, 7f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
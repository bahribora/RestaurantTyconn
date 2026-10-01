using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Ekonomi & İtibar")]
    public float currentMoney = 100f;
    public int currentReputation = 50;
    public int maxReputation = 100;

    [Header("Zaman & Gün Sistemi")]
    public int currentDay = 1;
    public float dayDuration = 90f; // 1 gün kaç saniye sürecek
    private float dayTimer;

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

    private void Start()
    {
        dayTimer = dayDuration;

        // UI Başlangıç Değerlerini Gönder
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateMoneyUI(currentMoney);
            UIManager.Instance.UpdateDayUI(currentDay, dayTimer);
            UIManager.Instance.UpdateReputationUI(currentReputation, maxReputation);
        }
    }

    private void Update()
    {
        // Gün Sayacı Güncellemesi
        if (dayTimer > 0)
        {
            dayTimer -= Time.deltaTime;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateDayUI(currentDay, dayTimer);
            }
        }
        else
        {
            NextDay();
        }
    }

    public void AddMoney(float amount)
    {
        currentMoney += amount;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateMoneyUI(currentMoney);
        }
    }

    public void SpendMoney(float amount)
    {
        currentMoney -= amount;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateMoneyUI(currentMoney);
        }
    }

    public void AddReputation(int amount)
    {
        currentReputation = Mathf.Clamp(currentReputation + amount, 0, maxReputation);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateReputationUI(currentReputation, maxReputation);
        }
    }

    private void NextDay()
    {
        currentDay++;
        dayTimer = dayDuration;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateDayUI(currentDay, dayTimer);
        }
    }
}
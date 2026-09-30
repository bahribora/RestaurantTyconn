using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int Money = 500;
    public int TableCost = 200;
    public GameObject[] LockedTables;

    public float PriceMultiplier = 1f;
    public float EatTimeMultiplier = 1f;

    const int MaxLevel = 3;
    int unlockedCount = 0;
    int priceLevel = 0;
    int speedLevel = 0;

    int LockedCount { get { return LockedTables == null ? 0 : LockedTables.Length; } }
    int PriceUpgradeCost { get { return 300 + priceLevel * 300; } }
    int SpeedUpgradeCost { get { return 250 + speedLevel * 250; } }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void AddMoney(int amount)
    {
        Money += amount;
    }

    public bool TrySpend(int amount)
    {
        if (Money < amount) return false;
        Money -= amount;
        return true;
    }

    void BuyTable()
    {
        if (unlockedCount >= LockedCount) return;
        if (LockedTables[unlockedCount] == null) return;
        if (!TrySpend(TableCost)) return;

        LockedTables[unlockedCount].SetActive(true);
        unlockedCount++;
    }

    void BuyPriceUpgrade()
    {
        if (priceLevel >= MaxLevel) return;
        if (!TrySpend(PriceUpgradeCost)) return;

        priceLevel++;
        PriceMultiplier += 0.25f;
    }

    void BuySpeedUpgrade()
    {
        if (speedLevel >= MaxLevel) return;
        if (!TrySpend(SpeedUpgradeCost)) return;

        speedLevel++;
        EatTimeMultiplier *= 0.8f;
    }

    void OnGUI()
    {
        GUIStyle label = new GUIStyle(GUI.skin.label);
        label.fontSize = 28;
        label.normal.textColor = Color.white;

        GUIStyle button = new GUIStyle(GUI.skin.button);
        button.fontSize = 22;

        GUI.Label(new Rect(20, 20, 400, 50), "Para: " + Money + " TL", label);

        if (unlockedCount >= LockedCount)
        {
            GUI.Label(new Rect(20, 70, 400, 40), "Tüm masalar açıldı", label);
        }
        else
        {
            GUI.enabled = Money >= TableCost;
            if (GUI.Button(new Rect(20, 70, 340, 50), "Masa Al (" + TableCost + " TL)", button))
                BuyTable();
            GUI.enabled = true;
        }

        if (priceLevel >= MaxLevel)
        {
            GUI.Label(new Rect(20, 130, 400, 40), "Fiyat Artışı: MAX", label);
        }
        else
        {
            GUI.enabled = Money >= PriceUpgradeCost;
            string t = "Fiyat +%25 (" + PriceUpgradeCost + " TL) [" + priceLevel + "/" + MaxLevel + "]";
            if (GUI.Button(new Rect(20, 130, 340, 50), t, button))
                BuyPriceUpgrade();
            GUI.enabled = true;
        }

        if (speedLevel >= MaxLevel)
        {
            GUI.Label(new Rect(20, 190, 400, 40), "Hızlı Servis: MAX", label);
        }
        else
        {
            GUI.enabled = Money >= SpeedUpgradeCost;
            string t = "Hızlı Servis (" + SpeedUpgradeCost + " TL) [" + speedLevel + "/" + MaxLevel + "]";
            if (GUI.Button(new Rect(20, 190, 340, 50), t, button))
                BuySpeedUpgrade();
            GUI.enabled = true;
        }
    }
}
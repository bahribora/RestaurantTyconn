using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int Money = 500;

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

    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 28;
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(20, 20, 400, 50), "Para: " + Money + " TL", style);
    }
}
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public string RestaurantName = "Lezzet Durağı";

    // Sadeleştirilmiş Küçük Sayılı Ekonomi
    public float Money = 4f;
    public float TableCost = 3.5f;
    public GameObject[] LockedTables;
    public int Reputation = 50;

    public GameObject[] Decorations;
    public float[] DecorCosts = { 2f, 4.5f, 8f };
    public int DecorReputationBonus = 8;

    public float DayLength = 90f;
    public float BaseDailyRent = 0.5f;

    public float WaiterCost = 4f;
    public float WaiterWage = 0.6f;

    public float PriceMultiplier = 1f;
    public float EatTimeMultiplier = 1f;

    static readonly string[] EventNames =
    {
        "Normal gün",
        "Yağmurlu gün (müşteri az)",
        "Hafta sonu (kalabalık)",
        "Gurme günü (fiyatlar yüksek)"
    };
    static readonly float[] EventSpawn = { 1f, 0.6f, 1.5f, 1f };
    static readonly float[] EventPrice = { 1f, 1f, 1f, 1.3f };

    static readonly string[] AchNames =
    {
        "İlk Adımlar", "Meşhur", "Efsane Restoran", "Zengin", "Kıdemli", "Beş Yıldız", "Tam Kadro"
    };
    static readonly string[] AchDesc =
    {
        "10 müşteri servis et", "50 müşteri servis et", "200 müşteri servis et",
        "Toplam 20 TL kazan", "5. güne ulaş", "5 yıldıza ulaş", "3 garson çalıştır"
    };
    static readonly float[] AchReward = { 1f, 2.5f, 6f, 3f, 2f, 5f, 3f };

    const int MaxLevel = 3;
    const int MaxWaiters = 3;
    float startMoney;
    int startReputation;
    int unlockedCount;
    int unlockedDishes = 1;
    int priceLevel;
    int speedLevel;
    int waiters;
    int decorCount;
    float saveTimer;

    int totalServed;
    float totalEarned;
    bool[] achDone = new bool[AchNames.Length];
    string bannerText = "";
    float bannerTimer;
    bool showAch;
    bool showSettings; // Ayarlar Menüsü Durumu

    // Çözünürlük ve Ayar Seçenekleri
    int selectedResolutionIndex = 0;
    bool isFullscreen = true;
    readonly string[] resolutionNames = { "1920 x 1080", "1600 x 900", "1366 x 768", "1280 x 720" };
    readonly int[] resWidths = { 1920, 1600, 1366, 1280 };
    readonly int[] resHeights = { 1080, 900, 768, 720 };

    int day = 1;
    int currentEvent;
    float dayTimer;
    float dayEarned;
    int dayServed;
    int dayAngry;
    float rentPaid;
    float wagesPaid;
    bool showReport;
    bool inMenu = true;
    bool isPaused = false;

    class Popup
    {
        public Vector3 Pos;
        public string Text;
        public float Age;
    }
    List<Popup> popups = new List<Popup>();

    int LockedCount { get { return LockedTables == null ? 0 : LockedTables.Length; } }
    int DecorTotal { get { return Decorations == null ? 0 : Decorations.Length; } }

    float PriceUpgradeCost { get { return 3.5f + priceLevel * 4f; } }
    float SpeedUpgradeCost { get { return 3f + speedLevel * 3.5f; } }

    public float CurrentDailyRent { get { return BaseDailyRent + (day * 0.15f); } }

    int EffectiveRep { get { return Mathf.Clamp(Reputation + decorCount * DecorReputationBonus, 0, 100); } }

    public float SpawnRateMultiplier { get { return (0.5f + EffectiveRep / 100f) * EventSpawn[currentEvent]; } }
    public float PriceFactor { get { return PriceMultiplier * EventPrice[currentEvent]; } }
    public int Stars { get { return Mathf.Clamp(1 + EffectiveRep / 20, 1, 5); } }
    public float VipChance { get { return 0.06f + 0.04f * Stars; } }
    public int UnlockedDishes { get { return unlockedDishes; } }
    public float PatienceBonus { get { return 1f + 0.4f * waiters; } }
    public float SpeedBonus { get { return 1f + 0.15f * waiters; } }

    float DecorCost(int i)
    {
        if (DecorCosts != null && i < DecorCosts.Length) return DecorCosts[i];
        return 2f + i * 2.5f;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Time.timeScale = 0f;
        startMoney = Money;
        startReputation = Reputation;
        isFullscreen = Screen.fullScreen;
        LoadGame();
    }

    void Start()
    {
        ApplyTables();
        ApplyDecor();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (!inMenu && !showReport)
            {
                if (showSettings) CloseSettings();
                else if (showAch) CloseAch();
                else TogglePause();
            }
        }

        if (bannerTimer > 0f) bannerTimer -= Time.unscaledDeltaTime;

        for (int i = popups.Count - 1; i >= 0; i--)
        {
            popups[i].Age += Time.deltaTime;
            if (popups[i].Age > 1.2f) popups.RemoveAt(i);
        }

        if (inMenu || showReport || showAch || showSettings || isPaused) return;

        dayTimer += Time.deltaTime;
        if (dayTimer >= DayLength) EndDay();

        saveTimer += Time.deltaTime;
        if (saveTimer >= 10f)
        {
            saveTimer = 0f;
            SaveGame();
        }
    }

    void OnApplicationQuit()
    {
        SaveGame();
    }

    void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
    }

    void PlayCoin() { if (AudioManager.Instance != null) AudioManager.Instance.PlayCoin(); }
    void PlayAngry() { if (AudioManager.Instance != null) AudioManager.Instance.PlayAngry(); }
    void PlayBuy() { if (AudioManager.Instance != null) AudioManager.Instance.PlayBuy(); }
    void PlayDayEnd() { if (AudioManager.Instance != null) AudioManager.Instance.PlayDayEnd(); }

    void RollEvent()
    {
        currentEvent = (day <= 1) ? 0 : Random.Range(0, EventNames.Length);
    }

    public void AddMoney(float amount)
    {
        Money += amount;
    }

    public bool TrySpend(float amount)
    {
        if (Money < amount) return false;
        Money -= amount;
        return true;
    }

    public void ChangeReputation(int delta)
    {
        Reputation = Mathf.Clamp(Reputation + delta, 0, 100);
    }

    public void RecordServed(float price, Vector3 pos)
    {
        AddMoney(price);
        ChangeReputation(1);
        dayEarned += price;
        dayServed++;
        totalServed++;
        totalEarned += price;
        PlayCoin();

        Popup p = new Popup();
        p.Pos = pos;
        p.Text = "+" + price.ToString("F1") + " TL";
        popups.Add(p);

        CheckAchievements();
    }

    public void RecordAngry()
    {
        ChangeReputation(-5);
        dayAngry++;
        PlayAngry();
    }

    void CheckAchievements()
    {
        bool[] cond =
        {
            totalServed >= 10,
            totalServed >= 50,
            totalServed >= 200,
            totalEarned >= 20f,
            day >= 5,
            Stars >= 5,
            waiters >= MaxWaiters
        };

        for (int i = 0; i < achDone.Length; i++)
        {
            if (!achDone[i] && cond[i]) UnlockAchievement(i);
        }
    }

    void UnlockAchievement(int i)
    {
        achDone[i] = true;
        AddMoney(AchReward[i]);
        bannerText = "Başarım: " + AchNames[i] + "  (+" + AchReward[i].ToString("F1") + " TL)";
        bannerTimer = 4f;
        PlayDayEnd();
    }

    void EndDay()
    {
        float totalRent = CurrentDailyRent;
        rentPaid = Mathf.Min(totalRent, Money);
        Money -= rentPaid;
        wagesPaid = Mathf.Min(waiters * WaiterWage, Money);
        Money -= wagesPaid;
        showReport = true;
        Time.timeScale = 0f;
        PlayDayEnd();
        SaveGame();
    }

    void StartNextDay()
    {
        day++;
        RollEvent();
        dayTimer = 0f;
        dayEarned = 0f;
        dayServed = 0;
        dayAngry = 0;
        rentPaid = 0f;
        wagesPaid = 0f;
        showReport = false;
        Time.timeScale = 1f;
        PlayBuy();
        CheckAchievements();
        SaveGame();
    }

    void StartGame()
    {
        inMenu = false;
        isPaused = false;
        showSettings = false;
        Time.timeScale = 1f;
        PlayBuy();
    }

    void ReturnToMainMenu()
    {
        SaveGame();
        inMenu = true;
        isPaused = false;
        showReport = false;
        showAch = false;
        showSettings = false;
        Time.timeScale = 0f;
    }

    void QuitGame()
    {
        SaveGame();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OpenAch()
    {
        showAch = true;
        Time.timeScale = 0f;
    }

    void CloseAch()
    {
        showAch = false;
        Time.timeScale = isPaused ? 0f : 1f;
    }

    void OpenSettings()
    {
        showSettings = true;
        Time.timeScale = 0f;
    }

    void CloseSettings()
    {
        showSettings = false;
        Time.timeScale = (inMenu || isPaused) ? 0f : 1f;
    }

    void ApplyUpgrades()
    {
        PriceMultiplier = 1f + 0.25f * priceLevel;
        EatTimeMultiplier = Mathf.Pow(0.8f, speedLevel);
    }

    void ApplyTables()
    {
        for (int i = 0; i < LockedCount; i++)
        {
            if (LockedTables[i] != null)
                LockedTables[i].SetActive(i < unlockedCount);
        }
    }

    void ApplyDecor()
    {
        for (int i = 0; i < DecorTotal; i++)
        {
            if (Decorations[i] != null)
                Decorations[i].SetActive(i < decorCount);
        }
    }

    void SaveGame()
    {
        int mask = 0;
        for (int i = 0; i < achDone.Length; i++)
        {
            if (achDone[i]) mask |= 1 << i;
        }

        PlayerPrefs.SetFloat("money", Money);
        PlayerPrefs.SetInt("tables", unlockedCount);
        PlayerPrefs.SetInt("dishes", unlockedDishes);
        PlayerPrefs.SetInt("priceLevel", priceLevel);
        PlayerPrefs.SetInt("speedLevel", speedLevel);
        PlayerPrefs.SetInt("waiters", waiters);
        PlayerPrefs.SetInt("decor", decorCount);
        PlayerPrefs.SetInt("rep", Reputation);
        PlayerPrefs.SetInt("day", day);
        PlayerPrefs.SetInt("totalServed", totalServed);
        PlayerPrefs.SetFloat("totalEarned", totalEarned);
        PlayerPrefs.SetInt("ach", mask);
        PlayerPrefs.Save();
    }

    void LoadGame()
    {
        Money = PlayerPrefs.GetFloat("money", 4f); // Varsayılan 4 TL
        unlockedCount = Mathf.Clamp(PlayerPrefs.GetInt("tables", 0), 0, LockedCount);
        unlockedDishes = Mathf.Max(1, PlayerPrefs.GetInt("dishes", 1));
        priceLevel = Mathf.Clamp(PlayerPrefs.GetInt("priceLevel", 0), 0, MaxLevel);
        speedLevel = Mathf.Clamp(PlayerPrefs.GetInt("speedLevel", 0), 0, MaxLevel);
        waiters = Mathf.Clamp(PlayerPrefs.GetInt("waiters", 0), 0, MaxWaiters);
        decorCount = Mathf.Clamp(PlayerPrefs.GetInt("decor", 0), 0, DecorTotal);
        Reputation = Mathf.Clamp(PlayerPrefs.GetInt("rep", startReputation), 0, 100);
        day = Mathf.Max(1, PlayerPrefs.GetInt("day", 1));
        totalServed = PlayerPrefs.GetInt("totalServed", 0);
        totalEarned = PlayerPrefs.GetFloat("totalEarned", 0f);

        int mask = PlayerPrefs.GetInt("ach", 0);
        for (int i = 0; i < achDone.Length; i++)
        {
            achDone[i] = (mask & (1 << i)) != 0;
        }

        ApplyUpgrades();
        RollEvent();
    }

    void ResetGame()
    {
        PlayerPrefs.DeleteAll();
        Money = 4f; // SIFIRLANDIĞINDA 4 TL YAPAR
        Reputation = startReputation;
        unlockedCount = 0;
        unlockedDishes = 1;
        priceLevel = 0;
        speedLevel = 0;
        waiters = 0;
        decorCount = 0;
        totalServed = 0;
        totalEarned = 0f;
        for (int i = 0; i < achDone.Length; i++) achDone[i] = false;
        bannerTimer = 0f;
        day = 1;
        dayTimer = 0f;
        dayEarned = 0f;
        dayServed = 0;
        dayAngry = 0;
        showReport = false;
        showAch = false;
        showSettings = false;
        isPaused = false;
        Time.timeScale = inMenu ? 0f : 1f;
        ApplyUpgrades();
        ApplyTables();
        ApplyDecor();
        RollEvent();
        SaveGame();
    }

    void BuyTable()
    {
        if (unlockedCount >= LockedCount) return;
        if (LockedTables[unlockedCount] == null) return;
        if (!TrySpend(TableCost)) return;

        LockedTables[unlockedCount].SetActive(true);
        unlockedCount++;
        PlayBuy();
        SaveGame();
    }

    void BuyDish()
    {
        CustomerSpawner sp = CustomerSpawner.Instance;
        if (sp == null || sp.Menu == null) return;
        if (unlockedDishes >= sp.Menu.Length) return;
        if (!TrySpend(sp.Menu[unlockedDishes].UnlockCost)) return;

        unlockedDishes++;
        PlayBuy();
        SaveGame();
    }

    void BuyWaiter()
    {
        if (waiters >= MaxWaiters) return;
        if (!TrySpend(WaiterCost)) return;

        waiters++;
        PlayBuy();
        CheckAchievements();
        SaveGame();
    }

    void BuyDecor()
    {
        if (decorCount >= DecorTotal) return;
        if (!TrySpend(DecorCost(decorCount))) return;

        if (Decorations[decorCount] != null) Decorations[decorCount].SetActive(true);
        decorCount++;
        PlayBuy();
        CheckAchievements();
        SaveGame();
    }

    void BuyPriceUpgrade()
    {
        if (priceLevel >= MaxLevel) return;
        if (!TrySpend(PriceUpgradeCost)) return;

        priceLevel++;
        ApplyUpgrades();
        PlayBuy();
        SaveGame();
    }

    void BuySpeedUpgrade()
    {
        if (speedLevel >= MaxLevel) return;
        if (!TrySpend(SpeedUpgradeCost)) return;

        speedLevel++;
        ApplyUpgrades();
        PlayBuy();
        SaveGame();
    }

    string MusicText()
    {
        bool on = AudioManager.Instance != null && AudioManager.Instance.MusicOn;
        return on ? "Müzik: Açık" : "Müzik: Kapalı";
    }

    void ToggleMusic()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.ToggleMusic();
    }

    void DrawPopups()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        GUIStyle s = new GUIStyle(GUI.skin.label);
        s.fontSize = 26;
        s.fontStyle = FontStyle.Bold;
        s.alignment = TextAnchor.MiddleCenter;

        foreach (Popup p in popups)
        {
            Vector3 sp = cam.WorldToScreenPoint(p.Pos + Vector3.up * 2f);
            if (sp.z < 0f) continue;

            float y = Screen.height - sp.y - p.Age * 50f;
            s.normal.textColor = new Color(0.4f, 1f, 0.4f, 1f - p.Age / 1.2f);
            GUI.Label(new Rect(sp.x - 100f, y - 20f, 200f, 40f), p.Text, s);
        }
    }

    void DrawBanner()
    {
        if (bannerTimer <= 0f) return;

        GUIStyle s = new GUIStyle(GUI.skin.label);
        s.fontSize = 30;
        s.fontStyle = FontStyle.Bold;
        s.alignment = TextAnchor.MiddleCenter;
        s.normal.textColor = new Color(1f, 0.85f, 0.2f);
        GUI.Label(new Rect(Screen.width / 2f - 400f, Screen.height - 90f, 800f, 50f), bannerText, s);
    }

    void DrawMenu(GUIStyle label, GUIStyle button)
    {
        Rect box = new Rect(1920 / 2f - 260, 1080 / 2f - 270, 520, 540);
        GUI.Box(box, "");
        GUI.Box(box, "");

        GUIStyle title = new GUIStyle(label);
        title.alignment = TextAnchor.MiddleCenter;
        title.fontSize = 44;
        title.fontStyle = FontStyle.Bold;

        GUIStyle sub = new GUIStyle(label);
        sub.alignment = TextAnchor.MiddleCenter;
        sub.fontSize = 22;

        GUI.Label(new Rect(box.x, box.y + 25, box.width, 70), RestaurantName, title);
        GUI.Label(new Rect(box.x, box.y + 90, box.width, 40), "Restoran Tycoon", sub);

        string startText = day > 1 ? "Devam Et (Gün " + day + ")" : "Oyuna Başla";
        if (GUI.Button(new Rect(box.x + 80, box.y + 150, 360, 55), startText, button))
            StartGame();

        if (GUI.Button(new Rect(box.x + 80, box.y + 220, 360, 55), "Ayarlar (Çözünürlük)", button))
            OpenSettings();

        if (GUI.Button(new Rect(box.x + 80, box.y + 290, 360, 55), "Kaydı Sıfırla", button))
            ResetGame();

        if (GUI.Button(new Rect(box.x + 80, box.y + 360, 360, 55), MusicText(), button))
            ToggleMusic();

        if (GUI.Button(new Rect(box.x + 80, box.y + 430, 360, 55), "Çıkış", button))
            QuitGame();
    }

    void DrawPauseMenu(GUIStyle label, GUIStyle button)
    {
        Rect box = new Rect(1920 / 2f - 220, 1080 / 2f - 270, 440, 540);
        GUI.Box(box, "");
        GUI.Box(box, "");

        GUIStyle title = new GUIStyle(label);
        title.alignment = TextAnchor.MiddleCenter;
        title.fontSize = 38;
        title.fontStyle = FontStyle.Bold;

        GUI.Label(new Rect(box.x, box.y + 25, box.width, 60), "OYUN DURDURULDU", title);

        if (GUI.Button(new Rect(box.x + 50, box.y + 95, 340, 55), "Devam Et", button))
            TogglePause();

        if (GUI.Button(new Rect(box.x + 50, box.y + 165, 340, 55), "Ayarlar", button))
            OpenSettings();

        if (GUI.Button(new Rect(box.x + 50, box.y + 235, 340, 55), MusicText(), button))
            ToggleMusic();

        if (GUI.Button(new Rect(box.x + 50, box.y + 305, 340, 55), "Kaydı Sıfırla", button))
            ResetGame();

        if (GUI.Button(new Rect(box.x + 50, box.y + 375, 340, 55), "Ana Menüye Dön", button))
            ReturnToMainMenu();

        if (GUI.Button(new Rect(box.x + 50, box.y + 445, 340, 55), "Çıkış", button))
            QuitGame();
    }

    void DrawSettingsMenu(GUIStyle label, GUIStyle button)
    {
        Rect box = new Rect(1920 / 2f - 250, 1080 / 2f - 220, 500, 440);
        GUI.Box(box, "");
        GUI.Box(box, "");

        GUIStyle title = new GUIStyle(label);
        title.alignment = TextAnchor.MiddleCenter;
        title.fontSize = 36;
        title.fontStyle = FontStyle.Bold;

        GUI.Label(new Rect(box.x, box.y + 20, box.width, 50), "AYARLAR", title);

        GUIStyle line = new GUIStyle(label);
        line.fontSize = 22;
        GUI.Label(new Rect(box.x + 40, box.y + 90, 200, 40), "Çözünürlük:", line);

        if (GUI.Button(new Rect(box.x + 200, box.y + 85, 250, 45), resolutionNames[selectedResolutionIndex], button))
        {
            selectedResolutionIndex = (selectedResolutionIndex + 1) % resolutionNames.Length;
        }

        string fsText = isFullscreen ? "Tam Ekran: EVET" : "Tam Ekran: HAYIR";
        if (GUI.Button(new Rect(box.x + 100, box.y + 155, 300, 50), fsText, button))
        {
            isFullscreen = !isFullscreen;
        }

        if (GUI.Button(new Rect(box.x + 100, box.y + 230, 300, 55), "Uygula", button))
        {
            int w = resWidths[selectedResolutionIndex];
            int h = resHeights[selectedResolutionIndex];
            Screen.SetResolution(w, h, isFullscreen);
        }

        if (GUI.Button(new Rect(box.x + 100, box.y + 310, 300, 55), "Kapat", button))
        {
            CloseSettings();
        }
    }

    void DrawReport(GUIStyle label, GUIStyle button)
    {
        Rect box = new Rect(1920 / 2f - 240, 1080 / 2f - 250, 480, 500);
        GUI.Box(box, "");
        GUI.Box(box, "");

        GUIStyle title = new GUIStyle(label);
        title.alignment = TextAnchor.MiddleCenter;
        title.fontSize = 34;

        GUIStyle line = new GUIStyle(label);
        line.fontSize = 24;

        float x = box.x + 30;
        float y = box.y + 20;

        GUI.Label(new Rect(box.x, y, box.width, 50), "Gün " + day + " Bitti", title);
        GUI.Label(new Rect(x, y + 65, 430, 40), EventNames[currentEvent], line);
        GUI.Label(new Rect(x, y + 105, 430, 40), "Kazanç: +" + dayEarned.ToString("F1") + " TL", line);
        GUI.Label(new Rect(x, y + 145, 430, 40), "Servis edilen: " + dayServed, line);
        GUI.Label(new Rect(x, y + 185, 430, 40), "Kızan müşteri: " + dayAngry, line);
        GUI.Label(new Rect(x, y + 225, 430, 40), "Günlük kira: -" + rentPaid.ToString("F1") + " TL", line);
        GUI.Label(new Rect(x, y + 265, 430, 40), "Maaşlar: -" + wagesPaid.ToString("F1") + " TL", line);
        GUI.Label(new Rect(x, y + 305, 430, 40), "İtibar: " + Reputation + "/100  |  Yıldız: " + Stars + "/5", line);

        if (GUI.Button(new Rect(box.x + 90, box.y + box.height - 75, 300, 55), "Sonraki Gün", button))
            StartNextDay();
    }

    void DrawAchievements(GUIStyle label, GUIStyle button)
    {
        Rect box = new Rect(1920 / 2f - 320, 1080 / 2f - 260, 640, 520);
        GUI.Box(box, "");
        GUI.Box(box, "");

        GUIStyle title = new GUIStyle(label);
        title.alignment = TextAnchor.MiddleCenter;
        title.fontSize = 34;

        GUIStyle line = new GUIStyle(label);
        line.fontSize = 21;

        GUI.Label(new Rect(box.x, box.y + 15, box.width, 50), "Başarımlar", title);

        for (int i = 0; i < AchNames.Length; i++)
        {
            line.normal.textColor = achDone[i] ? new Color(0.4f, 1f, 0.4f) : Color.white;
            string mark = achDone[i] ? "[X] " : "[  ] ";
            string text = mark + AchNames[i] + " - " + AchDesc[i] + " (+" + AchReward[i].ToString("F1") + " TL)";
            GUI.Label(new Rect(box.x + 25, box.y + 75 + i * 46, box.width - 40, 40), text, line);
        }

        if (GUI.Button(new Rect(box.x + 170, box.y + box.height - 75, 300, 55), "Kapat", button))
            CloseAch();
    }

    void OnGUI()
    {
        float targetWidth = 1920f;
        float targetHeight = 1080f;

        float scaleX = Screen.width / targetWidth;
        float scaleY = Screen.height / targetHeight;
        Matrix4x4 svMat = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scaleX, scaleY, 1f));

        GUIStyle label = new GUIStyle(GUI.skin.label);
        label.fontSize = 28;
        label.normal.textColor = Color.white;

        GUIStyle button = new GUIStyle(GUI.skin.button);
        button.fontSize = 22;

        DrawPopups();
        DrawBanner();

        if (showSettings)
        {
            DrawSettingsMenu(label, button);
            GUI.matrix = svMat;
            return;
        }

        if (inMenu)
        {
            DrawMenu(label, button);
            GUI.matrix = svMat;
            return;
        }

        if (isPaused)
        {
            DrawPauseMenu(label, button);
            GUI.matrix = svMat;
            return;
        }

        GUI.Label(new Rect(20, 20, 400, 50), "Para: " + Money.ToString("F1") + " TL", label);
        GUI.Label(new Rect(380, 20, 400, 50), "İtibar: " + Reputation + "/100", label);

        if (showReport)
        {
            DrawReport(label, button);
            GUI.matrix = svMat;
            return;
        }

        if (showAch)
        {
            DrawAchievements(label, button);
            GUI.matrix = svMat;
            return;
        }

        int remaining = Mathf.CeilToInt(DayLength - dayTimer);
        GUI.Label(new Rect(380, 70, 500, 50), "Gün " + day + "  |  " + remaining + " sn", label);

        GUIStyle small = new GUIStyle(label);
        small.fontSize = 22;
        GUI.Label(new Rect(380, 115, 600, 40), EventNames[currentEvent], small);
        GUI.Label(new Rect(380, 150, 600, 40), "Yıldız: " + Stars + "/5  (dekor bonusu +" + (decorCount * DecorReputationBonus) + ")", small);

        if (unlockedCount >= LockedCount)
        {
            GUI.Label(new Rect(20, 70, 400, 40), "Tüm masalar açıldı", label);
        }
        else
        {
            GUI.enabled = Money >= TableCost;
            if (GUI.Button(new Rect(20, 70, 340, 50), "Masa Al (" + TableCost.ToString("F1") + " TL)", button))
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
            string t = "Fiyat +%25 (" + PriceUpgradeCost.ToString("F1") + " TL) [" + priceLevel + "/" + MaxLevel + "]";
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
            string t = "Hızlı Servis (" + SpeedUpgradeCost.ToString("F1") + " TL) [" + speedLevel + "/" + MaxLevel + "]";
            if (GUI.Button(new Rect(20, 190, 340, 50), t, button))
                BuySpeedUpgrade();
            GUI.enabled = true;
        }

        CustomerSpawner spawner = CustomerSpawner.Instance;
        if (spawner != null && spawner.Menu != null && spawner.Menu.Length > 0)
        {
            if (unlockedDishes >= spawner.Menu.Length)
            {
                GUI.Label(new Rect(20, 250, 400, 40), "Menü tamam", label);
            }
            else
            {
                Dish next = spawner.Menu[unlockedDishes];
                GUI.enabled = Money >= next.UnlockCost;
                string t = "Yeni Yemek: " + next.Name + " (" + next.UnlockCost.ToString("F1") + " TL)";
                if (GUI.Button(new Rect(20, 250, 340, 50), t, button))
                    BuyDish();
                GUI.enabled = true;
            }
        }

        if (waiters >= MaxWaiters)
        {
            GUI.Label(new Rect(20, 310, 400, 40), "Garson: MAX", label);
        }
        else
        {
            GUI.enabled = Money >= WaiterCost;
            string t = "Garson Al (" + WaiterCost.ToString("F1") + " TL, maaş " + WaiterWage.ToString("F1") + ") [" + waiters + "/" + MaxWaiters + "]";
            if (GUI.Button(new Rect(20, 310, 480, 50), t, button))
                BuyWaiter();
            GUI.enabled = true;
        }

        if (DecorTotal == 0)
        {
            GUI.Label(new Rect(20, 370, 400, 40), "Dekor yok (sahneye ekle)", small);
        }
        else if (decorCount >= DecorTotal)
        {
            GUI.Label(new Rect(20, 370, 400, 40), "Dekorasyon: MAX", label);
        }
        else
        {
            float cost = DecorCost(decorCount);
            GUI.enabled = Money >= cost;
            string t = "Dekor Al (" + cost.ToString("F1") + " TL) [" + decorCount + "/" + DecorTotal + "]";
            if (GUI.Button(new Rect(20, 370, 340, 50), t, button))
                BuyDecor();
            GUI.enabled = true;
        }

        if (GUI.Button(new Rect(20, 440, 160, 40), "Kaydı Sıfırla"))
            ResetGame();

        if (GUI.Button(new Rect(190, 440, 180, 40), MusicText()))
            ToggleMusic();

        if (GUI.Button(new Rect(380, 440, 150, 40), "Ayarlar"))
            OpenSettings();

        if (GUI.Button(new Rect(540, 440, 160, 40), "Başarımlar"))
            OpenAch();

        GUI.matrix = svMat;
    }
}
using Fusion;
using TMPro;
using UnityEngine;

public class GameTimerController : NetworkBehaviour
{
    [SerializeField] private float totalGameSeconds = 180f;
    [SerializeField] private TextMeshProUGUI txtTimer;
    [SerializeField] private string timerObjectName = "txtTimer";

    [Networked] private float NetRemainingSeconds { get; set; }
    [Networked] private NetworkBool NetTimerRunning { get; set; }
    [Networked] private double NetStartTime { get; set; }

    public float RemainingSeconds { get; private set; }
    public bool IsTimerRunning => NetTimerRunning;
    public bool IsExpired => RemainingSeconds <= 0f;

    public bool IsGameplayActive { get; set; }
    public bool HasEnded { get; set; }

    private bool hasSpawned;

    public void Configure(float totalSeconds, TextMeshProUGUI timerLabel)
    {
        if (totalSeconds > 0f)
        {
            totalGameSeconds = totalSeconds;
        }

        if (timerLabel != null)
        {
            txtTimer = timerLabel;
        }
    }

    public void InitializeTimerText()
    {
        if (txtTimer != null)
        {
            return;
        }

        var timerObject = GameObject.Find(timerObjectName);
        if (timerObject != null)
        {
            txtTimer = timerObject.GetComponent<TextMeshProUGUI>();
        }

        Debug.LogWarning("[GameTimerController] txtTimer was not assigned. Found by name. Consider using a tag.");
    }

    public void ResetTimer(bool hasSpawned)
    {
        this.hasSpawned = hasSpawned;
        RemainingSeconds = totalGameSeconds;
        if (Object != null && Object.HasStateAuthority)
        {
            NetRemainingSeconds = totalGameSeconds;
            NetTimerRunning = false;
            NetStartTime = Runner != null ? Runner.SimulationTime : 0d;
        }
    }

    public void TickTimerUI()
    {
        RemainingSeconds = hasSpawned ? NetRemainingSeconds : totalGameSeconds;

        if (txtTimer == null)
        {
            return;
        }

        int minutes = Mathf.FloorToInt(RemainingSeconds / 60f);
        int seconds = Mathf.FloorToInt(RemainingSeconds % 60f);
        txtTimer.text = $"{minutes:00}:{seconds:00}";
        if (RemainingSeconds <= 0f)
        {
            txtTimer.text = "00:00";
        }
    }

    public override void Spawned()
    {
        hasSpawned = true;
        if (Object != null && Object.HasStateAuthority)
        {
            NetRemainingSeconds = totalGameSeconds;
            NetTimerRunning = false;
            NetStartTime = Runner != null ? Runner.SimulationTime : 0d;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object == null || Object.HasStateAuthority == false)
        {
            return;
        }

        if (HasEnded || IsGameplayActive == false)
        {
            return;
        }

        if (NetTimerRunning == false)
        {
            NetTimerRunning = AreAnyPlayersPresent();
            if (NetTimerRunning == false)
            {
                NetRemainingSeconds = totalGameSeconds;
                NetStartTime = Runner.SimulationTime;
                return;
            }

            NetStartTime = Runner.SimulationTime;
        }

        double elapsed = Runner.SimulationTime - NetStartTime;
        NetRemainingSeconds = Mathf.Max(0f, totalGameSeconds - (float)elapsed);
    }

    private bool AreAnyPlayersPresent()
    {
        if (Runner == null || Runner.IsRunning == false)
        {
            return false;
        }

        int activeCount = 0;
        foreach (var player in Runner.ActivePlayers)
        {
            activeCount++;
        }

        return activeCount > 0;
    }
}

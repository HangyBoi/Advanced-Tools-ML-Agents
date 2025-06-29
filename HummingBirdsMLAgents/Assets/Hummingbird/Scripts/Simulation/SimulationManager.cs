using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SimulationManager : MonoBehaviour
{
    public static SimulationManager Instance { get; private set; }

    public FlowerArea flowerArea;
    public int flowerCount = 8;
    public float agentInitialEnergy = 25f;

    private List<HummingbirdAgent> allAgents;
    private List<HummingbirdAgent> activeAgents;
    private bool isEpisodeEnding = false;

    private void Awake()
    {
        // --- DIAGNOSTIC LOGGING ---
        Debug.Log($"<color=cyan>SimulationManager Awake() called by object: {this.gameObject.name}, Instance ID: {this.GetInstanceID()}</color>");

        if (Instance != null && Instance != this)
        {
            // --- DIAGNOSTIC LOGGING ---
            Debug.LogError($"<color=orange>DUPLICATE SimulationManager DETECTED! Destroying this instance ({this.GetInstanceID()}). The original is {Instance.GetInstanceID()}.</color>");
            Destroy(this.gameObject);
            return; // Stop execution here
        }

        Instance = this;
        // DontDestroyOnLoad(this.gameObject); // Optional: uncomment if you switch between scenes.

        allAgents = new List<HummingbirdAgent>(FindObjectsOfType<HummingbirdAgent>());
    }

    private void Start()
    {
        Debug.Log($"<color=cyan>SimulationManager Start() on Instance ID: {this.GetInstanceID()}. Starting first episode.</color>");
        StartNewEpisode();
    }

    public void AgentDied(HummingbirdAgent deadAgent)
    {
        // --- DIAGNOSTIC LOGGING ---
        Debug.Log($"<color=yellow>AgentDied reported to Manager ID: {this.GetInstanceID()}. Current active agents: {activeAgents.Count}. isEpisodeEnding: {isEpisodeEnding}</color>");

        if (isEpisodeEnding) return;

        if (activeAgents.Contains(deadAgent))
        {
            activeAgents.Remove(deadAgent);
        }

        if (activeAgents.Count <= 1)
        {
            isEpisodeEnding = true;
            HummingbirdAgent winner = activeAgents.FirstOrDefault();

            // --- DIAGNOSTIC LOGGING ---
            Debug.Log($"<color=yellow>WIN CONDITION MET. Manager ID: {this.GetInstanceID()} is starting EndEpisodeRoutine.</color>");
            StartCoroutine(EndEpisodeRoutine(winner));
        }
    }

    private void StartNewEpisode()
    {
        Debug.Log($"<color=lime>--- StartNewEpisode CALLED on Manager ID: {this.GetInstanceID()} ---</color>");
        isEpisodeEnding = false;

        if (flowerArea.Flowers == null || flowerArea.Flowers.Count == 0)
        {
            Debug.LogError("Flower list is empty! Cannot reset flowers.");
            return;
        }

        if (flowerCount > 0) { flowerArea.ResetAndEnableRandomFlowers(flowerCount); }
        else { flowerArea.ResetFlowers(); }

        activeAgents = new List<HummingbirdAgent>(allAgents);

        Debug.Log($"<color=lime>--- New Episode has been set up. Active agents: {activeAgents.Count} ---</color>");
    }

    private IEnumerator EndEpisodeRoutine(HummingbirdAgent winner)
    {
        // --- DIAGNOSTIC LOGGING ---
        Debug.Log($"<color=magenta>EndEpisodeRoutine has BEGUN on Manager ID: {this.GetInstanceID()}.</color>");

        if (winner != null)
        {
            Debug.Log($"<color=green>Winner is {winner.name}! Assigning +1.0 reward.</color>");
            winner.AddReward(1.0f);
        }
        else { Debug.Log("<color=orange>Episode ended in a draw.</color>"); }

        foreach (var agent in allAgents)
        {
            agent.EndEpisode();
        }

        yield return new WaitForEndOfFrame();

        // --- DIAGNOSTIC LOGGING ---
        Debug.Log($"<color=magenta>EndEpisodeRoutine has finished waiting and will now start a new episode. Manager ID: {this.GetInstanceID()}</color>");
        StartNewEpisode();
    }
}
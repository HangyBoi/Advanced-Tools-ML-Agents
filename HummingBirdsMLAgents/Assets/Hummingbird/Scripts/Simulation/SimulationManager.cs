using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
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
        // Sort the list to ensure a consistent "first" agent
        allAgents = allAgents.OrderBy(a => a.gameObject.GetInstanceID()).ToList();
    }

    private void Start()
    {
        // On the very first run, we need to set up the episode.
        OnNewEpisodeBegan();
    }

    /// <summary>
    /// Called by a "leader" agent from OnEpisodeBegin to reset shared resources.
    /// This is the single source of truth for starting a new episode setup.
    /// </summary>
    public void OnNewEpisodeBegan()
    {
        Debug.Log($"<color=lime>--- OnNewEpisodeBegan CALLED on Manager. Resetting environment. ---</color>");
        isEpisodeEnding = false;

        // Reset and spawn flowers
        if (flowerCount > 0) { flowerArea.ResetAndEnableRandomFlowers(flowerCount); }
        else { flowerArea.ResetFlowers(); }

        // Refill the list of active agents for the new round
        activeAgents = new List<HummingbirdAgent>(allAgents);

        Debug.Log($"<color=lime>--- New Episode has been set up. Active agents: {activeAgents.Count} ---</color>");
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

        // Check for win condition
        if (activeAgents.Count <= 1)
        {
            isEpisodeEnding = true; // Close the gate immediately
            StartCoroutine(EndEpisodeRoutine());
        }
    }

    /// <summary>
    /// Checks if a given agent is the first in the consistent, sorted list.
    /// </summary>
    public bool IsFirstAgent(HummingbirdAgent agent)
    {
        return allAgents.Count > 0 && agent == allAgents[0];
    }

    private IEnumerator EndEpisodeRoutine()
    {
        // --- DIAGNOSTIC LOGGING ---
        Debug.Log($"<color=magenta>EndEpisodeRoutine has BEGUN.</color>");

        HummingbirdAgent winner = activeAgents.FirstOrDefault();
        if (winner != null)
        {
            Debug.Log($"<color=green>Winner is {winner.name}! Assigning +1.0 reward.</color>");

            winner.FreezeAgent();
            winner.AddReward(1.0f);

            // --- STATS RECORDING ---
            // Record the survival time for the WINNER.
            Academy.Instance.StatsRecorder.Add("survival/time_steps", winner.StepCount);
            // -----------------------
        }
        else
        {
            Debug.Log("<color=orange>Episode ended in a draw or with no survivors.</color>");
        }

        // Wait a frame to ensure rewards are processed before the reset.
        yield return new WaitForEndOfFrame();

        // --- THE CRITICAL STEP ---
        // End the episode for the WINNER. If there's a draw, end it for any agent.
        // This single call will trigger OnEpisodeBegin() for ALL agents, starting the
        // new, correct lifecycle chain.
        if (winner != null)
        {
            winner.EndEpisode();
        }
        else if (allAgents.Count > 0)
        {
            // Pick any agent to end the episode if there's a draw.
            // We need to unfreeze it temporarily just to call EndEpisode, as a frozen agent might not respond.
            // This is an edge case, but good to handle.
            HummingbirdAgent agentToReset = allAgents.First(a => a.gameObject.activeInHierarchy);
            if (agentToReset.frozen) agentToReset.UnfreezeAgent(); // Temporary unfreeze
            agentToReset.EndEpisode();
        }

        Debug.Log($"<color=magenta>EndEpisodeRoutine has finished. Called EndEpisode() to trigger the next round.</color>");
    }
}
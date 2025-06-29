using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using UnityEngine;

public class SimulationManager : MonoBehaviour
{
    public static SimulationManager Instance { get; private set; }

    public List<FlowerArea> flowerAreas;
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
        // Subscribe to lesson changes
        Academy.Instance.OnEnvironmentReset += ApplyCurriculum;
    }

    private void ApplyCurriculum()
    {
        // Get the current lesson number from the Academy
        float lesson = Academy.Instance.EnvironmentParameters.GetWithDefault("lesson", 0f);

        // Announce the new lesson in the console for easy debugging
        Debug.Log($"<color=cyan>--- Applying Lesson: {lesson} ---</color>");

        // Let's assume ~180 total flowers per island.
        if (lesson < 0.33f)
        {
            // === LESSON 1: "Abundant Paradise" ===
            // Goal: Teach the absolute basic link: FLOWER -> ENERGY -> SURVIVAL
            // Over half the flowers are active. The world is saturated with resources.
            flowerCount = 100; // Approx. 55% of flowers are active. Very high chance of success.
            agentInitialEnergy = 50; // Very generous energy. Agents can make many mistakes.
            Debug.Log("<color=green>Difficulty set to EASY: 100 Flowers, 50 Energy</color>");
        }
        else if (lesson < 0.66f)
        {
            // === LESSON 2: "The Search Begins" ===
            // Goal: Teach agents to actively SEARCH for flowers, not just wander.
            // The density is lowered significantly, requiring purposeful flight.
            flowerCount = 45; // Approx. 25% of flowers active. Searching is now a required skill.
            agentInitialEnergy = 35; // Less energy, mistakes are more costly.
            Debug.Log("<color=orange>Difficulty set to MEDIUM: 45 Flowers, 35 Energy</color>");
        }
        else
        {
            // === LESSON 3: "Competitive Scarcity" ===
            // Goal: Teach agents to compete and manage energy efficiently.
            // This is our target difficulty for the final evaluation. Flowers are a contested resource.
            flowerCount = 20; // Approx. 11% active. This is now a truly competitive environment.
            agentInitialEnergy = 25; // Low starting energy. Every action matters.
            Debug.Log("<color=red>Difficulty set to HARD: 20 Flowers, 25 Energy</color>");
        }
    }

    /// <summary>
    /// Called by a "leader" agent from OnEpisodeBegin to reset shared resources.
    /// This is the single source of truth for starting a new episode setup.
    /// </summary>
    public void OnNewEpisodeBegan()
    {
        Debug.Log($"<color=lime>--- OnNewEpisodeBegan CALLED on Manager. Resetting environment. ---</color>");
        isEpisodeEnding = false;

        // Loop through every FlowerArea and reset it
        foreach (var area in flowerAreas)
        {
            if (flowerCount > 0) { area.ResetAndEnableRandomFlowers(flowerCount); }
            else { area.ResetFlowers(); }
        }

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
            winner.AddReward(1.0f);

            winner.FreezeAgent();

            // --- STATS RECORDING ---
            // Record the survival time for the WINNER.
            Academy.Instance.StatsRecorder.Add("survival/time_steps", winner.StepCount);

            // Record the nectar obtained by the WINNER.
            Academy.Instance.StatsRecorder.Add("stats/NectarObtained", winner.NectarObtained);
            if (winner.StepCount > 0)
            {
                Academy.Instance.StatsRecorder.Add("stats/EnergyEfficiency", winner.NectarObtained / winner.StepCount);
            }
            // ----------------
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
            // If there's a draw (0 agents left), we still need to trigger the reset.
            // Pick the first agent from the master list to do it.
            allAgents[0].EndEpisode();
        }
        
        Debug.Log($"<color=magenta>EndEpisodeRoutine has finished. Called EndEpisode() to trigger the next round.</color>");
    }
}
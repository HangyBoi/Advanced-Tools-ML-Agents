// SimulationManager.cs

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages the simulation state for the competitive multi-agent environment.
/// Responsibilities:
/// - Tracks all active agents.
/// - Checks for the win/end condition (e.g., last agent standing).
/// - Manages the episode lifecycle (starting and ending episodes for all agents).
/// - Acts as a central point for agents to get information about the environment.
/// </summary>
public class SimulationManager : MonoBehaviour
{
    // --- SINGLETON PATTERN ---
    // This makes it easy for any other script to access the SimulationManager without
    // needing a direct reference. E.g., SimulationManager.Instance.DoSomething();
    public static SimulationManager Instance { get; private set; }

    [Tooltip("Reference to the FlowerArea in the scene.")]
    public FlowerArea flowerArea;

    // A list of all agents in the simulation, populated at the start.
    private List<HummingbirdAgent> allAgents;

    // A list of agents that are still "alive" in the current episode.
    private List<HummingbirdAgent> activeAgents;

    // --- UNITY LIFECYCLE METHODS ---

    private void Awake()
    {
        // Implement the Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        // Find all HummingbirdAgent components in children of this GameObject's parent
        // This assumes the manager and all agents live under a common parent object.
        allAgents = new List<HummingbirdAgent>(transform.parent.GetComponentsInChildren<HummingbirdAgent>());
        activeAgents = new List<HummingbirdAgent>();
    }

    private void Start()
    {
        // Subscribe to the agent's death event for all agents
        foreach (var agent in allAgents)
        {
            agent.OnAgentDied += HandleAgentDeath;
        }

        // Start the first episode
        StartEpisode();
    }

    // --- PUBLIC METHODS ---

    /// <summary>
    /// Called by an agent when it dies (e.g., runs out of energy).
    /// </summary>
    /// <param name="deadAgent">The agent that has died.</param>
    public void HandleAgentDeath(HummingbirdAgent deadAgent)
    {
        if (activeAgents.Contains(deadAgent))
        {
            activeAgents.Remove(deadAgent);
        }

        // Check if the game is over (only one or zero agents left)
        if (activeAgents.Count <= 1)
        {
            EndEpisode();
        }
    }

    // --- EPISODE MANAGEMENT ---

    private void StartEpisode()
    {
        // Reset the list of active agents to include everyone
        activeAgents.Clear();
        activeAgents.AddRange(allAgents);

        // Reset the flowers in the area
        flowerArea.ResetFlowers();

        // Call OnEpisodeBegin for each agent to reset them
        foreach (var agent in allAgents)
        {
            agent.OnEpisodeBegin(); // We will modify the agent's OnEpisodeBegin next
        }
    }

    private void EndEpisode()
    {
        // In the future, we will give a large reward to the winner here.
        // For now, we just end the episode for everyone.
        HummingbirdAgent winner = activeAgents.FirstOrDefault(); // This will be null if they all die at once
        if (winner != null)
        {
            // TODO: Give winner a +1.0f reward
            Debug.Log($"Episode Over. Winner: {winner.name}");
        }

        // End the ML-Agents episode for every agent.
        // This is crucial for ML-Agents to record the final state and rewards.
        foreach (var agent in allAgents)
        {
            agent.EndEpisode();
        }

        // Start a new episode after a brief delay to allow ML-Agents to process.
        // Using Invoke to delay the start of the next episode.
        Invoke(nameof(StartEpisode), 0.1f);
    }
}
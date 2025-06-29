using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages the overall simulation state for the competitive multi-agent environment.
/// This includes tracking agents, checking win conditions, and managing episode lifecycles.
/// </summary>
public class SimulationManager : MonoBehaviour
{
    // Singleton instance to allow easy access from other scripts.
    public static SimulationManager Instance { get; private set; }

    [Tooltip("Reference to the FlowerArea script in the scene.")]
    public FlowerArea flowerArea;

    [Header("Environment Configuration")]
    [Tooltip("The number of flowers to spawn in the area. If set to 0, it will use all flowers currently in the scene.")]
    public int flowerCount = 8; // Default to 8 flowers, for example

    [Header("Agent Configuration")]
    [Tooltip("The starting energy for each agent at the beginning of an episode.")]
    public float agentInitialEnergy = 25f;

    // List of all agents participating in the simulation.
    private List<HummingbirdAgent> allAgents;

    // List of agents currently active (alive) in the episode.
    private List<HummingbirdAgent> activeAgents;

    // Boolean to track if the current episode is ending.
    private bool isEpisodeEnding = false;

    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }

        // Find all agents that are children of this object's parent.
        allAgents = new List<HummingbirdAgent>(transform.parent.GetComponentsInChildren<HummingbirdAgent>());
        activeAgents = new List<HummingbirdAgent>();
        activeAgents.AddRange(allAgents); // Populate for the first episode
    }

    private void Start()
    {
        // Set up the flowers for the very first episode.
        ResetFlowersForNewEpisode();
    }

    /// <summary>
    /// Called by an agent when it dies.
    /// </summary>
    /// <param name="deadAgent">The agent that has died.</param>
    public void AgentDied(HummingbirdAgent deadAgent)
    {
        if (activeAgents.Contains(deadAgent))
        {
            activeAgents.Remove(deadAgent);
        }

        // Check if the game should end AND if we aren't already ending it
        if (!isEpisodeEnding && activeAgents.Count <= 1)
        {
            isEpisodeEnding = true;
            EndEpisode();
        }
    }

    /// <summary>
    /// Ends the current episode, assigns rewards, and prepares for the next.
    /// </summary>
    private void EndEpisode()
    {
        // Start the end-of-episode sequence as a coroutine
        StartCoroutine(EndEpisodeRoutine());
    }

    private IEnumerator EndEpisodeRoutine()
    {
        HummingbirdAgent winner = activeAgents.FirstOrDefault();
        if (winner != null)
        {
            Debug.Log($"Winner is {winner.name}!");
            // Give the winner a large reward for surviving. This is a very strong positive signal.
            winner.AddReward(1.0f);
        }
        else
        {
            // Optional: Log if there's a draw (e.g., last two agents die simultaneously)
            Debug.Log("Episode ended in a draw.");
        }

        // End the ML-Agents episode for every agent.
        // This queues OnEpisodeBegin() for the next physics step.
        foreach (var agent in allAgents)
        {
            agent.EndEpisode();
        }

        // --- THE CRITICAL FIX ---
        // Wait for the end of the frame. By this point, all physics updates (like
        // the last agent dying) for this frame are complete.
        yield return new WaitForEndOfFrame();

        // Now that we are in the next frame, it's safe to reset the environment.
        ResetFlowersForNewEpisode();

        // Reset the active agent list for the new episode
        activeAgents.Clear();
        activeAgents.AddRange(allAgents);

        // And finally, reset the flag.
        isEpisodeEnding = false;
    }

    /// <summary>
    /// A helper method to centralize the logic for resetting flowers based on the flowerCount parameter.
    /// </summary>
    private void ResetFlowersForNewEpisode()
    {
        if (flowerCount > 0)
        {
            flowerArea.ResetAndEnableRandomFlowers(flowerCount);
        }
        else
        {
            flowerArea.ResetFlowers();
        }
    }
}
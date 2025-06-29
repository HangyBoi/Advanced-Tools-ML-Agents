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

        // Check for end condition ONLY if an episode isn't already ending.
        if (!isEpisodeEnding)
        {
            if (activeAgents.Count == 1)
            {
                // WIN CONDITION MET!
                isEpisodeEnding = true;
                HummingbirdAgent winner = activeAgents[0]; // The last one remaining is the winner
                EndEpisode(winner);
            }
            else if (activeAgents.Count == 0)
            {
                // DRAW CONDITION MET!
                isEpisodeEnding = true;
                EndEpisode(null); // No winner
            }
        }
    }

    /// <summary>
    /// Ends the current episode, assigns rewards, and prepares for the next.
    /// </summary>
    /// <param name="winner">The winning agent, or null for a draw.</param>
    private void EndEpisode(HummingbirdAgent winner)
    {
        // Start the end-of-episode sequence, passing the winner information along.
        StartCoroutine(EndEpisodeRoutine(winner));
    }

    private IEnumerator EndEpisodeRoutine(HummingbirdAgent winner)
    {
        if (winner != null)
        {
            Debug.Log($"Winner is {winner.name}!");
            // This is the CRITICAL terminal reward for success.
            winner.AddReward(1.0f);
        }
        else
        {
            // This log should now only appear when the last two agents die at the same time.
            Debug.Log("Episode ended in a draw.");
        }

        // Give a penalty to all agents who died. This is their terminal reward for failure.
        // The winner is still in the 'allAgents' list, but it did not die, so it won't have a -1 penalty.

        // End the ML-Agents episode for every agent in the original list.
        foreach (var agent in allAgents)
        {
            agent.EndEpisode();
        }

        // Wait for the next frame to safely reset the environment.
        yield return new WaitForEndOfFrame();

        ResetFlowersForNewEpisode();

        // Reset the active agent list for the new episode
        activeAgents.Clear();
        activeAgents.AddRange(allAgents);

        // And finally, reset the flag, allowing a new episode to end.
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
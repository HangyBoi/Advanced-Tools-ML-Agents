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

    // List of all agents participating in the simulation.
    public List<HummingbirdAgent> allAgents;

    // List of agents currently active (alive) in the episode.
    private List<HummingbirdAgent> activeAgents;

    /// <summary>
    /// Event fired when a new episode begins. Agents can subscribe to this.
    /// </summary>
    public UnityEvent OnEpisodeBegan;

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
        // This ensures a clean hierarchy where the manager and agents are siblings.
        allAgents = new List<HummingbirdAgent>(transform.parent.GetComponentsInChildren<HummingbirdAgent>());
        activeAgents = new List<HummingbirdAgent>();
    }

    private void Start()
    {
        // Subscribe the manager's own episode logic to the event.
        OnEpisodeBegan.AddListener(ResetEpisode);

        // Agents subscribe themselves in their own Initialize methods.
        // This is a more decentralized and robust approach.

        // Trigger the start of the first episode.
        OnEpisodeBegan.Invoke();
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

        // Check if the game should end.
        if (activeAgents.Count <= 1)
        {
            EndEpisode();
        }
    }

    /// <summary>
    /// Resets the environment and all agents for a new episode.
    /// </summary>
    private void ResetEpisode()
    {
        // Reset the active agent list
        activeAgents.Clear();
        activeAgents.AddRange(allAgents);

        // Reset all flowers
        flowerArea.ResetFlowers();

        // Let all agents know the episode has begun so they can reset themselves.
        foreach (var agent in allAgents)
        {
            // We'll modify the agent to handle this call
            agent.OnEpisodeBegin();
        }
    }

    /// <summary>
    /// Ends the current episode, assigns rewards, and prepares for the next.
    /// </summary>
    private void EndEpisode()
    {
        HummingbirdAgent winner = activeAgents.FirstOrDefault();
        if (winner != null)
        {
            Debug.Log($"Winner is {winner.name}!");
            // This is where we will give the winner a large reward in a future step.
            // winner.AddReward(1.0f);
        }

        // End the ML-Agents episode for every agent.
        foreach (var agent in allAgents)
        {
            agent.EndEpisode();
        }

        // Start a new episode.
        OnEpisodeBegan.Invoke();
    }
}
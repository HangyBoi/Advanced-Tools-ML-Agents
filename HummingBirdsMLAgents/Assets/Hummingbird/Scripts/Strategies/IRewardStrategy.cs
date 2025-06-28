using UnityEngine;

/// <summary>
/// Defines the contract for a reward calculation strategy for a Hummingbird agent.
/// </summary>
public interface IRewardStrategy
{
    /// <summary>
    /// Calculates the reward to be given every FixedUpdate step.
    /// </summary>
    /// <param name="agent">The agent to calculate the reward for.</param>
    /// <returns>The calculated reward value.</returns>
    float GetFixedUpdateReward(HummingbirdAgent agent);

    // We can add more methods here later if needed, e.g., for nectar collection reward.
}
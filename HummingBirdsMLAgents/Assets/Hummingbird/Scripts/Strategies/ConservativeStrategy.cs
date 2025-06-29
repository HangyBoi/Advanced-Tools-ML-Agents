using UnityEngine;

public class ConservativeStrategy : IRewardStrategy
{
    // A small negative reward given every step to punish wasting time.
    private const float TimePenalty = -0.0005f;

    // The ideal, energy-saving speed. Flying slower than this is good.
    private const float OptimalSpeed = 2f;

    // The reward multiplier for flying efficiently.
    private const float EfficiencyBonus = 0.001f;

    public float GetFixedUpdateReward(HummingbirdAgent agent)
    {
        float reward = TimePenalty;

        // Reward the agent for flying slowly and conserving energy
        float currentSpeed = agent.Rigidbody.linearVelocity.magnitude;
        if (currentSpeed < OptimalSpeed)
        {
            // The slower they fly (below optimal), the higher the reward.
            // (OptimalSpeed - currentSpeed) will be positive here.
            reward += (OptimalSpeed - currentSpeed) * EfficiencyBonus;
        }

        return reward;
    }
}
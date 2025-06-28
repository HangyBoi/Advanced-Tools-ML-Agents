using UnityEngine;

public class ConservativeStrategy : IRewardStrategy
{
    // A small negative reward given every step to punish wasting time.
    private const float TimePenalty = -0.0005f;

    public float GetFixedUpdateReward(HummingbirdAgent agent)
    {
        return TimePenalty;
    }
}
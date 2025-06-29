using UnityEngine;

public class ConservativeStrategy : IRewardStrategy
{
    // A small negative reward given every step to punish wasting time.
    private const float TimePenalty = -0.0005f;

    public float GetFixedUpdateReward(HummingbirdAgent agent)
    {
        float reward = TimePenalty;

        // Reward for being close to the flower, BUT in a larger, safer radius.
        // This encourages it to be near flowers but doesn't force a risky, direct approach.
        if (agent.NearestFlower != null)
        {
            float distanceToFlower = Vector3.Distance(agent.transform.position, agent.NearestFlower.FlowerCenterPosition);

            // Reward it in a larger 4-meter radius
            if (distanceToFlower < 4f)
            {
                // Give a smaller, broader reward for being in the general area.
                // This is less of a "tractor beam" and more of a "warm zone".
                reward += (1.0f - (distanceToFlower / 4f)) * 0.002f;
            }
        }
        return reward;
    }
}
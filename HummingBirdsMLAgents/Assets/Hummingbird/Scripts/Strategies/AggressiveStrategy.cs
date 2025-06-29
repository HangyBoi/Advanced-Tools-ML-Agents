using UnityEngine;

public class AggressiveStrategy : IRewardStrategy
{
    public float GetFixedUpdateReward(HummingbirdAgent agent)
    {
        if (agent.NearestFlower != null)
        {
            // Reward for being close to the flower, not just flying towards it.
            // The closer it gets, the smaller the distance, the higher the reward.
            float distanceToFlower = Vector3.Distance(agent.transform.position, agent.NearestFlower.FlowerCenterPosition);

            // Let's say we reward it heavily inside a 2-meter radius
            if (distanceToFlower < 2f)
            {
                // Reward is inverse to distance. (1.0 - (dist/2.0)) gives a reward from 0 to 0.5
                return (1.0f - (distanceToFlower / 2f)) * 0.005f;
            }
        }
        return 0f; // No reward if no flower or too far
    }
}
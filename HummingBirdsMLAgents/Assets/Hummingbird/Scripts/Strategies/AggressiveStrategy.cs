using UnityEngine;

public class AggressiveStrategy : IRewardStrategy
{
    public float GetFixedUpdateReward(HummingbirdAgent agent)
    {
        float reward = 0f;

        // Reward for flying towards the nearest flower
        if (agent.NearestFlower != null)
        {
            // Calculate the direction to the flower
            Vector3 toFlower = agent.NearestFlower.FlowerCenterPosition - agent.transform.position;
            toFlower.Normalize();

            // Calculate how much the agent's velocity is aligned with that direction
            // Vector3.Dot returns > 0 if they are in the same general direction
            float alignment = Vector3.Dot(agent.Rigidbody.linearVelocity.normalized, toFlower);

            // Give a small reward for positive alignment
            if (alignment > 0.5f) // Only reward if moving clearly towards the flower
            {
                reward += 0.001f * alignment;
            }
        }

        return reward;
    }
}
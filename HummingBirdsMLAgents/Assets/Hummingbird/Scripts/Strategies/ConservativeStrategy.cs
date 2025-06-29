using UnityEngine;

public class ConservativeStrategy : IRewardStrategy
{
    public float GetFixedUpdateReward(HummingbirdAgent agent)
    {
        // Start with the time penalty
        float reward = -0.0005f;

        // Add the efficiency bonus for slow flight
        float currentSpeed = agent.Rigidbody.linearVelocity.magnitude;
        if (currentSpeed < 2f)
        {
            reward += (2f - currentSpeed) * 0.001f;
        }

        // NEW: Add a bonus for being near a flower. This encourages it to leave the ground.
        if (agent.NearestFlower != null)
        {
            float distanceToFlower = Vector3.Distance(agent.transform.position, agent.NearestFlower.FlowerCenterPosition);
            // Give a small reward for being in the "vicinity" of a flower (e.g., within 4 meters)
            // This creates a "reward gradient" in the air, pulling it off the ground.
            if (distanceToFlower < 4f)
            {
                reward += (1.0f - (distanceToFlower / 4f)) * 0.001f;
            }
        }

        return reward;
    }
}
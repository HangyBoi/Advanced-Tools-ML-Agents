using System;
using Unity.MLAgents;
using UnityEngine;

/// <summary>
/// A hummingbird Machinge Learning Agent that can interact with flowers.
/// </summary>
public class HummingbirdAgent : Agent
{
    [Tooltip("Force to apply when moving")]
    public float moveForce = 2f;

    [Tooltip("Speed to pitch up or down")]
    public float pitchSpeed = 100f;

    [Tooltip("Speed to rotate around the up axis")]
    public float yawSpeed = 100f;

    [Tooltip("Transform at the tip of the beak")]
    public Transform beakTip;

    [Tooltip("The agent's camera")]
    public Camera agentCamera;

    [Tooltip("Whether this is training mode or gameplay mode")]
    public bool trainindMode;

    // The rigidbody component of the agent
    new private Rigidbody rigidbody;

    // The flower are the agent is in
    private FlowerArea flowerArea;

    // The nearest flower to the agent
    private Flower nearestFlower;

    // Allows for smoother pitch changes
    private float smoothPitchChange = 0f;

    // Allows for smoother yaw changes
    private float smoothYawChange = 0f;

    // Maximum angle that the agent can pitch up or down
    private const float MaxPitchAngle = 80f;

    // Maximum distance from the beak tip to accept nectar collision
    private const float BeakTipRadius = 0.008f;

    // Whether the agest is frozen (intentionally not flying)
    private bool frozen = false;

    /// <summary>
    /// The amount of ntar the agent has obtained from the flowers this episode.
    /// </summary>
    public float NectarObtained { get; private set; }

    /// <summary>
    /// Initialize the agent and its components.
    /// </summary>
    public override void Initialize()
    {
        rigidbody = GetComponent<Rigidbody>();
        flowerArea = GetComponentInParent<FlowerArea>();

        // If not training mode, no max step, play forever
        if (!trainindMode) MaxStep = 0;
    }

    /// <summary>
    /// Reset the agent when an episode begins.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        if (trainindMode)
        {
            // Only reset flowers in training mode when there is one agent per area
            flowerArea.ResetFlowers();
        }

        //Reset nectar obtained
        NectarObtained = 0f;

        // Zero out the rigidbody velocity, so the movement stops before a new episode begins
        rigidbody.linearVelocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;

        // Default to spawning the agent in front of a flower
        bool inFrontOfFlower = true;
        if (trainindMode)
        {
            // Spawn in front of flower 50% of the time during training
            inFrontOfFlower = UnityEngine.Random.value > 0.5f;
        }

        // Move the ganet to a new random position
        MoveToSafeRandomPosition(inFrontOfFlower);

        // Recalculate the nearest flower now that the agent has moved
        UpdateNearestFlower();
    }

    /// <summary>
    /// Move the agent to a safe random position (i.e. does not collide with anything).
    /// If in front of flower, also point the beak at the flower
    /// </summary>
    /// <param name="inFrontOfFlower">Whether to choose a spot in front of a flower</param>
    private void MoveToSafeRandomPosition(bool inFrontOfFlower)
    {
        bool safePositionFound = false;
        int attemptsRemaining = 100; // Prevents infinite loops
        Vector3 potentialPosition = Vector3.zero;
        Quaternion potentialRotation = new Quaternion();

        // Loop until a safe position is found or attempts run out
        while (!safePositionFound && attemptsRemaining > 0)
        {
            attemptsRemaining--;
            if (inFrontOfFlower)
            {
                // Pick a random flower from the flower area
                Flower randomFlower = flowerArea.Flowers[UnityEngine.Random.Range(0, flowerArea.Flowers.Count)];

                // Position 10 to 20 cm in front of the flower
                float distanceFromFlower = UnityEngine.Random.Range(0.1f, 0.2f);
                potentialPosition = randomFlower.transform.position + randomFlower.FlowerUpVector * distanceFromFlower;

                // Point the beak at the flower (bird's head is center of transform)
                Vector3 toFlower = randomFlower.FlowerCenterPosition - transform.position;
                potentialRotation = Quaternion.LookRotation(toFlower, Vector3.up);
            }
            else
            {
                // Pick a random height from the ground
                float height = UnityEngine.Random.Range(1.2f, 2.5f);

                // Pick a random radius from the center of the flower area
                float radius = UnityEngine.Random.Range(2f, 7f);

                // Pick a random direction rotated around the Y axis
                Quaternion direction = Quaternion.Euler(0f, UnityEngine.Random.Range(-180f, 180f), 0f);

                // Combine the height, radius and direction to pick a potential position
                potentialPosition = flowerArea.transform.position + Vector3.up * height + direction * Vector3.forward * radius;

                // Choose and set random starting pitch and yaw
                float pitch = UnityEngine.Random.Range(-60f, 60f);
                float yaw = UnityEngine.Random.Range(-180f, 180f);
                potentialRotation = Quaternion.Euler(pitch, yaw, 0f);
            }

            // Check to see if the agent will collide with anything at the potential position
            Collider[] colliders = Physics.OverlapSphere(potentialPosition, 0.05f);

            // Safe [psition found if no colliders are hit
            safePositionFound = colliders.Length == 0;
        }

        Debug.Assert(safePositionFound, "Failed to find a safe position for the agent after 100 attempts.");

        // Set the agent's position and rotation to the potential position
        transform.SetPositionAndRotation(potentialPosition, potentialRotation);
    }

    /// <summary>
    /// Updatw the nearest flower to the agent
    /// </summary>
    private void UpdateNearestFlower()
    {
        foreach (Flower flower in flowerArea.Flowers)
        {
            // If this is the first flower or the current flower is closer than the previous nearest
            if (nearestFlower == null && flower.HasNectar)
            {
                // No current nearest flower, set this one as the nearest
                nearestFlower = flower;
            }
            else if (flower.HasNectar)
            {
                // Calculate distance to this flower and distance to the current nearest flower
                float distanceToFlower = Vector3.Distance(flower.transform.position, beakTip.position);
                float distanceToCurrentNearestFlower = Vector3.Distance(nearestFlower.transform.position, beakTip.position);

                // If current flower is empty OR this flower is closer, update the nearest flower
                if (!nearestFlower.HasNectar || distanceToFlower < distanceToCurrentNearestFlower)
                {
                    nearestFlower = flower;
                }
            }
        }
    }
}

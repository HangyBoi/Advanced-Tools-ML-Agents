using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

// Public enum to choose the strategy in the Inspector
public enum RewardStrategyType
{
    Conservative,
    Aggressive
}

/// <summary>
/// A hummingbird Machinge Learning Agent that can interact with flowers.
/// </summary>
public class HummingbirdAgent : Agent
{
    [Header("Movement Mechanics")]
    [Tooltip("Force to apply when moving")]
    public float moveForce = 2f;

    [Tooltip("Speed to pitch up or down")]
    public float pitchSpeed = 100f;

    [Tooltip("Speed to rotate around the up axis")]
    public float yawSpeed = 100f;

    [Header("Energy Mechanics")]
    [Tooltip("The rate at which energy is depleted per second while flying.")]
    public float energyDrainRate = 1f;

    [Tooltip("A multiplier for how much MORE energy is drained based on speed.")]
    public float speedEnergyPenalty = 0.5f;

    [Header("Strategy")]
    [Tooltip("The reward strategy for this agent.")]
    public RewardStrategyType rewardStrategyType = RewardStrategyType.Conservative;

    [Header("References")]
    [Tooltip("The parent GameObject for all visual/physical parts of the agent.")]
    public GameObject visuals;

    [Tooltip("Transform at the tip of the beak")]
    public Transform beakTip;

    [Tooltip("The agent's camera")]
    public Camera agentCamera;

    // The rigidbody component of the agent
    new private Rigidbody rigidbody;

    // Reference to the FlowerArea this agent belongs to
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

    // The current energy level of the agent.
    private float currentEnergy;

    // The maximum energy level of the agent.
    private float maxEnergy;

    // Private field to hold the actual strategy object
    private IRewardStrategy rewardStrategy;

    // Whether the agest is frozen (intentionally not flying)
    public bool frozen = false;

    // A flag to ensure the death logic only runs once.
    private bool isDead = false;

    /// <summary>
    /// The amount of ntar the agent has obtained from the flowers this episode.
    /// </summary>
    public float NectarObtained { get; private set; }

    // We need public accessors for the strategy classes to get agent data
    public Rigidbody Rigidbody => rigidbody;
    public Flower NearestFlower => nearestFlower;

    /// <summary>
    /// Initialize the agent and its components.
    /// </summary>
    public override void Initialize()
    {
        rigidbody = GetComponent<Rigidbody>();

        // Find the FlowerArea that this agent is a child of
        flowerArea = GetComponentInParent<FlowerArea>();

        // Instantiate the correct strategy based on the Inspector setting
        if (rewardStrategyType == RewardStrategyType.Conservative)
        {
            rewardStrategy = new ConservativeStrategy();
        }
        else if (rewardStrategyType == RewardStrategyType.Aggressive)
        {
            rewardStrategy = new AggressiveStrategy();
        }

        // In a competitive setting, episodes end by condition, not steps.
        MaxStep = 5000;
    }

    /// <summary>
    /// Called every frame
    /// </summary>
    private void Update()
    {
        // Draw a line from the beak tip to the nearest flower
        if (nearestFlower != null)
        {
            Debug.DrawLine(beakTip.position, nearestFlower.FlowerCenterPosition, Color.green);
        }
    }

    /// <summary>
    /// Called every .02 seconds (fixed update)
    /// </summary>
    private void FixedUpdate()
    {
        // If the agent is dead OR frozen, do nothing. This is a critical addition.
        if (isDead || frozen) return;

        // Avoids scenario where neares flower nectar is stolen by opponent agent and not updated
        if (nearestFlower != null && !nearestFlower.HasNectar)
        {
            UpdateNearestFlower();
        }

        // --- APPLY STRATEGY REWARD ---
        if (rewardStrategy != null)
        {
            AddReward(rewardStrategy.GetFixedUpdateReward(this));
        }

        // --- ENERGY DRAIN LOGIC ---
        // Calculate the speed-based penalty. The faster the agent moves, the higher the penalty.
        float speedPenalty = rigidbody.linearVelocity.magnitude * speedEnergyPenalty;

        // Calculate total energy drained this frame
        float energyToDrain = (energyDrainRate + speedPenalty) * Time.fixedDeltaTime;

        // Drain the energy
        currentEnergy -= energyToDrain;

        // --- DEATH CHECK ---
        // If energy has run out AND the agent isn't already dead...
        if (currentEnergy <= 0f && !isDead)
        {
            currentEnergy = 0f;

            // Set the flag immediately to prevent this from running again.
            isDead = true;

            // Give a large negative reward for dying.
            AddReward(-1.0f);

            // --- STATS RECORDING ---
            // Get the strategy name to create unique stat keys.
            string strategyName = rewardStrategyType.ToString();

            // Record the survival time (in steps) for this agent before it died.
            Academy.Instance.StatsRecorder.Add($"survival/{strategyName}/time_steps", StepCount);

            // Record the nectar obtained by this agent.
            Academy.Instance.StatsRecorder.Add($"stats/{strategyName}/NectarObtained", NectarObtained);

            // Record the energy efficiency for this agent.
            if (StepCount > 0)
            {
                Academy.Instance.StatsRecorder.Add($"stats/{strategyName}/EnergyEfficiency", NectarObtained / StepCount);
            }
            // ------------------------------------

            // Freeze the agent to stop it from moving.
            FreezeAgent();

            // Deactivate the agent's GameObject.
            visuals.SetActive(false);

            // Notify the SimulationManager that this agent has died.
            Debug.Log($"<color=red>{gameObject.name} has died and is reporting to the manager.</color>");
            SimulationManager.Instance.AgentDied(this);
        }
    }

    /// <summary>
    /// Reset the agent when an episode begins.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        // --- NOTIFY THE MANAGER TO RESET SHARED RESOURCES ---
        // Have the first agent in the list be the "leader" that tells the manager
        // that a new episode has officially begun. This prevents every agent from
        // trying to reset the flowers.
        if (SimulationManager.Instance.IsFirstAgent(this))
        {
            SimulationManager.Instance.OnNewEpisodeBegan();
        }

        // The agent is alive again.
        isDead = false;

        // Clear the previous target to force a fresh search.
        nearestFlower = null;

        //Reset nectar obtained
        NectarObtained = 0f;

        // --- PULL CONFIGURATION FROM SIMULATION MANAGER ---
        // This is the correct pattern. The agent asks the manager for its setup parameters.
        maxEnergy = SimulationManager.Instance.agentInitialEnergy;
        currentEnergy = maxEnergy;
        // --------------------------------------------------

        // Make sure the agent is active and visible
        visuals.SetActive(true);
        // Unfreeze the agent if it was frozen
        if (frozen) UnfreezeAgent();

        // Zero out the rigidbody velocity, so the movement stops before a new episode begins
        rigidbody.linearVelocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;

        // Determine the probability of spawning in front of a flower based on the current
        // .
        float lesson = Academy.Instance.EnvironmentParameters.GetWithDefault("lesson", 0f);
        float chanceToSpawnInFrontOfFlower = 0f;

        if (lesson < 0.33f)
        {
            // Lesson 1: Very high chance to get help.
            chanceToSpawnInFrontOfFlower = 0.9f; // 90%
        }
        else if (lesson < 0.66f)
        {
            // Lesson 2: Reduced chance. Agent must learn to search.
            chanceToSpawnInFrontOfFlower = 0.5f; // 50%
        }
        // Lesson 3: No help. The agent is on its own.
        // chanceToSpawnInFrontOfFlower remains 0f.

        // Decide if this episode will be an "easy start"
        bool inFrontOfFlower = UnityEngine.Random.value < chanceToSpawnInFrontOfFlower;
        // ------------------------------------


        // Move the agent to a new random position
        MoveToSafeRandomPosition(inFrontOfFlower);

        // Recalculate the nearest flower now that the agent has moved
        UpdateNearestFlower();
    }

    /// <summary>
    /// <summary>
    /// Called when an action is received from either the neural network or the player's input.
    /// 
    /// The <paramref name="actionBuffers"/> parameter provides the actions as an <see cref="ActionBuffers"/> object.
    /// The continuous actions are mapped as follows:
    /// Index 0: Move vector x (-1 = left, +1 = right)
    /// Index 1: Move vector y (-1 = down, +1 = up)
    /// Index 2: Move vector z (-1 = backward, +1 = forward)
    /// Index 3: Pitch angle change (-1 = pitch down, +1 = pitch up)
    /// Index 4: Yaw angle change (-1 = rotate left, +1 = rotate right)
    /// </summary>
    /// <param name="actionBuffers">The actions to take, provided as an ActionBuffers object</param>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Don't take actions if the agent is frozen
        if (frozen) return;

        // Calculate movement vector
        Vector3 move = new Vector3(
            actionBuffers.ContinuousActions[0], // x
            actionBuffers.ContinuousActions[1], // y
            actionBuffers.ContinuousActions[2]  // z
        );

        // Add force in the direction of the movement vector
        rigidbody.AddForce(move * moveForce);

        // Get the current rotation of the agent
        Vector3 currentRotationVector = transform.rotation.eulerAngles;

        // Calculate pitch and yaw rotations
        float pitchChange = actionBuffers.ContinuousActions[3];
        float yawChange = actionBuffers.ContinuousActions[4];

        // Smoothly change the pitch and yaw
        smoothPitchChange = Mathf.MoveTowards(smoothPitchChange, pitchChange, 2f * Time.fixedDeltaTime);
        smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2f * Time.fixedDeltaTime);

        // Calculate the new pitch and yaw angles based on smoothed values
        // Clamp pitch to avoid flipping upside down
        float pitch = currentRotationVector.x + smoothPitchChange * pitchSpeed * Time.fixedDeltaTime;
        if (pitch > 180f) pitch -= 360f; // Ensure pitch is in the range [-180, 180]
        pitch = Mathf.Clamp(pitch, -MaxPitchAngle, MaxPitchAngle);

        float yaw = currentRotationVector.y + smoothYawChange * yawSpeed * Time.fixedDeltaTime;

        // Apply the new rotation to the agent
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    /// <summary>
    /// Collect vector observations from the environment for the agent.
    /// </summary>
    /// <param name="sensor">The vector sensor</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        // If nearest flower is null, observer an empty array and return early
        if (nearestFlower == null)
        {
            sensor.AddObservation(new float[11]);
            return;
        }

        // Observe the agent's local rotation (4 observations: x, y, z, w)
        sensor.AddObservation(transform.localRotation.normalized);

        // Get a vector from the beak tip to the nearest flower
        Vector3 toFlower = nearestFlower.FlowerCenterPosition - beakTip.position;

        //Observer a normalize vector pointing to the nearest flower (3 observations: x, y, z)
        sensor.AddObservation(toFlower.normalized);

        // Observe a dot product that indicates whether the beak is in front of the flower (1 observation)
        // (+1 means that the beak is directly in front of the flower, -1 means that it is behind)
        sensor.AddObservation(Vector3.Dot(toFlower.normalized, -nearestFlower.FlowerUpVector.normalized));

        // Observe a dot product that indicates whether the beak is pointing at the flower (1 observation)
        // (+1 means that the beak is pointing at the flower, -1 means that it is pointing away)
        sensor.AddObservation(Vector3.Dot(beakTip.forward.normalized, -nearestFlower.FlowerUpVector.normalized));

        // Observer the relative distance from the beak tip to the flower (1 observation)
        sensor.AddObservation(toFlower.magnitude / FlowerArea.AreaDiameter);

        // Observe the agent's current energy level, normalized (1 observation)
        // A value of 1 means full energy, 0 means empty.
        sensor.AddObservation(currentEnergy / maxEnergy);

        // 11 total observations
    }

    /// <summary>
    /// When Behaviour Type is set to "Heuristic Only" on the agent's Behavior Parameters,
    /// this function will be called. Its return values will be fed into
    /// <see cref="OnActionReceived(ActionBuffers)"/> instead of using the neural network.
    /// </summary>
    /// <param name="actionsOut">An output action array</param>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Create placeholders for all movement/turning actions
        Vector3 forward = Vector3.zero;
        Vector3 left = Vector3.zero;
        Vector3 up = Vector3.zero;
        float pitch = 0f;
        float yaw = 0f;

        // Mouse sensitivity for pitch and yaw
        float mouseSensitivity = 1.0f;

        // Convert keyboard/mouse input to movement and turning actions
        // All values should be in the range [-1, 1]

        // Forward/backward movement
        if (Input.GetKey(KeyCode.W)) forward = transform.forward; // Move forward
        else if (Input.GetKey(KeyCode.S)) forward = -transform.forward; // Move backward

        // Left/right movement
        if (Input.GetKey(KeyCode.A)) left = -transform.right; // Move left
        else if (Input.GetKey(KeyCode.D)) left = transform.right; // Move right

        // Up/down movement
        if (Input.GetKey(KeyCode.Space)) up = transform.up; // Move up
        else if (Input.GetKey(KeyCode.LeftControl)) up = -transform.up; // Move down

        // Pitching up/down and turning left/right using mouse movement
        float mouseY = Input.GetAxis("Mouse Y");
        float mouseX = Input.GetAxis("Mouse X");

        pitch = Mathf.Clamp(mouseY * mouseSensitivity, -1f, 1f); // Pitch up/down
        yaw = Mathf.Clamp(mouseX * mouseSensitivity, -1f, 1f);   // Turn left/right
        // Pitching up/down
        if (Input.GetKey(KeyCode.UpArrow)) pitch = 1f; // Pitch up
        else if (Input.GetKey(KeyCode.DownArrow)) pitch = -1f; // Pitch down

        // Turning left/right
        if (Input.GetKey(KeyCode.LeftArrow)) yaw = -1f; // Turn left
        else if (Input.GetKey(KeyCode.RightArrow)) yaw = 1f; // Turn right

        // Combine the movement vectors and normalize them
        Vector3 combined = (forward + left + up).normalized;

        // Add the 3 movement values, pitch, and yaw to the actionsOut buffer
        actionsOut.ContinuousActions.Array[0] = combined.x; // x
        actionsOut.ContinuousActions.Array[1] = combined.y; // y
        actionsOut.ContinuousActions.Array[2] = combined.z; // z
        actionsOut.ContinuousActions.Array[3] = pitch; // Pitch change
        actionsOut.ContinuousActions.Array[4] = yaw; // Yaw change
    }

    /// <summary>
    /// Prevent the agent from moving and taking actions.
    /// </summary>
    public void FreezeAgent()
    {
        Debug.Assert(frozen == false, "Agent is already frozen");
        frozen = true;
        isDead = true;
        rigidbody.Sleep();
        var decisionRequester = GetComponent<DecisionRequester>();
        if (decisionRequester != null) decisionRequester.enabled = false;

    }

    /// <summary>
    /// Unfreezes the agent, enabling decision-making and restoring physical interactions.
    /// </summary>
    public void UnfreezeAgent()
    {
        // Assert should check if the agent is actually frozen before unfreezing
        Debug.Assert(frozen == true, "Agent is not frozen, cannot unfreeze.");
        if (frozen)
        {
            frozen = false;
            GetComponent<DecisionRequester>().enabled = true;
            rigidbody.WakeUp();
        }
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

        // Get a reference to the flower list from the simulation manager
        List<Flower> flowerList = flowerArea.Flowers;

        // Loop until a safe position is found or attempts run out
        while (!safePositionFound && attemptsRemaining > 0)
        {
            attemptsRemaining--;
            if (inFrontOfFlower)
            {
                // First, create a temporary list of only the flowers that are active and have nectar.
                List<Flower> activeFlowers = new List<Flower>();
                foreach (Flower flower in flowerArea.Flowers)
                {
                    if (flower.gameObject.activeInHierarchy && flower.HasNectar)
                    {
                        activeFlowers.Add(flower);
                    }
                }

                // If we found at least one active flower, spawn in front of it.
                if (activeFlowers.Count > 0)
                {
                    // Pick a random flower from our new list of ACTIVE flowers
                    Flower randomFlower = activeFlowers[UnityEngine.Random.Range(0, activeFlowers.Count)];

                    // Position 10 to 20 cm in front of the flower
                    float distanceFromFlower = UnityEngine.Random.Range(0.1f, 0.2f);
                    potentialPosition = randomFlower.transform.position + randomFlower.FlowerUpVector * distanceFromFlower;

                    // Point the beak at the flower
                    Vector3 toFlower = randomFlower.FlowerCenterPosition - potentialPosition; // Use potentialPosition for accuracy
                    potentialRotation = Quaternion.LookRotation(toFlower, Vector3.up);
                }
                else
                {
                    // Fallback: if no active flowers were found, just do a random spawn for this attempt.
                    inFrontOfFlower = false; // This will force the 'else' block below to run
                }
            }

            // This 'else' block now handles both "random spawn" episodes and the fallback case above.
            if (!inFrontOfFlower)
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
    /// Update the nearest flower to the agent
    /// </summary>
    public void UpdateNearestFlower()
    {
        // Set nearest flower to null to ensure a fresh search
        nearestFlower = null;

        foreach (Flower flower in flowerArea.Flowers)
        {

            // If the flower's GameObject is not active in the scene, skip it.
            if (!flower.gameObject.activeInHierarchy)
            {
                continue;
            }
            // ---------------------

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

    /// <summary>
    /// Called when the agent's collider enters a trigger collider.
    /// </summary>
    /// <param name="other">The trigger collider</param>
    private void OnTriggerEnter(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    /// <summary>
    /// Called when the agent's collider stays in a trigger collider.
    /// </summary>
    /// <param name="other">The trigger collider</param>
    private void OnTriggerStay(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    /// <summary>
    ///  Handles when the agent's colliider enters or stays in a trigger collider.
    /// </summary>
    /// <param name="collider">The trigger collider</param>
    private void TriggerEnterOrStay(Collider collider)
    {
        // Check if the agent is colliding with a nectar collider
        if (collider.CompareTag("Nectar"))
        {
            Vector3 closestPointToBeakTip = collider.ClosestPoint(beakTip.position);

            // Check if the closest point to the beak tip is within the beak tip radius
            // Note: a collision with anything but the beak tip will not count
            if (Vector3.Distance(beakTip.position, closestPointToBeakTip) < BeakTipRadius)
            {
                // Look up the flower for this nectar collider
                Flower flower = flowerArea.GetFlowerFromNectar(collider);

                // Attempt to take nectar from the flower. The amount is based on the drain rate
                // to create a balanced system where drinking replenishes energy at a good pace.
                float nectarReceived = flower.Feed(energyDrainRate * Time.fixedDeltaTime);

                // If we successfully received nectar...
                if (nectarReceived > 0f)
                {
                    // Keep track of the total nectar obtained this episode
                    NectarObtained += nectarReceived;

                    // REPLENISH ENERGY: This is the critical line that was missing.
                    // Let's say 1 unit of nectar (which takes about a second to drink)
                    // should restore a significant amount of energy. A multiplier of 25 feels balanced.
                    float energyGained = nectarReceived * 25f;
                    currentEnergy = Mathf.Clamp(currentEnergy + energyGained, 0f, maxEnergy);

                    // REWARD: Give a MUCH BIGGER reward for drinking.
                    // The bonus for alignment is good, but the base reward must be significant.
                    float baseReward = 0.1f; // Increased from 0.01f to 0.1f (10x bigger!)
                    float bonusReward = 0.1f * Mathf.Clamp01(Vector3.Dot(transform.forward.normalized, -flower.FlowerUpVector.normalized));
                    AddReward(baseReward + bonusReward);
                }

                // If flower is empty, update the nearest flower
                if (!flower.HasNectar)
                {
                    UpdateNearestFlower();
                }
            }
        }
    }

    /// <summary>
    /// Called when the agent's collider collides with something solid
    /// </summary>
    /// <param name="collision">The collision info</param>
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Boundary"))
        {
            // Collided with the area boundary, give a negative reward (penalty)
            AddReward(-.5f);
        }
    }

    /// <summary>
    /// Draws a gizmo in the scene view to visualize the agent's energy level.
    /// </summary>
    private void OnDrawGizmos()
    {
        // Ensure maxEnergy is not zero to avoid division by zero errors.
        if (maxEnergy > 0f)
        {
            // Define the colors for the energy bar
            Color fullColor = Color.green;
            Color emptyColor = Color.red;

            // Calculate the position for the energy bar above the agent's head
            Vector3 barPosition = transform.position + Vector3.up * 0.5f; // 0.5 units above the agent's pivot

            // Calculate the current energy percentage
            float energyPercent = currentEnergy / maxEnergy;

            // --- Draw the background of the bar (the "empty" part) ---
            Gizmos.color = Color.gray;
            Gizmos.DrawCube(barPosition, new Vector3(0.5f, 0.1f, 0.01f)); // A thin, wide bar

            // --- Draw the foreground of the bar (the "full" part) ---
            // Lerp the color from red to green based on energy percentage
            Gizmos.color = Color.Lerp(emptyColor, fullColor, energyPercent);

            // Calculate the position and scale of the foreground bar
            float barWidth = 0.5f;
            Vector3 forgroundPosition = barPosition - Vector3.right * (barWidth * (1 - energyPercent) / 2f);
            Vector3 foregroundScale = new Vector3(barWidth * energyPercent, 0.1f, 0.01f);

            Gizmos.DrawCube(forgroundPosition, foregroundScale);
        }
    }
}

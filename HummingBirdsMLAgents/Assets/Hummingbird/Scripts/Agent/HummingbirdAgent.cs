// HummingbirdAgent.cs (REVISED)

using System;
using System.Collections.Generic; // Added for List<Flower>
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class HummingbirdAgent : Agent
{
    // --- AGENT CONFIGURATION ---
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

    // --- INTERNAL STATE ---
    new private Rigidbody rigidbody;
    private Flower nearestFlower;
    private float smoothPitchChange = 0f;
    private float smoothYawChange = 0f;
    private const float MaxPitchAngle = 80f;
    private const float BeakTipRadius = 0.008f;
    private bool frozen = false;

    // REFACTOR NOTE: The agent no longer knows about FlowerArea or trainingMode.
    // The new SimulationManager handles that logic.

    // REFACTOR NOTE: We create a public event for when the agent "dies".
    // This allows the SimulationManager to listen for this event without a tight coupling.
    public event Action<HummingbirdAgent> OnAgentDied;

    public float NectarObtained { get; private set; }

    public override void Initialize()
    {
        rigidbody = GetComponent<Rigidbody>();
        // REFACTOR NOTE: The agent finds the SimulationManager singleton instance.
        // This is its only link to the outside world.
        if (SimulationManager.Instance == null)
        {
            Debug.LogError("SimulationManager instance not found in the scene.");
        }
    }

    public override void OnEpisodeBegin()
    {
        // REFACTOR NOTE: This method is now MUCH simpler. The SimulationManager
        // is responsible for resetting the environment (flowers). The agent only
        // resets its own internal state.

        NectarObtained = 0f;
        rigidbody.linearVelocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;

        // Move to a new random position
        MoveToSafeRandomPosition(UnityEngine.Random.value > 0.5f);

        // Update nearest flower
        UpdateNearestFlower();
    }

    // --- MOVEMENT & ACTIONS ---
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        if (frozen) return;

        Vector3 move = new Vector3(actionBuffers.ContinuousActions[0], actionBuffers.ContinuousActions[1], actionBuffers.ContinuousActions[2]);
        rigidbody.AddForce(move * moveForce);

        Vector3 currentRotationVector = transform.rotation.eulerAngles;
        float pitchChange = actionBuffers.ContinuousActions[3];
        float yawChange = actionBuffers.ContinuousActions[4];

        smoothPitchChange = Mathf.MoveTowards(smoothPitchChange, pitchChange, 2f * Time.fixedDeltaTime);
        smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2f * Time.fixedDeltaTime);

        float pitch = currentRotationVector.x + smoothPitchChange * pitchSpeed * Time.fixedDeltaTime;
        if (pitch > 180f) pitch -= 360f;
        pitch = Mathf.Clamp(pitch, -MaxPitchAngle, MaxPitchAngle);

        float yaw = currentRotationVector.y + smoothYawChange * yawSpeed * Time.fixedDeltaTime;
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    // --- OBSERVATIONS ---
    public override void CollectObservations(VectorSensor sensor)
    {
        if (nearestFlower == null)
        {
            sensor.AddObservation(new float[10]);
            return;
        }

        sensor.AddObservation(transform.localRotation.normalized);
        Vector3 toFlower = nearestFlower.FlowerCenterPosition - beakTip.position;
        sensor.AddObservation(toFlower.normalized);
        sensor.AddObservation(Vector3.Dot(toFlower.normalized, -nearestFlower.FlowerUpVector.normalized));
        sensor.AddObservation(Vector3.Dot(beakTip.forward.normalized, -nearestFlower.FlowerUpVector.normalized));
        sensor.AddObservation(toFlower.magnitude / FlowerArea.AreaDiameter);
    }

    // --- SIMULATION LOGIC ---

    // REFACTOR NOTE: A new public method for the agent to "die".
    // This will be called when its energy runs out in the next step.
    public void Die()
    {
        // This is a placeholder. For now, we'll just deactivate the agent.
        // Later, this will be triggered by running out of energy.
        gameObject.SetActive(false);

        // Fire the event to notify the SimulationManager.
        OnAgentDied?.Invoke(this);
    }

    public void UpdateNearestFlower()
    {
        // REFACTOR NOTE: The agent gets the list of flowers from the SimulationManager.
        // This decouples the Agent from the FlowerArea script.
        var flowers = SimulationManager.Instance.flowerArea.Flowers;

        // Find the closest flower with nectar
        float closestDistance = float.MaxValue;
        Flower potentialNearestFlower = null;

        foreach (Flower flower in flowers)
        {
            if (flower.HasNectar)
            {
                float distanceToFlower = Vector3.Distance(flower.transform.position, beakTip.position);
                if (distanceToFlower < closestDistance)
                {
                    closestDistance = distanceToFlower;
                    potentialNearestFlower = flower;
                }
            }
        }
        nearestFlower = potentialNearestFlower;
    }

    private void TriggerEnterOrStay(Collider other)
    {
        if (other.CompareTag("Nectar"))
        {
            Vector3 closestPointToBeakTip = other.ClosestPoint(beakTip.position);
            if (Vector3.Distance(beakTip.position, closestPointToBeakTip) < BeakTipRadius)
            {
                Flower flower = SimulationManager.Instance.flowerArea.GetFlowerFromNectar(other);
                float nectarReceived = flower.Feed(.01f);
                NectarObtained += nectarReceived;

                // REFACTOR NOTE: The reward logic has been simplified for now.
                // We will build our A/B reward functions on top of this later.
                AddReward(0.01f);

                if (!flower.HasNectar)
                {
                    UpdateNearestFlower();
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Boundary"))
        {
            AddReward(-.5f);
        }
    }

    private void FixedUpdate()
    {
        if (nearestFlower != null && !nearestFlower.HasNectar)
        {
            UpdateNearestFlower();
        }
    }

    // --- HELPERS & HEURISTICS ---
    // (Heuristic, MoveToSafeRandomPosition, Freeze/Unfreeze, and Update methods remain largely the same)
    // For brevity, I'm omitting them here, but you should keep them in your script.
    #region Heuristics and Helpers
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
        else if (Input.GetKey(KeyCode.RightArrow)) yaw = -1f; // Turn right

        // Combine the movement vectors and normalize them
        Vector3 combined = (forward + left + up).normalized;

        // Add the 3 movement values, pitch, and yaw to the actionsOut buffer
        actionsOut.ContinuousActions.Array[0] = combined.x; // x
        actionsOut.ContinuousActions.Array[1] = combined.y; // y
        actionsOut.ContinuousActions.Array[2] = combined.z; // z
        actionsOut.ContinuousActions.Array[3] = pitch; // Pitch change
        actionsOut.ContinuousActions.Array[4] = yaw; // Yaw change
    }
    public void FreezeAgent()
    {
        frozen = true;
        rigidbody.Sleep();
    }
    public void UnfreezeAgent()
    {
        frozen = false;
        rigidbody.WakeUp();
    }
    private void MoveToSafeRandomPosition(bool inFrontOfFlower)
    {
        bool safePositionFound = false;
        int attemptsRemaining = 100;
        Vector3 potentialPosition = Vector3.zero;
        Quaternion potentialRotation = new Quaternion();

        while (!safePositionFound && attemptsRemaining > 0)
        {
            attemptsRemaining--;
            if (inFrontOfFlower)
            {
                Flower randomFlower = SimulationManager.Instance.flowerArea.Flowers[UnityEngine.Random.Range(0, SimulationManager.Instance.flowerArea.Flowers.Count)];
                float distanceFromFlower = UnityEngine.Random.Range(0.1f, 0.2f);
                potentialPosition = randomFlower.transform.position + randomFlower.FlowerUpVector * distanceFromFlower;
                Vector3 toFlower = randomFlower.FlowerCenterPosition - potentialPosition; // Use potential position here
                potentialRotation = Quaternion.LookRotation(toFlower, Vector3.up);
            }
            else
            {
                float height = UnityEngine.Random.Range(1.2f, 2.5f);
                float radius = UnityEngine.Random.Range(2f, 7f);
                Quaternion direction = Quaternion.Euler(0f, UnityEngine.Random.Range(-180f, 180f), 0f);
                potentialPosition = SimulationManager.Instance.flowerArea.transform.position + Vector3.up * height + direction * Vector3.forward * radius;
                float pitch = UnityEngine.Random.Range(-60f, 60f);
                float yaw = UnityEngine.Random.Range(-180f, 180f);
                potentialRotation = Quaternion.Euler(pitch, yaw, 0f);
            }

            Collider[] colliders = Physics.OverlapSphere(potentialPosition, 0.05f);
            safePositionFound = colliders.Length == 0;
        }

        Debug.Assert(safePositionFound, "Could not find a safe position to spawn agent.");
        transform.SetPositionAndRotation(potentialPosition, potentialRotation);
    }
    private void Update()
    {
        if (nearestFlower != null)
        {
            Debug.DrawLine(beakTip.position, nearestFlower.FlowerCenterPosition, Color.green);
        }
    }
    #endregion
}
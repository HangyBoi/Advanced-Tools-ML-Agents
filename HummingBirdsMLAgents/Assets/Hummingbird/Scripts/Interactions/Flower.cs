using UnityEngine;

/// <summary>
/// Manages
/// </summary>
public class Flower : MonoBehaviour
{
    [Tooltip("The color when the flowes is full")]
    public Color fullFlowerColor = new(1f, 0f, .3f);

    [Tooltip("The color when the flowes is empty")]
    public Color emptyFlowerColor = new(.5f, 0f, 1f);

    [Tooltip("The trigger collider for the nectar. Should be a child of this GameObject.")]
    public Collider nectarCollider;

    // The solid collider representing the flower petals
    private Collider flowerCollider;

    // The flower's material, used for changing colors
    private Material flowerMaterial;

    /// <summary>
    /// A vector representing the up direction of the flower.
    /// </summary>
    public Vector3 FlowerUpVector
    {
        get
        {
            return nectarCollider.transform.up;
        }
    }

    /// <summary>
    /// The center position of the flower, where the nectar is located.
    /// </summary>
    public Vector3 FlowerCenterPosition
    {
        get
        {
            return nectarCollider.transform.position;
        }
    }

    /// <summary>
    /// The amount of nectar remaining in the flower.
    /// </summary>
    public float NectarAmount { get; private set; }

    /// <summary>
    /// Whether the flower has any nectar remaining.
    /// </summary>
    public bool HasNectar
    {
        get
        {
            return NectarAmount > 0f;
        }
    }

    /// <summary>
    /// Attempts to remove nectar from the flower.
    /// </summary>
    /// <param name="amount">The amount of nectar to remove </param>
    /// <returns>The actual ammount successfully removed</returns>
    public float Feed(float amount)
    {
        // Track how much nectar was successfully taken (cannot take more than available)
        float nectarTaken = Mathf.Clamp(amount, 0f, NectarAmount);

        //Subtract the nectar taken from the total amount
        NectarAmount -= nectarTaken;

        if (NectarAmount <= 0f)
        {
            // No nectar remaining, update the flower's appearance
            NectarAmount = 0f;

            // Deactivate the colliders to prevent further interaction
            flowerCollider.gameObject.SetActive(false);
            nectarCollider.gameObject.SetActive(false);

            // Set the flower's color to indicate it's empty
            flowerMaterial.color = emptyFlowerColor;
        }
        else
        {
            flowerMaterial.color = Color.Lerp(emptyFlowerColor, fullFlowerColor, NectarAmount);
        }

        // Return the amount of nectar that was actually taken
        return nectarTaken;
    }

    /// <summary>
    /// Resets the flower to its initial state.
    /// </summary>
    /// <remarks>This method restores the flower's nectar amount to full, reactivates its colliders,  and
    /// resets its color to the original full state. It is typically used to prepare the  flower for reuse or to
    /// simulate a refreshed state.</remarks>
    public void ResetFlower()
    {
        // Reset the nectar amount to full
        NectarAmount = 1f;

        // Reactivate the colliders
        flowerCollider.gameObject.SetActive(true);
        nectarCollider.gameObject.SetActive(true);

        // Reset the flower's color to full
        flowerMaterial.color = fullFlowerColor;
    }

    /// <summary>
    /// Called when the flower wakes up.
    /// </summary>
    private void Start()
    {
        // Find the flower's mesh renderer and get its material
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        flowerMaterial = meshRenderer.material;

        // Find the nectar collider (trigger) and the flower collider (solid)
        flowerCollider = transform.Find("FlowerCollider").GetComponent<Collider>();
    }
}
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages a collection of flower plants and attached flowers.
/// </summary>
public class FlowerArea : MonoBehaviour
{
    // The diameter of the area where the agent and flowers can be
    // used for observing relative distance from agent to flower.
    [Tooltip("The diameter of the area where the agent and flowers can be used for observing relative distance from agent to flower")]
    public const float AreaDiameter = 20f;

    // The list of all flowers in the area (flower plants have multiple flowers)
    private List<GameObject> flowerPlants;

    // A lookup dictionary foor looking up a flower from a nectar collider
    private Dictionary<Collider, Flower> nectarColliderToFlowerDictionary;

    /// <summary>
    /// The list of all flowers in the area, including those attached to flower plants.
    /// </summary>
    public List<Flower> Flowers { get; private set; }


    private void Awake()
    {
        // Initialize variables
        flowerPlants = new List<GameObject>();
        nectarColliderToFlowerDictionary = new Dictionary<Collider, Flower>();
        Flowers = new List<Flower>();

        // Find all flowers as soon as this object wakes up.
        FindChildFlowers(transform);

        // --- NEW LOGIC: Populate the dictionary right here ---
        // By doing this in Awake(), we guarantee it runs before any agent can interact.
        foreach (Flower flower in Flowers)
        {
            // Check to prevent errors if a flower is misconfigured
            if (flower.nectarCollider != null)
            {
                // Add the collider and its parent flower to the dictionary
                nectarColliderToFlowerDictionary.Add(flower.nectarCollider, flower);
            }
            else
            {
                // This debug message is a lifesaver for finding broken prefabs.
                Debug.LogError($"Flower '{flower.gameObject.name}' under '{this.gameObject.name}' is missing its nectarCollider reference!", flower.gameObject);
            }
        }
    }

    /// <summary>
    /// Reset flowers in flower plants
    /// </summary>
    public void ResetFlowers()
    {
        // First, rotate each plant once.
        foreach (GameObject flowerPlant in flowerPlants)
        {
            float xRotation = UnityEngine.Random.Range(-5f, 5f);
            float yRotation = UnityEngine.Random.Range(-180f, 180f);
            float zRotation = UnityEngine.Random.Range(-5f, 5f);
            flowerPlant.transform.localRotation = Quaternion.Euler(xRotation, yRotation, zRotation);
        }

        // Then, reset each flower once.
        foreach (Flower flower in Flowers)
        {
            flower.gameObject.SetActive(true);
            flower.ResetFlower();
        }
    }

    /// <summary>
    /// Resets a random subset of flowers and disables the rest.
    /// The parent plant rotation is not changed here to keep experiments consistent.
    /// </summary>
    /// <param name="count">The number of flowers to enable.</param>
    public void ResetAndEnableRandomFlowers(int count)
    {
        // Disable all flowers first
        foreach (var flower in Flowers)
        {
            flower.gameObject.SetActive(false);
        }

        // Check if the count is greater than the total number of available flowers
        if (count > Flowers.Count) count = Flowers.Count;

        // Shuffle the flowers and take the specified count
        var shuffledFlowers = Flowers.OrderBy(x => System.Guid.NewGuid()).Take(count);

        // Enable the selected flowers and reset them
        foreach (var flower in shuffledFlowers)
        {
            flower.gameObject.SetActive(true);
            flower.ResetFlower();
        }
    }

    /// <summary>
    /// Gets the <see cref="Flower"/> that a nectar collider belongs to."/>
    /// </summary>
    /// <param name="nectarCollider">The nectar collider</param>
    /// <returns>The matching flower</returns>
    public Flower GetFlowerFromNectar(Collider nectarCollider)
    {
        return nectarColliderToFlowerDictionary[nectarCollider];
    }

    /// <summary>
    /// Recursively finds all child flowers under the given parent transform.
    /// </summary>
    /// <param name="parent">The parent of the children to check</param>
    private void FindChildFlowers(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.CompareTag("Flower_Plant"))
            {
                // Found a flower plant, add it to the list
                flowerPlants.Add(child.gameObject);

                // Look for flowers within this flower plant
                FindChildFlowers(child);
            }
            else
            {
                // Not a flower plant, look for a Flower component
                Flower flower = child.GetComponent<Flower>();

                if (flower != null)
                {
                    // Found a flower, add it to the list
                    Flowers.Add(flower);

                    // Note: there are no flowers that are children of other flowers
                }
                else
                {
                    // Flower component not found, check children recursively
                    FindChildFlowers(child);
                }
            }
        }
    }
}

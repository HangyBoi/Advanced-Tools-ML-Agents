// FlowerArea.cs (REVISED)

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages a collection of flower plants and attached flowers.
/// Its sole responsibility is to control the state of the flowers.
/// </summary>
public class FlowerArea : MonoBehaviour
{
    public const float AreaDiameter = 20f;
    private Dictionary<Collider, Flower> nectarColliderToFlowerDictionary;
    public List<Flower> Flowers { get; private set; }

    private void Awake()
    {
        // A much cleaner way to find all flowers
        Flowers = GetComponentsInChildren<Flower>().ToList();
        nectarColliderToFlowerDictionary = new Dictionary<Collider, Flower>();

        foreach (var flower in Flowers)
        {
            nectarColliderToFlowerDictionary.Add(flower.nectarCollider, flower);
        }
    }

    public void ResetFlowers()
    {
        // Reset each flower in the flower plant
        foreach (Flower flower in Flowers)
        {
            // NOTE: The random rotation of the parent plant is removed for now
            // for more consistent evaluation. We can add it back later if needed.
            flower.ResetFlower();
        }
    }

    public Flower GetFlowerFromNectar(Collider nectarCollider)
    {
        return nectarColliderToFlowerDictionary[nectarCollider];
    }
}
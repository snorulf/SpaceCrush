using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TileData", menuName = "RG/TileData", order = 1)]
public class TileData : ScriptableObject {
    [Header("Explode")]
    public bool explodeOnPop = false;
    public float explosionForce = 200.0f;
    public float explosionRadius = 10.0f;
    public Vector2 constantForce = new Vector2(0.0f, 0.0f);
    [Tooltip("The maximum distance traveled once exploded")]
    public float maxExplodeDistance = 50.0f;

    [Header("Movement")]
    public float moveSpeed = 0.25f;
    public float resetDuration = 2.5f;

    [Header("Emissive")]
    public Color emissionColor = Color.white;
    public float lerpToEmissiveDuration = 5.0f;
    public float emissiveIntensity = 10.0f;
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

[SelectionBase]
public class Tile : MonoBehaviour
{

    public enum TileType
    {
        Cube,
        Sphere,
        Cylinder,
        Capsule,
        Unknown
    }

    private bool matching = false;
    public bool Matching
    {
        get => matching;
    }

    [SerializeField] private GameObject cubePrefab;
    [SerializeField] private GameObject spherePrefab;
    [SerializeField] private GameObject cylinderPrefab;
    [SerializeField] private GameObject capsulePrefab;

    [SerializeField] private TileType type = TileType.Unknown;
    [SerializeField] private TileData data;

    [SerializeField] private Transform explosionLocator;

    public Vector3 initPos;
    public Quaternion initRot;

    private GameObject tileGO;
    private Material material;
    private MeshRenderer meshRenderer;
    private Rigidbody rb;

    public Tile left = null;
    public Tile right = null;
    public Tile top = null;
    public Tile bottom = null;

    private bool popped = false;

    public int columnIndex;

    public Vector2 stepSize = Vector2.one;
    public Vector3 positionOffset = Vector3.zero;

    public float moveDuration = 0.0f;
    public TileType Type
    {
        get => type;
        set
        {
            type = value;
            if (tileGO == null)
            {
                InstantiateTilePrefab();
            }
            else
            {
                // FIXME: Change type of instantiated tile.
                throw new System.NotImplementedException();
            }
        }
    }

    private Material Material
    {
        get
        {
            if (material == null)
            {
                material = GetComponentInChildren<MeshRenderer>().material;
                UnityEngine.Assertions.Assert.IsNotNull(material);
            }
            return material;
        }
    }

    private MeshRenderer MeshRenderer
    {
        get
        {
            if (meshRenderer == null)
            {
                meshRenderer = GetComponentInChildren<MeshRenderer>();
                UnityEngine.Assertions.Assert.IsNotNull(meshRenderer);
            }
            return meshRenderer;
        }
    }

    private Rigidbody Rigidbody
    {
        get
        {
            if (rb == null)
            {
                rb = GetComponentInChildren<Rigidbody>();
                UnityEngine.Assertions.Assert.IsNotNull(rb);
            }
            return rb;
        }
    }

    public void SetEmissive(bool enable)
    {
        if (gameObject.activeSelf)
        {
            StartCoroutine(LerpEmissive(data.lerpToEmissiveDuration, enable ? data.emissiveIntensity : 0.0f));
        }
    }

    private readonly int emissionColorID = Shader.PropertyToID("_EmissionColor");

    public float EmissionIntensity
    {
        set
        {
            Material.SetColor(emissionColorID, data.emissionColor * value);
        }
    }

    public int Column { get => columnIndex; set => columnIndex = value; }

    public bool Popped { get => popped; }

    private void InstantiateTilePrefab()
    {

        GameObject prefab = null;
        switch (type)
        {
            case TileType.Cube:
                prefab = cubePrefab;
                break;
            case TileType.Sphere:
                prefab = spherePrefab;
                break;
            case TileType.Cylinder:
                prefab = cylinderPrefab;
                break;
            case TileType.Capsule:
                prefab = capsulePrefab;
                break;
            default:
                Assert.IsTrue(false, "Invalid type: " + type.ToString());
                break;
        }
        tileGO = Instantiate(prefab, transform);
    }

    public void ResetTile()
    {

        // Reactivate
        gameObject.SetActive(true);

        // De-explode
        Rigidbody.isKinematic = true;

        // Flags
        popped = false;
        matching = false;

        // Shadows
        MeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

        // Emission
        StartCoroutine(LerpEmissive(data.lerpToEmissiveDuration, 0.0f));

        // Move back
        //float duration = data.resetSpeed * Vector3.Distance(transform.localPosition, initPos);
        StartCoroutine(LerpToPositionAndRotation(initPos, initRot, data.resetDuration));
    }

    private void Update()
    {
        if (Exploded())
        {
            // We don't want to the tile to fly too far away so lock if it has travelled by enough distance.
            if (Vector3.Distance(transform.localPosition, initPos) > data.maxExplodeDistance)
            {
                Rigidbody.isKinematic = true;
                MeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }

    private bool Exploded()
    {
        return popped && Rigidbody.isKinematic == false;
    }

    public void Pop()
    {
        popped = true;

        if (data.explodeOnPop)
        {
            Explode(explosionLocator);
        }
        else
        {
            gameObject.SetActive(false);
        }

        UpdatePoppedVicinity();

        top?.MoveDown(this);
    }

    private IEnumerator LerpEmissive(float duration, float targetValue)
    {
        float time = 0;
        float start = targetValue == data.emissiveIntensity ? data.emissiveIntensity : 0.0f;

        while (time < duration)
        {
            EmissionIntensity = Mathf.Lerp(start, targetValue, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
    }

    public void Explode(Transform transform)
    {
        Rigidbody.isKinematic = false;
        Rigidbody.AddExplosionForce(data.explosionForce, transform.position, data.explosionRadius);
    }

    public void MoveDown(Tile formerTile)
    {
        // Move along any further top tiles
        top?.MoveDown(this);

        UpdateTraversedVicinity(formerTile);

        // Move the tile
        Vector3 positionToMoveTo = new Vector3(Column * stepSize.x, CalcRowIndex() * stepSize.y);
        moveDuration = data.moveSpeed * Vector3.Distance(transform.localPosition, positionToMoveTo);
        StartCoroutine(LerpToPositionAndRotation(positionToMoveTo, initRot, moveDuration));
    }

    private int CalcRowIndex()
    {
        int newRow = 0;
        Tile tile = bottom;
        while (tile != null)
        {
            tile = tile.bottom;
            newRow++;
        }
        return newRow;
    }

    private void UpdatePoppedVicinity()
    {
        if (bottom != null)
        {
            bottom.top = top;
        }
        if (top != null)
        {
            top.bottom = bottom;
        }
        if (left != null)
        {
            left.right = top;
        }
        if (right != null)
        {
            right.left = top;
        }
    }

    private void UpdateTraversedVicinity(Tile formerTile)
    {

        // If there is no top tile we make sure to clear former row neighbours.
        if (top == null)
        {

            if (left != null)
            {
                left.right = null;
            }
            if (right != null)
            {
                right.left = null;
            }
        }

        // Assign new neighbours from the former tile.
        left = formerTile.left;
        right = formerTile.right;

        if (left != null)
        {
            left.right = formerTile.top;
        }
        if (right != null)
        {
            right.left = formerTile.top;
        }
    }

    public void CheckForRowMatches()
    {
        // Check for row matches.
        // Legend:
        // <X> = this tile
        // |x| = neighbouring tiles
        if (left != null && left.type == type)
        { // |x|<X>
            if (left.left != null && left.left.type == type)
            { // |x|x|<X>
                matching = true;
            }
            if (right != null && right.type == type)
            { // |x|<X>|x|
                matching = true;
            }
        }
        if (right != null && right.type == type)
        {// <X>|x|
            if (right.right != null && right.right.type == type)
            { //<X>|x|x|
                matching = true;
            }
            if (left != null && left.type == type)
            { //|x|<X>|x|
                matching = true;
            }
        }
    }

    private IEnumerator LerpToPositionAndRotation(Vector3 targetPosition, Quaternion targetRotation, float duration)
    {
        float time = 0;
        Vector3 startPosition = transform.localPosition;
        Quaternion startRotation = transform.localRotation;

        while (time < duration)
        {
            transform.localPosition = Vector3.Lerp(startPosition, targetPosition, time / duration);
            transform.localRotation = Quaternion.Lerp(startRotation, targetRotation, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = targetPosition;
    }

    public List<Tile> GetTopTiles()
    {
        var topTiles = new List<Tile>();
        Tile tile = top;
        while (tile != null)
        {
            topTiles.Add(tile);
            tile = tile.top;
        }
        return topTiles;
    }

    #region Draw gizmos
    void OnDrawGizmosSelected()
    {

        DrawTileGizmo(left, Color.green);
        DrawTileGizmo(right, Color.red);
        DrawTileGizmo(top, Color.blue);
        DrawTileGizmo(bottom, Color.cyan);
    }

    private void OnDrawGizmos()
    {
        if (Matching)
        {
            DrawTileGizmo(this, Color.black, -0.35f, -0.5f, 0.25f);
        }
    }

    private void DrawTileGizmo(Tile tile, Color color, float yOffset = 0.0f, float zOffset = -1.0f, float radius = 0.1f)
    {
        Gizmos.color = color;
        if (tile != null)
        {
            Gizmos.DrawSphere(tile.transform.position + new Vector3(0.0f, yOffset, zOffset), radius);
        }
        else
        {
            // TODO draw null?
        }
    }
    #endregion
}
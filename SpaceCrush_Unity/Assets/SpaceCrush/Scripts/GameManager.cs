using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour {

    [SerializeField, Range(3, 100)] private int columns = 10, rows = 10;
    [SerializeField, Range(0.0f, 10.0f)] private float popMatchesWaitTime = 1.0f;

    private TileGrid grid;
    private Camera mainCamera;

    private bool handlingturn = false;

    [SerializeField, Range(15.0f, 60.0f * 10.0f)] private float IdleTimeSetting = 15.0f;
    private float lastIdleTime;

    private bool playing = false;

    WaitForSeconds popMatchesWait;

    void Start() {
        grid = GetComponentInChildren<TileGrid>();
        UnityEngine.Assertions.Assert.IsNotNull(grid);
        grid.PopulateGrid(columns, rows);
        mainCamera = Camera.main;

        lastIdleTime = Time.time;

        popMatchesWait = new WaitForSeconds(popMatchesWaitTime);
    }

    public void ResetGame() {
        if (PlayersTurn()) {
            grid.ResetTiles();

        }
    }

    private bool Idle() {
        return Time.time - lastIdleTime > IdleTimeSetting;
    }

    private void Update() {
        if (playing && Idle() && !handlingturn) {
            grid.ResetTiles();
            playing = false;
        }

        if (Input.touchCount > 0) { // Touch input

            lastIdleTime = Time.time;
            playing = true;

            HandleTouch();
        }

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0)) { // left button
            
            lastIdleTime = Time.time;
            playing = true;

            HandleClick(); ;
        }

        if (Input.GetKeyDown(KeyCode.R)) {
            ResetGame();
        }
#endif
    }

    private void HandleTouch() {
        if (PlayersTurn()) {
            var touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Began) {
                return;
            }

            PopTileAtPosition(touch.position);
        }
    }

    private void HandleClick() {
        if (PlayersTurn()) {
            PopTileAtPosition(Input.mousePosition);
        }
    }

    private bool PlayersTurn() {
        return !handlingturn;
    }

    private void PopTileAtPosition(Vector3 pos) {
        // Construct a ray from pos
        Ray ray = mainCamera.ScreenPointToRay(pos);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit)) {
            if (hit.transform.TryGetComponent(out Tile tile)) {
                //Debug.Log("Clicked: " + tile.transform.name, tile.gameObject);

                if (tile.Popped) { // Popped tiles can still be pushed
                    tile.Explode(hit.transform);
                } else {
                    HandlePlayerTurn(tile);
                }
            }
        }
    }

    private void HandlePlayerTurn(Tile tile) {
        handlingturn = true; // flag
        var turnResult = grid.PopTile(tile);
        StartCoroutine(PopMatches(turnResult));
    }

    private IEnumerator PopMatches(TileGrid.TurnResult turnResult) {

        yield return new WaitForSeconds(turnResult.turnDuration);

        while (turnResult.matches.Count >= 3) {

            grid.SetEmissive(turnResult.matches, true);

            yield return popMatchesWait;

            var result = grid.PopTiles(turnResult.matches);

            grid.SetEmissive(turnResult.matches, false);

            turnResult = result;

            yield return new WaitForSeconds(result.turnDuration);

            lastIdleTime = Time.time; // Update this here since the popping-sequence should not count as idle-time.
        }
        handlingturn = false; // flag
    }
}
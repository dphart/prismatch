using UnityEngine;

public class BoardGenerator : MonoBehaviour
{
    public GameObject rowPrefab;
    public GameObject tilePrefab;
    public int numRows = 8;
    public int numCols = 8;

    public void GenerateBoard()
    {
        // Clear existing children
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        for (int row = 0; row < numRows; row++)
        {
            GameObject rowObj = Instantiate(rowPrefab, transform);
            rowObj.name = $"Row_{row + 1}";

            for (int col = 0; col < numCols; col++)
            {
                GameObject tile = Instantiate(tilePrefab, rowObj.transform);
                tile.name = $"Tile_{col + 1}";
            }
        }
    }

    private void Start()
    {
        GenerateBoard(); // Optional: Auto-generate on play
    }
}

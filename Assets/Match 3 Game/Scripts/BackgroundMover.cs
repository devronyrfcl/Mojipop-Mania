using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class BubbleCloudSpawner : MonoBehaviour
{
    [Header("🔹 Bubble Settings")]
    public GameObject bubblePrefab;
    public Transform[] bubbleSpawnPoints;
    public float bubbleSpawnInterval = 1.5f;
    public float bubbleMoveSpeed = 2f;
    public float bubbleTravelDistance = 5f;
    public static Vector2 bubbleScaleRange = new Vector2(0.5f, 1.5f); // Accessible globally

    [Header("🔹 Cloud Settings")]
    public GameObject cloudPrefab;
    public Transform[] cloudSpawnLeft;
    public Transform[] cloudSpawnRight;
    public float cloudSpawnInterval = 3f;
    public float cloudMoveSpeed = 5f;
    public float cloudTravelDistance = 20f;

    private void Start()
    {
        StartCoroutine(SpawnBubblesRoutine());
        StartCoroutine(SpawnCloudsRoutine());
    }

    IEnumerator SpawnBubblesRoutine()
    {
        while (true)
        {
            SpawnBubble();
            yield return new WaitForSeconds(bubbleSpawnInterval);
        }
    }

    IEnumerator SpawnCloudsRoutine()
    {
        while (true)
        {
            SpawnCloud();
            yield return new WaitForSeconds(cloudSpawnInterval);
        }
    }

    void SpawnBubble()
    {
        if (bubblePrefab == null || bubbleSpawnPoints.Length == 0) return;

        Transform spawnPoint = bubbleSpawnPoints[Random.Range(0, bubbleSpawnPoints.Length)];
        GameObject bubble = Instantiate(bubblePrefab, spawnPoint.position, Quaternion.identity);

        // Randomize scale
        float scaleValue = Random.Range(bubbleScaleRange.x, bubbleScaleRange.y);
        bubble.transform.localScale = Vector3.one * scaleValue;

        // Move upward
        Vector3 targetPos = bubble.transform.position + Vector3.up * bubbleTravelDistance;
        bubble.transform.DOMove(targetPos, bubbleMoveSpeed).SetEase(Ease.Linear)
            .OnComplete(() => Destroy(bubble));
    }

    void SpawnCloud()
    {
        if (cloudPrefab == null || cloudSpawnLeft.Length == 0 || cloudSpawnRight.Length == 0) return;

        bool fromLeft = Random.value > 0.5f;
        Transform spawnPoint = fromLeft
            ? cloudSpawnLeft[Random.Range(0, cloudSpawnLeft.Length)]
            : cloudSpawnRight[Random.Range(0, cloudSpawnRight.Length)];

        GameObject cloud = Instantiate(cloudPrefab, spawnPoint.position, Quaternion.identity);

        // Determine target position
        Vector3 targetPos = fromLeft
            ? cloud.transform.position + Vector3.right * cloudTravelDistance
            : cloud.transform.position + Vector3.left * cloudTravelDistance;

        cloud.transform.DOMove(targetPos, cloudMoveSpeed).SetEase(Ease.Linear)
            .OnComplete(() => Destroy(cloud));
    }
}

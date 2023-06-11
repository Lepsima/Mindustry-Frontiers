using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Assets;

public class BlockRubble : MonoBehaviour {
    const float fadeOutTime = 3f;

    SpriteRenderer spriteRenderer;
    float fadeOutTimer = 0;

    private void Start() {
        fadeOutTimer = Time.time + 10f;
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.color = new Color(0f, 0f, 0f);
    }

    private void Update() {
        if (fadeOutTimer < Time.time) {
            float fadeOutProgress = (Time.time - fadeOutTimer) / fadeOutTime;
            spriteRenderer.color = new Color(0f, 0f, 0f, 1f - fadeOutProgress);

            // If the fadeout progress has ended, destroy this
            if (fadeOutProgress >= 1f) {
                Destroy(gameObject);
            }
        }
    }
}

public static class RubbleGenerator {
    public static List<Sprite>[] rubbleSprites;
    public static GameObject rubblePrefab;
    public static int maxRubbleSize = 5;
    public static int maxRubbleVariants = 2;

    static RubbleGenerator() {
        rubbleSprites = new List<Sprite>[maxRubbleSize];
        rubblePrefab = AssetLoader.GetPrefab("RubblePrefab");

        for (int size = 0; size < maxRubbleSize; size++) {
            rubbleSprites[size] = new();

            for(int variant = 0; variant < maxRubbleVariants; variant++) {
                Sprite sprite = AssetLoader.GetSprite($"rubble-{size}-{variant}", true);

                if (sprite) rubbleSprites[size].Add(sprite);
                else if (variant == 0) break;
                else continue;      
            }
        }
    }


    public static GameObject CreateRubble(Vector2 position, int size) {
        GameObject rubbleGameObject = Object.Instantiate(rubblePrefab, position, Quaternion.Euler(0, 0, Mathf.Ceil(Random.Range(0f, 360f) / 90f) * 90f));
        rubbleGameObject.GetComponent<SpriteRenderer>().sprite = rubbleSprites[size][Random.Range(0, rubbleSprites[size].Count)];
        return rubbleGameObject;
    }
}

using Frontiers.Content;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Assets;

public class LandPadBlock : Block {
    static int allyLandPadCount;
    static int enemyLandPadCount;

    public new LandPadBlockType Type { get => (LandPadBlockType)base.Type; protected set => base.Type = value; }
    public List<Unit> landedUnits = new();

    public bool IsFull() => landedUnits.Count >= Type.unitCapacity;

    public bool CanLand(Unit unit) => unit.Type.size <= Type.unitSize && !IsFull() && !landedUnits.Contains(unit) && unit.GetTeam() == teamCode;

    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        base.Set(position, rotation, type, id, teamCode);

        if (IsLocalTeam()) allyLandPadCount++;
        else enemyLandPadCount++;
    }

    protected override void SetSprites() {
        base.SetSprites();

        int number = IsLocalTeam() ? allyLandPadCount : enemyLandPadCount;
        transform.CreateNumbers(number);
    }

    public void Land(Unit unit) {
        unit.transform.position = GetLandPosition(landedUnits.Count);
        landedUnits.Add(unit);
    }

    private Vector2 GetLandPosition(int index) {
        return Type.landPositions.Length <= index
            ? GetGridPosition() + (0.5f * Type.size * Vector2.one)
            : GetGridPosition() + Type.landPositions[index];
    }

    public void TakeOff(Unit unit) {
        if (landedUnits.Contains(unit)) landedUnits.Remove(unit);
    }

    public override void OnDestroy() {
        base.OnDestroy();

        if (IsLocalTeam()) allyLandPadCount--;
        else enemyLandPadCount--;
    }
}

public static class LandingPadNumberGenerator {
    public static Sprite[] numberSprites;

    static LandingPadNumberGenerator() {
        numberSprites = new Sprite[10];

        for (int i = 0; i < 10; i++) {
            numberSprites[i] = AssetLoader.GetSprite("landingPad-numbers-" + i);
        }
    }

    public static void CreateNumbers(this Transform transform, int number) {
        number = Mathf.Clamp(number, 0, 99);
        int num1 = number % 10;
        int num2 = (number - num1) / 10;

        Material numberMaterial = AssetLoader.GetAsset<Material>("SpriteGlow");

        Transform numberTransform = new GameObject("Number 1", typeof(SpriteRenderer)).transform;
        SpriteRenderer spriteRenderer = numberTransform.GetComponent<SpriteRenderer>();

        numberTransform.parent = transform;
        numberTransform.position = transform.position + new Vector3(0.25f, 0, 0);
        numberTransform.localRotation = Quaternion.identity;

        spriteRenderer.sprite = numberSprites[num1];
        spriteRenderer.sortingLayerName = "Blocks";
        spriteRenderer.sortingOrder = 5;
        spriteRenderer.material = numberMaterial;

        numberTransform = new GameObject("Number 2", typeof(SpriteRenderer)).transform;
        spriteRenderer = numberTransform.GetComponent<SpriteRenderer>();

        numberTransform.parent = transform;
        numberTransform.position = transform.position + new Vector3(-0.25f, 0, 0);
        numberTransform.localRotation = Quaternion.identity;

        spriteRenderer.sprite = numberSprites[num2];
        spriteRenderer.sortingLayerName = "Blocks";
        spriteRenderer.sortingOrder = 5;
        spriteRenderer.material = numberMaterial;
    }
}
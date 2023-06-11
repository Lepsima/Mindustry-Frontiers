using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Generators : MonoBehaviour {

    public static Texture2D Generate() {
        /*
        for (int size = 0; size < 10; size++) {
            for (int i = 0; i < 3; i++) {
                ScorchGenerator gen = new();
                int multiplier = 30;
                float ss = size * multiplier / 20f;

                gen.seed = Random.Range(0, 100000);
                gen.size += size * multiplier;
                gen.scale = gen.size / 80f * 18f;
                //gen.nscl -= size * 0.2f;
                gen.octaves += ss / 3f;
                gen.pers += ss / 10f / 5f;

                gen.scale += Random.Range(0f, 3f);
                gen.scale -= ss * 2f;
                gen.nscl -= Random.Range(0f, 1f);

                Texture2D texture = gen.Generate();
            }
        }
        */

        ScorchGenerator gen = new();
        int multiplier = 3;
        int size = 2;
        float ss = size * multiplier / 20f;

        gen.seed = Random.Range(0, 100000);
        gen.size += size * multiplier;
        gen.scale = gen.size / 80f * 18f;
        //gen.nscl -= size * 0.2f;
        gen.octaves += ss / 3f;
        gen.pers += ss / 10f / 5f;

        gen.scale += Random.Range(0f, 3f);
        gen.scale -= ss * 2f;
        gen.nscl -= Random.Range(0f, 1f);

        return gen.Generate();
    }
}

public class ScorchGenerator {
    public int size = 80, seed = 0;
    public float scale = 18f, pow = 2f, octaves = 4f, pers = 0.4f, add = 2f, nscl = 4.5f;
    public Color color = Color.black;

    public Texture2D Generate() {
        Texture2D texture = new(size, size);

        for (int x = 0; x < size; x++) {
            for (int y = 0; y < size; y++) {
                float distance = Vector2.Distance(new Vector2(x, y), Vector2.one * size / 2) / (size / 2f);
                float scaled = Mathf.Abs(distance - 0.5f) * 5f + add;
                scaled -= Noise(Angle(x, y, size / 2, size / 2) * nscl);

                if (scaled < 1.5f) texture.SetPixel(x, y, color);
                else texture.SetPixel(x, y, new Color(0, 0, 0, 0));
            }
        }

        texture.Apply();
        return texture;
    }

    private float Noise(float angle) {
        return Mathf.Pow(Noise2d(seed, octaves, pers, 1 / scale, Mathf.Cos(angle) * size / 2f + size / 2f, Mathf.Sin(angle) * size / 2f + size / 2f), pow);
    }

    public static float Angle(float x, float y, float x2, float y2) {
        float ang = Mathf.Atan2(x2 - x, y2 - y) * 57.29578f;
        if (ang < 0) ang += 360f;
        return ang;
    }

    public static float Noise2d(int seed, float octaves, float persistence, float scale, float x, float y) {
        float total = 0;
        float frequency = scale;
        float amplitude = 1;

        // We have to keep track of the largest possible amplitude,
        // because each octave adds more, and we need a value in [-1, 1].
        float maxAmplitude = 0;

        for (int i = 0; i < octaves; i++) {
            //total += (raw2d(seed, x * frequency, y * frequency) + 1f) / 2f * amplitude;
            total += (Mathf.PerlinNoise(x * frequency + seed, y * frequency + seed) + 1f) / 2f * amplitude;

            frequency *= 2;
            maxAmplitude += amplitude;
            amplitude *= persistence;
        }

        return (float)(total / maxAmplitude);
    }
}
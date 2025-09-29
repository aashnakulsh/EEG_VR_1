using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class coolThemeManager : MonoBehaviour
{
    [Header("Theme Colors")]
    public Color cubeColor = Color.cyan;    // Cubes
    public Color cubeHighlightColor = Color.cyan;    // Cubes
    public Color tableCoolor = Color.gray;  // Table / props
    public Color uiColor = Color.magenta;  // UI & highlights
    public Color backgroundColor = Color.black; // Camera background
    public Color lightColor = Color.white;     // Directional light / ambient

    [Header("References")]
    public List<GameObject> cubes = new List<GameObject>();
    public GameObject table;
    public List<TextMeshProUGUI> textElements = new List<TextMeshProUGUI>();
    // public Light directionalLight;

    [Header("CubeHighlights")]

    public TrialManager tm;


    private void Start()
    {
        ApplyTheme();
    }

    public void ApplyTheme()
    {
        tm.defaultColor = cubeColor;
        tm.highlightColor = cubeHighlightColor;
        tm.ghostColor = cubeColor;
        // 1. Cubes
        foreach (GameObject cube in cubes)
        {
            if (cube != null)
            {
                Renderer rend = cube.GetComponent<Renderer>();
                if (rend != null)
                {
                    // rend.material.color = cubeColor;
                    // Optional: glow / emission
                    if (rend.material.HasProperty("_EmissionColor"))
                        rend.material.SetColor("_EmissionColor", cubeColor * 0.5f);
                }
            }
        }

        // 2. Table / Props
        if (table != null)
        {
            Renderer tableRend = table.GetComponent<Renderer>();
            if (tableRend != null)
                tableRend.material.color = tableCoolor;
        }

        // 3. UI Text
        foreach (var text in textElements)
        {
            if (text != null)
                text.color = uiColor;
        }

        // 5. Camera background
        // Camera.main.backgroundColor = backgroundColor;

        // // 6. Lighting
        // if (directionalLight != null)
        //     directionalLight.color = lightColor;

        // RenderSettings.ambientLight = lightColor * 0.2f; // subtle ambient tint
    }
}

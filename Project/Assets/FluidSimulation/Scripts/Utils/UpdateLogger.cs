using _2D_Shader_V4;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public class UpdateLogger : MonoBehaviour
{
    string currentFrameDeltas = "";
    [SerializeField] MainSimulationRunner trackedSimulation;
    [SerializeField] private bool trackUpdates = false;

    private List<string> logLines = new List<string>();
    private string fileNameStart = "update_times";
    [SerializeField] private string customFileName = "example"; // Set this in the Inspector
    private string filePath;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if(trackUpdates) {
            string finalDeltas = trackedSimulation.updateDeltas;

            logLines.Add(finalDeltas);
            Debug.Log(finalDeltas);

        } else if(logLines.Count > 1){
            createCSV();
            logLines.Insert(0, "pressure reset, projection, velocity advection, smoke advection, mergeObjects, rendering");
            File.WriteAllLines(filePath, logLines);
            Debug.Log($"Updates time log saved to: {filePath}");
            logLines.Clear(); // Reset for next session
        }
    }


    private void createCSV() {
        // Ensure .csv extension
        if (!customFileName.EndsWith(".csv"))
            customFileName += ".csv";


        // Save to same folder as script: Assets/Scripts/
        filePath = Path.Combine(Application.dataPath, "Experiments/FrameTimes", fileNameStart + "_" + customFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));

    }
}

using _2D_Shader_V4;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Unity.Mathematics;
using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

namespace Utils {
public class RecordFPS : MonoBehaviour {

    [SerializeField] MainSimulationRunner trackedSimulation;
    private List<string> logLines = new List<string>();
    private string fileNameStart = "frame_times";
    [SerializeField] private string customFileName = "example"; // Set this in the Inspector
    private string filePath;

    private bool firstTime = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() {

        //// Ensure .csv extension
        //if (!customFileName.EndsWith(".csv"))
        //    customFileName += ".csv";


        //// Save to same folder as script: Assets/Scripts/
        //filePath = Path.Combine(Application.dataPath, "Experiments/FrameTimes", fileNameStart + "_" + customFileName);
        //Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        //logLines.Add("Time (s),DeltaTime (s)");
    }

    // Update is called once per frame
    void LateUpdate() {
        bool isRunning = trackedSimulation.simulationPlaying;

        if (isRunning) {
            //int frame = Time.frameCount;
            float time = Time.time;
            float delta = Time.deltaTime;
            //float2 maxMin = trackedSimulation.FindMaxMinDivergence();  
            //float max = maxMin.x;
            //float min = maxMin.y;

            logLines.Add(string.Format(CultureInfo.InvariantCulture, "{0:F4},{1:F6}", time, delta));
            //Debug.Log($"{time:F4},{delta:F6}");
            //Debug.Log(string.Format(CultureInfo.InvariantCulture, "{0:F4},{1:F6}", time, delta));
            } else if (logLines.Count > 1) {
            // Save to CSV when recording stops
            createCSV();
            logLines.Insert(0, "Time (s),DeltaTime (s)");
            File.WriteAllLines(filePath, logLines);
            Debug.Log($"Frame time log saved to: {filePath}");
            logLines.Clear(); // Reset for next session
        }
    }

    private void createCSV(){
        // Ensure .csv extension
        if (!customFileName.EndsWith(".csv"))
            customFileName += ".csv";


        // Save to same folder as script: Assets/Scripts/
        filePath = Path.Combine(Application.dataPath, "Experiments/FrameTimes", fileNameStart + "_" + customFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        
    }
}
}
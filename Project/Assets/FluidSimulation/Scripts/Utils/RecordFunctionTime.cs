using _2D_Shader_V4;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;



namespace Utils {
public class RecordFunctionTime : MonoBehaviour
{
    public bool record = false;
    [SerializeField] MainSimulationRunner trackedSimulation;
    private List<string> logLines = new List<string>();
    private string fileNameStart = "function_times";
    [SerializeField] private string customFileName = "example"; // Set this in the Inspector
    private string filePath;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(record){
            double start = Time.realtimeSinceStartupAsDouble;
            trackedSimulation.CheckMaxMinClick();
            double end = Time.realtimeSinceStartupAsDouble;

            double startShader = Time.realtimeSinceStartupAsDouble;
            trackedSimulation.CheckMaxMinShaderClick();
            double endShader = Time.realtimeSinceStartupAsDouble;
            

            logLines.Add(string.Format(CultureInfo.InvariantCulture, "{0:F4},{1:F6}", end - start, endShader - startShader));
        } else if(logLines.Count > 1){

            // Save to CSV when recording stops
            createCSV();
            logLines.Insert(0, "CPU DeltaTime (s), Shader DeltaTime (s)");
            File.WriteAllLines(filePath, logLines);
            Debug.Log($"Function time log saved to: {filePath}");
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
}
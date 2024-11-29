using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;
using Vector2 = UnityEngine.Vector2;

namespace test {



public class cSharp_ShaderTest : MonoBehaviour
{

    public ComputeShader shaderTest;

    public RenderTexture renderTex;

    // public int resolution = 512;
    public Vector2Int resolution = new Vector2Int(512, 512); //(width, height)
    // public int width = 512;
    // public int height = 512;


    private float[] support;

    private int colorEvolKernel;
    private int CSMainBuffered;

    // Start is called before the first frame update
    void Start()
    {
        SetupRenderTexture();



        int CSMain = SetupCSMainKernel();
        shaderTest.Dispatch(CSMain, resolution.x / 8, resolution.y / 8, 1);

        this.GetComponent<Renderer>().material.mainTexture = renderTex;

        //cam.targetTexture = renderTex;

        colorEvolKernel = SetupColorEvolutionKernel();
        // SetupNRunFillBlack();
        // SetupNRunMagic();
        support = new float[resolution.x * resolution.y];
        Array.Fill(support, 0.0f);
        CSMainBuffered = SetupCSMainBuffered();

    }

    // Update is called once per frame
    void Update()
    {
        float startTime = Time.realtimeSinceStartup;

        shaderTest.Dispatch(colorEvolKernel, renderTex.width / 8, renderTex.height / 8, 1);
        // shaderTest.Dispatch(CSMainBuffered, resolution.x / 8, resolution.y / 8, 1);

        float endTime = Time.realtimeSinceStartup;
    }


    private void SetupRenderTexture() {
        // int actualWidth = 1024;
        // int actualHeight = 1024;

        // while(actualWidth < resolution.x || actualHeight < resolution.y){
        //     break;
        // }


        renderTex = new RenderTexture(resolution.x, resolution.y, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SNorm);
        renderTex.enableRandomWrite = true;
        renderTex.Create();
    }

    private int SetupColorEvolutionKernel() {
        int kernel = shaderTest.FindKernel("colorEvolution");

        shaderTest.SetTexture(kernel, "Result", renderTex);

        return kernel;
    }

    private int SetupCSMainKernel() {
        int CSMain = shaderTest.FindKernel("CSMain");

        shaderTest.SetTexture(CSMain, "Result", renderTex);
        shaderTest.SetFloat("Resolution", renderTex.width);
        return CSMain;
    }

    private int SetupCSMainBuffered() {
        int kernel = shaderTest.FindKernel("CSMainBuffered");

        ComputeBuffer support_CB = new ComputeBuffer(support.Length, sizeof(float));
        support_CB.SetData(support);

        shaderTest.SetTexture(kernel, "Result", renderTex);
        shaderTest.SetBuffer(kernel, "support", support_CB);
        // shaderTest.SetInt("resolution", resolution);
        shaderTest.SetInts("resolution", new int[] {resolution.x, resolution.y});
        // support_CB.Release();

        return kernel;
    }

    private void SetupNRunFillBlack() {
        int kernel = shaderTest.FindKernel("FillBlack");

        shaderTest.SetTexture(kernel, "Result", renderTex);
        shaderTest.Dispatch(kernel, renderTex.width / 8, renderTex.height / 8, 1);
    }

    private void SetupNRunMagic()
    {
        int kernel = shaderTest.FindKernel("Magic");

        shaderTest.SetTexture(kernel, "Result", renderTex);
        shaderTest.Dispatch(kernel, renderTex.width / 64, renderTex.height, 1);
    }

}
}
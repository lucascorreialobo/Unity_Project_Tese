using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;


namespace _2D_Shader_V2
{

    public class CS_FluidRenderer : MonoBehaviour
{
    public ComputeShader projectionShader;
    public ComputeShader advectionShader;
    public ComputeShader colorShader;

    public RenderTexture fluidTex;
    public SimulationState state;

    private ComputeBuffer cb;

    public int resX = 14;
    public int resY = 8;

    // Start is called before the first frame update
    void Start()
    {
        state = new SimulationState(resX, resY);
        // cb = new ComputeBuffer(64 * 64, sizeof(float));

        int kernel = projectionShader.FindKernel("CSMain");

        //texture
        fluidTex = new RenderTexture(resX, resY, 0);
        fluidTex.enableRandomWrite = true;
        fluidTex.Create();

        SetupProjectShader();
        SetupColorShader();

        // projectionShader.SetBuffer(kernel, "cb", cb);
        // advectionShader.SetBuffer(kernel, "cb", cb);
        // colorShader.SetBuffer(kernel, "cb", cb);
        // float startTime = Time.realtimeSinceStartup;
        // float endTime = Time.realtimeSinceStartup;
        // Debug.Log("Set buffer took " + (endTime - startTime) + " seconds to finnish");

        colorShader.SetTexture(kernel, "Result", fluidTex);


        // GetComponent<Renderer>().material.mainTexture = fluidTex;

        // projectionShader.Dispatch(kernel, 64 / 8, 64 / 8, 1);
        // advectionShader.Dispatch(kernel, 64 / 8, 64 / 8, 1);
        // colorShader.Dispatch(kernel, 64 / 8, 64 / 8, 1);
    }

    void SetupProjectShader(){
        projectionShader.SetInt("_simResX", state.simResolution.x);
        projectionShader.SetInt("_simResY", state.simResolution.y);

        int kernel = projectionShader.FindKernel("CSMain");

        //buffers
        projectionShader.SetBuffer(kernel, "_velocityV", state.velocityV);
        projectionShader.SetBuffer(kernel, "_velocityH", state.velocityH);
        // projectionShader.SetBuffer(kernel, "_pressure", state.pressure);
        projectionShader.SetBuffer(kernel, "_isFluid", state.type);
    }

    void SetupColorShader(){
        colorShader.SetInt("_simResX", state.simResolution.x);
        colorShader.SetInt("_simResY", state.simResolution.y);

        int kernel = colorShader.FindKernel("CSMain");

        //buffers
        colorShader.SetBuffer(kernel, "_velocityV", state.velocityV);
        colorShader.SetBuffer(kernel, "_velocityH", state.velocityH);
        // colorShader.SetBuffer(kernel, "_pressure", state.pressure);
        colorShader.SetBuffer(kernel, "_isFluid", state.type);
    }


    void CallProjectShader(){
        int height = state.simResolution.x;
        int width = state.simResolution.y;
        int kernel = projectionShader.FindKernel("CSMain");

        projectionShader.SetInts("_offset", new int[]{0,0});
        projectionShader.Dispatch(kernel,  ((height / 2) / 8) + 1, ((width / 2) / 8 + 1), 1);
        projectionShader.SetInts("_offset", new int[]{1,0});
        projectionShader.Dispatch(kernel,  ((height / 2) / 8) + 1, ((width / 2) / 8 + 1), 1);
        projectionShader.SetInts("_offset", new int[]{0,1});
        projectionShader.Dispatch(kernel,  ((height / 2) / 8) + 1, ((width / 2) / 8 + 1), 1);
        projectionShader.SetInts("_offset", new int[]{1,1});
        projectionShader.Dispatch(kernel,  ((height / 2) / 8) + 1, ((width / 2) / 8 + 1), 1);
    }

    void CallColorShader(){
        int height = state.simResolution.x;
        int width = state.simResolution.y;
        int kernel = projectionShader.FindKernel("CSMain");

        colorShader.Dispatch(0, height / 8, width / 8, 1);
    }


    // Update is called once per frame
    void Update()
    {
        CallProjectShader();
        CallColorShader();
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        //Dispatch ColorShader
        // colorShader.Dispatch(kernel, height, width, 1);


        Graphics.Blit(fluidTex, dest);
    }

    void OnDestroy(){
        state.ReleaseComputeBuffers();
        // cb.Release();
    }
}
}
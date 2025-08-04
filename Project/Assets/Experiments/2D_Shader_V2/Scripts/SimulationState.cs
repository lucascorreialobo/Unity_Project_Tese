using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace _2D_Shader_V2 {



public class SimulationState
{
    public readonly Vector2Int simResolution; //by number of cells (height, width)

    

    public ComputeBuffer pressure;
    public ComputeBuffer type;
    public ComputeBuffer velocityV;
    public ComputeBuffer velocityH;
    // public ComputeBuffer newV;
    // public ComputeBuffer newH;
    public ComputeBuffer smoke;
    // public ComputeBuffer newSmoke;

    public SimulationState(int width, int height){
        simResolution = new Vector2Int(width, height);
        int trueResolutionWidth = width + 2;
        int trueResolutionHeight = height + 2;

        float[] pressureA = new float[trueResolutionWidth * trueResolutionHeight];
        int[] typeA = new int[trueResolutionWidth * trueResolutionHeight];
        float[] velocityVA = new float[(trueResolutionWidth + 1) * trueResolutionHeight];
        float[] velocityHA = new float[trueResolutionWidth * (trueResolutionHeight + 1)];
        float[] smokeA = new float[trueResolutionWidth * trueResolutionHeight];

        Array.Fill(typeA, 1);
        Array.Fill(pressureA, 0.0f);
        Array.Fill(velocityVA, 0.0f);
        Array.Fill(velocityHA, 1.0f);
        Array.Fill(smokeA, 0);

        for(int x = 0; x < trueResolutionHeight; x++){
            for(int y = 0; y < trueResolutionWidth; y++){
                velocityHA[x * (trueResolutionWidth + 1) + y] = (x + y) % 2;
            }
        }

        pressure = new ComputeBuffer(pressureA.Length, sizeof(float));
        pressure.SetData(pressureA);

        velocityV = new ComputeBuffer(velocityVA.Length, sizeof(float));
        velocityV.SetData(velocityVA);

        velocityH = new ComputeBuffer(velocityHA.Length, sizeof(float));
        velocityH.SetData(velocityHA);

        type = new ComputeBuffer(typeA.Length, sizeof(int));
        type.SetData(typeA);

        // newH = new ComputeBuffer(velocityH.Length, sizeof(float));
        // newH.SetData(velocityH);

        // newV = new ComputeBuffer(velocityV.Length, sizeof(float));
        // newV.SetData(velocityV);

        smoke = new ComputeBuffer(smokeA.Length, sizeof(float));
        smoke.SetData(smokeA);

        // newSmoke = new ComputeBuffer(smoke.Length, sizeof(float));
        // newSmoke.SetData(smoke);


    }

    public void ReleaseComputeBuffers(){
        pressure.Release();
        type.Release();
        velocityV.Release();
        velocityH.Release();
        // newV.Release();
        // newH.Release();
        smoke.Release();
        // newSmoke.Release();
    }
}

}
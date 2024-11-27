using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace cSharp {
public class SimulationState
{
    public readonly Vector2Int simResolution; //by number of cells (height, width)

    public Buf2<float> press;
    public ComputeBuffer pressure;
    public ComputeBuffer type;
    public ComputeBuffer velocityV;
    public ComputeBuffer velocityH;
    // public ComputeBuffer newV;
    // public ComputeBuffer newH;
    public ComputeBuffer smoke;
    // public ComputeBuffer newSmoke;

    public SimulationState(int height, int width){
        simResolution = new Vector2Int(height, width);
        int trueResolutionHeight = height + 2;
        int trueResolutionWidth = width + 2;

        float[] pressureA = new float[trueResolutionHeight * trueResolutionWidth];
        int[] typeA = new int[trueResolutionHeight * trueResolutionWidth];
        float[] velocityVA = new float[(trueResolutionHeight + 1) * trueResolutionWidth];
        float[] velocityHA = new float[trueResolutionHeight * (trueResolutionWidth + 1)];
        float[] smokeA = new float[trueResolutionHeight * trueResolutionWidth];

        Array.Fill(typeA, 1);
        Array.Fill(pressureA, 0.0f);
        Array.Fill(velocityVA, 0.0f);
        Array.Fill(velocityHA, 1.0f);
        Array.Fill(smokeA, 0);

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
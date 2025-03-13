using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using Utils;

namespace _2D_Shader_V4 {




public class SimulationState
{
    public Buf2<float> pressure;  //Pascal (Pa) - N/m2
    public Buf2<float> velocityV; //m/s
    public Buf2<float> velocityH;
    public Buf2<float> wall; // 0 - wall | 1 - fluid
    public Buf2<float> smoke;
    public Buf2<int> objectsSeperated;
    public Buf2<float> objectsMerged;

    public int numberOfObjects; //max number is sizeOf(int) * 8 = number of bits
    public int2 simRes;

    public SimulationState(int2 simRes, int numberOfObjects = 5){
        pressure = new Buf2<float>(simRes.x, simRes.y);
        velocityV = new Buf2<float>(simRes.x, simRes.y);
        velocityH = new Buf2<float>(simRes.x, simRes.y);
        wall = new Buf2<float>(simRes.x, simRes.y);
        smoke = new Buf2<float>(simRes.x, simRes.y);
        objectsSeperated = new Buf2<int>(simRes.x, simRes.y);
        objectsMerged = new Buf2<float>(simRes.x, simRes.y);

        this.numberOfObjects = numberOfObjects;
        this.simRes = simRes;

        InizializeWalls();
        InizializePressure();
    }

    private void InizializeWalls(){
        for(int x = 0; x < simRes.x; x++){ 
            for(int y = 0; y < simRes.y; y++){
                wall[x, y] = 1;
                objectsMerged[x, y] = 1;
                objectsSeperated[x, y] = -1; //-1 => all bits are 1
                if (x == 0 || x == simRes.x - 1 || y == 0 || y == simRes.y - 1){ //simulation borders
                    wall[x, y] = 0;
                }
            }
        }
    }

    private void InizializePressure(){
        for (int x = 0; x < simRes.x; x++)
        {
            for (int y = 0; y < simRes.y; y++)
            {
                pressure[x,y] = 0.0f;
            }
        }
    }

    public void allToGPU(){
        pressure.ToGPU();
        velocityV.ToGPU();
        velocityH.ToGPU();
        wall.ToGPU();
        smoke.ToGPU();
        objectsMerged.ToGPU();
        objectsSeperated.ToGPU();
    }


    public void allFromGPU() {
        pressure.FromGPU();
        velocityV.FromGPU();
        velocityH.FromGPU();
        wall.FromGPU();
        smoke.FromGPU();
        objectsMerged.FromGPU();
        objectsSeperated.FromGPU();
    }


    public void Destroy(){
        pressure.Release();
        velocityV.Release();
        velocityH.Release();
        wall.Release();
        smoke.Release();
        objectsMerged.Release();
        objectsSeperated.Release();
    }

    public void addObject(int objectIndex, int2 gridCoord) {
        objectsSeperated.FromGPU();
        int obj = objectsSeperated[gridCoord.x, gridCoord.y];
        int isFluid = (obj >> objectIndex) & 1;

        if(isFluid == 1)
            objectsSeperated[gridCoord.x, gridCoord.y] = obj & ~(1 << objectIndex);
        else
            objectsSeperated[gridCoord.x, gridCoord.y] = obj | (1 << objectIndex);


        objectsSeperated.ToGPU();
    }
}
}
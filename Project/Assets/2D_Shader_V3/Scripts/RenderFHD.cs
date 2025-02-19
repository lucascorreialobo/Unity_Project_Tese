using System;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Scripting;
using Utils;



namespace _2D_Shader_V3 {




public class RenderFHD : MonoBehaviour
{
    

    [Header("Constants")]
    [SerializeField] private ComputeShader viewRenderer;
    [SerializeField] private ComputeShader viewRendererBorders;
    [SerializeField] private ComputeShader projectionShader;
    [SerializeField] private RenderTexture tex;
    [SerializeField] private GameObject plane;

    [SerializeField] private Vector2Int viewRes = new Vector2Int(500, 500);
    [SerializeField] private Vector2Int simRes = new Vector2Int(50, 50);

    // [SerializeField] private float[,] color;


    private Buf2<float> property;
    private SimulationState simState;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InitializeSimState();

        //resizes the plane to match the view resolution
        ResizePlane();

        iniciateProperty();

        //Initialize texture
        CreateTex();

        


        int kernel = viewRenderer.FindKernel("CSMain");
        viewRenderer.SetTexture(kernel, "Result", tex);
        // viewRenderer.SetBuffer(kernel, "_property", property.GetComputeBuffer());
        viewRenderer.SetBuffer(kernel, "_property", simState.pressure.GetComputeBuffer());
        viewRenderer.SetBuffer(kernel, "_propertyBordersV", simState.velocityV.GetComputeBuffer());
        viewRenderer.SetBuffer(kernel, "_propertyBordersH", simState.velocityH.GetComputeBuffer());
        viewRenderer.SetInts("_viewRes", new int[]{viewRes.x, viewRes.y});
        viewRenderer.SetInts("_simRes", new int[]{simRes.x, simRes.y});


        viewRenderer.Dispatch(kernel, (viewRes.x / 8) + 1, (viewRes.y / 8) + 1, 1);




        // RunProjection();

    }

    // Update is called once per frame
    void Update()
    {


        int kernel = viewRenderer.FindKernel("CSMain");
        // viewRenderer.SetBuffer(kernel, "_property", simState.pressure.GetComputeBuffer());
        // viewRenderer.SetBuffer(kernel, "_propertyBorders", simState.velocityV.GetComputeBuffer());
        viewRenderer.Dispatch(kernel, (viewRes.x / 8) + 1, (viewRes.y / 8) + 1, 1);

        RunProjection();
    }

    void OnDestroy(){
        simState.Destroy();
        property.Release();
    }

    //Acording to simRes.
    void ResizePlane(){
        float simAspectRatio = (float)simRes.x / simRes.y;
        if(simAspectRatio < 1)
            plane.transform.localScale = new Vector3(simAspectRatio, 1, 1);
        else
            plane.transform.localScale = new Vector3(1, 1, 1 / simAspectRatio);
    }

    void iniciateProperty(){
        //Initialize property
        property = new Buf2<float>(simRes.x, simRes.y);
        for (int x = 0; x < simRes.x; x++)
        {
            for (int y = 0; y < simRes.y; y++)
            {
                property[x, y] = (x + y) % 2;
                // property[x,y] = x < 20 ? 0 : 1;
            }
        }
        property.ToGPU();
    }

    void CreateTex(){
        tex = new RenderTexture(viewRes.x, viewRes.y, 0)
        {
            enableRandomWrite = true
        };
        tex.Create();

        plane.GetComponent<Renderer>().material.mainTexture = tex;
    }

    void RunProjection(){
        int kernel = projectionShader.FindKernel("Projection");
        projectionShader.SetBuffer(kernel, "_pressure", simState.pressure.GetComputeBuffer());
        projectionShader.SetBuffer(kernel, "_type", simState.type.GetComputeBuffer());
        projectionShader.SetBuffer(kernel, "_velocityV", simState.velocityV.GetComputeBuffer());
        projectionShader.SetBuffer(kernel, "_velocityH", simState.velocityH.GetComputeBuffer());
        projectionShader.SetInts("_simRes", new int[] { simRes.x, simRes.y });

        projectionShader.SetInts("_offset", new int[] { 0, 0 });
        projectionShader.Dispatch(kernel, (simRes.x / 8) + 1, (simRes.y / 8) + 1, 1);

        // projectionShader.SetInts("_offset", new int[] { 0, 1 });
        // projectionShader.Dispatch(kernel, (simRes.x / 8) + 1, (simRes.y / 8) + 1, 1);

        // projectionShader.SetInts("_offset", new int[] { 1, 0 });
        // projectionShader.Dispatch(kernel, (simRes.x / 8) + 1, (simRes.y / 8) + 1, 1);

        // projectionShader.SetInts("_offset", new int[] { 1, 1 });
        // projectionShader.Dispatch(kernel, (simRes.x / 8) + 1, (simRes.y / 8) + 1, 1);

    }

    void InitializeSimState(){
        simState = new SimulationState(simRes);

        // for (int x = 0; x < simRes.x; x++)
        // {
        //     simState.velocityV[x, 0] = 0.5f;
        //     simState.velocityV[x, 1] = 0.5f;
        //     simState.velocityV[x, 2] = 0.5f;
        //     simState.velocityV[x, 3] = 0.5f;
        //     simState.velocityV[x, 4] = 0.5f;
        //     simState.velocityV[x, 5] = 0.5f;
        // }
        // for (int y = 0; y < simRes.y; y++)
        // {
        //     simState.velocityH[0, y] = 1;
        //     simState.velocityH[1, y] = 1;
        //     // property[x,y] = x < 20 ? 0 : 1;
        // }

        for (int x = 0; x < simRes.x; x++)
        {
            for (int y = 0; y < simRes.y; y++)
            {
                simState.type[x, y] = 1;
                if(x == 0 || x == simRes.x || y == 0 || y == simRes.y)
                    simState.type[x, y] = 0;

                simState.pressure[x,y] = 1f;
                if(x < 40){
                    simState.velocityV[x,y] = 1;
                }
                    
                // simState.velocityV[x,y] = (float) (x + y) / (simRes.x + simRes.y);
                // simState.velocityH[x,y] = (float) (x + y) / (simRes.x + simRes.y);
                // Debug.Log(simState.velocityV[x,y]);
            }
        }

        simState.pressure.ToGPU();
        simState.type.ToGPU();
        simState.velocityV.ToGPU();
        simState.velocityH.ToGPU();
        simState.smoke.ToGPU();
    }
}
}
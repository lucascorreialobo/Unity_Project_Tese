using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;
using Random = UnityEngine.Random;

public class CSharp_FluidShader : MonoBehaviour
{
    public RenderTexture renderTex;
    public GameObject plane;


    public ComputeShader fluidShader;

    //public RenderTexture renderTex;

    public GameObject square;
    private GameObject[] visualGrid;

    public float gridSpacing = 0.01f; //size of one grid cell edge
    private float origin; //game coordinates of top left corner of grid

    //simulated width/height; an extra layer of cell will be added on each edge to deal with border conditions
    private int resolutionWidth = 128; 
    private int resolutionHeight = 128;
    private int trueResolutionWidth;  
    private int trueResolutionHeight; 

    private float[] pressure;
    private int[] type;             // 0 - fluid  |  1 - wall
    private float[] velocityV;  
    private float[] velocityH;

    public RenderTexture fluidTex;
    private Kernels kernels = new Kernels();
    private CBs cBuffers = new CBs();

    private class Kernels {
        public int solveIncompressibility = -1;
        public int extrapolateH = -1;
        public int extrapolateV = -1;
        public int advectVel = -1;
        public int advectSmoke = -1;
    }
    private class CBs {  //compute buffers
        public ComputeBuffer pressure;
        public ComputeBuffer type;
        public ComputeBuffer velocityV;
        public ComputeBuffer velocityH;
    }
    private enum FIELD {
        U_FIELD,
        V_FIELD,
        S_FIELD
    }
    //  pressure will be stored for each pixel
    //  velocity is stored in the edges between cells. 
    //      Edges between horizantal cells are velocityH
    //      Edges between vertical cells are velocityV
    // In the general case:
    //      velocityH will have size (width+1, height)
    //      velocityV will have size (width, height+1)
    //
    // This is shown in the given case where : velocityH (3,4)  |  velocityV (4,3)



    //               V (0,2)
    //    _________ _________ _________ 
    //   |         |         |         |
    //   |         |         |         |
    //   |         |         |         |
    //   |_________|_________|_________|
    //   |         |         |         |
    //   |         |         |         |
    //   |         |         |         |
    //   |_________|_________|_________|
    //   | V (3,0) |         |         |
    //   |         |         |         | ->
    //   |         |         |         | H (3,4)
    //   |_________|_________|_________|




    // Start is called before the first frame update
    void Start()
    {
        trueResolutionWidth = resolutionWidth + 2;
        trueResolutionHeight = resolutionHeight + 2;

        initializeVariables();
        //callCSMain();
        SetupSolveIncompressibilityShader();
        SetupExtrapolateShaders();


        // callSolveIncompressibilityShader(false);





    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void initializeVariables() {
        //resolutionWidth = 1000;
        //resolutionHeight = 1000;

        pressure = new float[trueResolutionHeight * trueResolutionWidth];
        type = new int[trueResolutionHeight * trueResolutionWidth];
        velocityV = new float[(trueResolutionHeight + 1) * trueResolutionWidth];
        velocityH = new float[trueResolutionHeight * (trueResolutionWidth + 1)];

        Array.Fill(type, 0);

        for (int i = 0; i < trueResolutionHeight * trueResolutionWidth; i++) {
            pressure[i]  = Random.Range(0, 10.0f);
            velocityV[i] = Random.Range(0, 10.0f);
            velocityH[i] = Random.Range(0, 10.0f);
            //type[i]      = Random.Range(0, 2);
       }

       //Due to the possibly different sizes assign the last values seperatly
       for( int i = 0; i < trueResolutionHeight; i++) {
            velocityH[trueResolutionHeight * trueResolutionWidth + i] = Random.Range(0, 10.0f);
       }
       for (int i = 0; i < trueResolutionWidth; i++) {
            velocityV[trueResolutionHeight * trueResolutionWidth + i] = Random.Range(0, 10.0f);
       }

    }

    void createGrid() {
        visualGrid = new GameObject[trueResolutionHeight * trueResolutionWidth];
        origin = trueResolutionWidth * gridSpacing / 2f * -1f;
        square.transform.localScale = new Vector3(gridSpacing, gridSpacing, gridSpacing);

        for (int i = 0; i < trueResolutionHeight; i++) {
            for (int j = 0; j < trueResolutionWidth; j++) {


                visualGrid[i * trueResolutionWidth + j] = Instantiate(square, globalPos(i, j), Quaternion.identity);
                //visualGrid[i * resolutionWidth + j].GetComponent<Renderer>().material.color = new Color(100, 100, 100);
            }
        }
    }

    private Vector3 globalPos(float i, float j) {
        float x = origin + (i * gridSpacing);
        float y = origin + (j * gridSpacing);

        return new Vector3(x, y, 0);
    }

    private int getCoordH_Left(int x, int y) {
        return x * trueResolutionWidth + y + x; // + x; is to account for the last edge of a row
    }
    private int getCoordH_Right(int x, int y) {
        return x * trueResolutionWidth + y + x + 1; // + x; is to account for the last edge of a row
    }
    private int getCoordV_Up(int x, int y) {
        return x * trueResolutionWidth + y;
    }
    private int getCoordV_Down(int x, int y) {
        return x * trueResolutionWidth + y + 1;
    }

    void SetupSolveIncompressibilityShader() {

        int kernel = fluidShader.FindKernel("solveIncompressibility");
        kernels.solveIncompressibility = kernel;

        cBuffers.pressure = new ComputeBuffer(pressure.Length, sizeof(float));
        cBuffers.pressure.SetData(pressure);

        cBuffers.velocityV = new ComputeBuffer(velocityV.Length, sizeof(float));
        cBuffers.velocityV.SetData(velocityV);

        cBuffers.velocityH = new ComputeBuffer(velocityH.Length, sizeof(float));
        cBuffers.velocityH.SetData(velocityH);

        cBuffers.type = new ComputeBuffer(type.Length, sizeof(int));
        cBuffers.type.SetData(type);

        //buffers
        fluidShader.SetBuffer(kernel, "_velocityV", cBuffers.velocityV);
        fluidShader.SetBuffer(kernel, "_velocityH", cBuffers.velocityH);
        fluidShader.SetBuffer(kernel, "_pressure",  cBuffers.pressure);
        fluidShader.SetBuffer(kernel, "_type",      cBuffers.type);

        //texture
        fluidTex = new RenderTexture(resolutionWidth, resolutionHeight, 0);
        fluidTex.enableRandomWrite = true;
        fluidTex.Create();
        fluidShader.SetTexture(kernel, "_color", fluidTex);

        //ints
        fluidShader.SetInt("resolutionWidth", resolutionWidth);
        fluidShader.SetInt("resolutionHeight", resolutionHeight);

        plane.GetComponent<Renderer>().material.mainTexture = fluidTex;
    }

    void callSolveIncompressibilityShader(bool getData) {

        float startTime = Time.realtimeSinceStartup;
        for(int i = 0; i < 100; i++)
            fluidShader.Dispatch(kernels.solveIncompressibility, resolutionHeight / 8, resolutionHeight / 8, 1);
        float endTime  = Time.realtimeSinceStartup;

        Debug.Log("Dispatch took " + (endTime - startTime) + " seconds to finnish");

        
        if(getData){
            cBuffers.pressure.GetData(pressure);
            cBuffers.type.GetData(type);
            cBuffers.velocityV.GetData(velocityV);
            cBuffers.velocityH.GetData(velocityH);
        }
    }

    private void SetupExtrapolateShaders(){

        int kernel = fluidShader.FindKernel("extrapolateH");
        kernels.extrapolateV = kernel;
        fluidShader.SetBuffer(kernel, "_velocityH", cBuffers.velocityH);


        kernel = fluidShader.FindKernel("extrapolateV");
        kernels.extrapolateV = kernel;
        fluidShader.SetBuffer(kernel, "_velocityV", cBuffers.velocityV);

    }
    private void extrapolateShaders() {

        fluidShader.Dispatch(kernels.extrapolateH, resolutionHeight / 32, 1, 1);
        fluidShader.Dispatch(kernels.extrapolateH, resolutionWidth / 32, 1, 1);


        // for(int i = 0; i < trueResolutionHeight; i++) {
        //     velocityH[getCoordH_Left(i, 0)] = velocityH[getCoordH_Right(i, 0)];
        //     //TODO: make sure its width-1 and not width - 2
        //     velocityH[getCoordH_Right(i, trueResolutionWidth - 2)] = velocityH[getCoordH_Left(i, trueResolutionWidth - 2)];
        // }
        // for (int i = 0; i < trueResolutionWidth; i++) {
        //     velocityH[getCoordV_Up(0, i)] = velocityH[getCoordV_Down(0, i)];
        //     //TODO: make sure its height-1 and not height - 2
        //     velocityH[getCoordV_Up(trueResolutionHeight - 2, i)] = velocityH[getCoordV_Down(trueResolutionHeight - 2, i)];
        // }
    }

    //get vector (velocityX || velocityY || smoke_density) on point x,y 
    private float sampleField(float x, float y, FIELD fieldType) {




        return 0.0f;
    }


    private float avgU(int i, int j) {
        return 0.0f;
    }

    private float avgV(int i, int j) {
        return 0.0f;
    }

    private void advectVel() {

        float[] newH = new float[velocityH.Length];
        float[] newV = new float[velocityV.Length];

        Array.Copy(velocityH, newH, velocityH.Length);
        Array.Copy(velocityV, newV, velocityV.Length);



    }


    private void callCSMain() {
        if (fluidShader != null) {
            renderTex = new RenderTexture(256, 256, 24);
            renderTex.enableRandomWrite = true;
            renderTex.Create();
        }
        int CSMain = fluidShader.FindKernel("CSMain");

        fluidShader.SetTexture(CSMain, "Result", renderTex);
        fluidShader.SetFloat("Resolution", renderTex.width);
        fluidShader.Dispatch(CSMain, renderTex.width / 8, renderTex.height / 8, 1);

        //Texture2D t2d = new Texture2D(256, 256);
        //RenderTexture.active = renderTex;
        //t2d.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
        //t2d.Apply();

        plane.GetComponent<Renderer>().material.mainTexture = renderTex;
        //Graphics.Blit(renderTex, destination);
    }

    public void simulate() {
        if (fluidShader == null) return;

        Array.Fill(pressure, 0.0f);
        callSolveIncompressibilityShader(false);

        extrapolateShaders();

    }
}

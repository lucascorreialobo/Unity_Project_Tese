using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;
using Random = UnityEngine.Random;

namespace Shader2D_V1
{
    

public class CSharp_FluidShader : MonoBehaviour
{
    public bool debug = false;
    public int zeros = 0;






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

    private float dt = 0.01f;
    private int trueResolutionWidth;  
    private int trueResolutionHeight; 

    private float[] pressure;
    private int[] type;             // 0 - wall  |  1 - fluid
    private float[] velocityV;  
    private float[] velocityH;
    private float[] smoke;

    public RenderTexture fluidTex;
    private Kernels kernels = new Kernels();
    private CBs cBuffers = new CBs();

    private class Kernels {
        public int solveIncompressibility = -1;
        public int extrapolateH = -1;
        public int extrapolateV = -1;
        public int advectVelV = -1;
        public int advectVelH = -1;
        public int extrapolateCorners = -1;
        public int advectSmoke = -1;
        public int fillPressure = -1;
        public int colorTexture = -1;
    }
    private class CBs {  //compute buffers
        public ComputeBuffer pressure;
        public ComputeBuffer type;
        public ComputeBuffer velocityV;
        public ComputeBuffer velocityH;
        public ComputeBuffer newV;
        public ComputeBuffer newH;
        public ComputeBuffer smoke;
        public ComputeBuffer newSmoke;
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
        createComputeBuffers();
        setShaderWideVariables();


        SetupSolveIncompressibilityShader();
        SetupFillPressure(0.0f);
        SetupExtrapolateShaders();
        SetupAdvectVelocityShaders();
        SetupAdvectSmoke();
        SetupColorTexture();

    }

    // Update is called once per frame
    void Update()
    {
        simulate();
    }

    void initializeVariables() {
        //resolutionWidth = 1000;
        //resolutionHeight = 1000;

        pressure = new float[trueResolutionHeight * trueResolutionWidth];
        type = new int[trueResolutionHeight * trueResolutionWidth];
        velocityV = new float[(trueResolutionHeight + 1) * trueResolutionWidth];
        velocityH = new float[trueResolutionHeight * (trueResolutionWidth + 1)];
        smoke = new float[trueResolutionHeight * trueResolutionWidth];

        Array.Fill(type, 0);
        Array.Fill(pressure, 0.0f);
        Array.Fill(velocityV, 0.0f);
        Array.Fill(velocityH, 0.0f);
        Array.Fill(smoke, 0);

        //     for (int i = 0; i < trueResolutionHeight * trueResolutionWidth; i++) {
        //         pressure[i]  = Random.Range(0, 10.0f);
        //         velocityV[i] = Random.Range(0, 10.0f);
        //         velocityH[i] = Random.Range(0, 10.0f);
        //         //type[i]      = Random.Range(0, 2);
        //    }

        //Due to the possibly different sizes assign the last values seperatly
        for( int i = 0; i < trueResolutionHeight; i++) {
            velocityH[i * (trueResolutionWidth + 1) + 0] = 2.0f;
            velocityH[i * (trueResolutionWidth + 1) + 1] = 2.0f;
        }
        //    for (int i = 0; i < trueResolutionWidth; i++) {
        //         velocityV[trueResolutionHeight * trueResolutionWidth + i] = Random.Range(0, 10.0f);
        //    }
        // for (var i = 10; i < 50; i++) {
        //     for (var j = 10; j < 100; j++) {
        //         smoke[i*trueResolutionWidth + j] = 1;

        //     }
        // }

        for (var i = 50; i < 60; i++) {
            for (var j = 60; j < trueResolutionHeight - 60; j++){
                smoke[j * trueResolutionWidth + i] = 1;
            }
        }
        for (var i = 0; i < trueResolutionWidth; i++){
            for (var j = 0; j < trueResolutionHeight; j++){

                int t = 1; //fluid
                if(i == 0 /*|| i == (trueResolutionWidth - 1)*/ ||
                   j == 0 || j == (trueResolutionHeight - 1)){

                    t = 0; //solid
                }
                type[j * trueResolutionWidth + i] = t;

                if (i >= 1 && i < 50)
                {
                    // velocityV[j * (trueResolutionWidth) + i] = 2.0f;
                    // velocityH[j * (trueResolutionWidth + 1) + i] = 2.0f;
                    // velocityH[j * (trueResolutionWidth + 1) + i + 1] = 2.0f;
                    // velocityH[j * (trueResolutionWidth + 1) + i + 2] = 2.0f;
                    // velocityH[j * (trueResolutionWidth + 1) + i + 3] = 2.0f;
                    // velocityH[j * (trueResolutionWidth + 1) + i + 4] = 2.0f;
                }
            }
        }
        // for (int j = 0; j < trueResolutionHeight; j++){
        //     velocityH[j * (trueResolutionWidth + 1) + 1] = 2.0f;
        // }
        // for (var i = 60; i < trueResolutionWidth - 1 - 60; i++){
        //     for (var j = 60; j < trueResolutionHeight - 1 - 60; j++){
        //         pressure[j * trueResolutionWidth + i] = 1;
        //     }
        // }
    }

    private void createComputeBuffers() {
        cBuffers.pressure = new ComputeBuffer(pressure.Length, sizeof(float));
        cBuffers.pressure.SetData(pressure);

        cBuffers.velocityV = new ComputeBuffer(velocityV.Length, sizeof(float));
        cBuffers.velocityV.SetData(velocityV);

        cBuffers.velocityH = new ComputeBuffer(velocityH.Length, sizeof(float));
        cBuffers.velocityH.SetData(velocityH);

        cBuffers.type = new ComputeBuffer(type.Length, sizeof(int));
        cBuffers.type.SetData(type);

        cBuffers.newH = new ComputeBuffer(velocityH.Length, sizeof(float));
        cBuffers.newH.SetData(velocityH);

        cBuffers.newV = new ComputeBuffer(velocityV.Length, sizeof(float));
        cBuffers.newV.SetData(velocityV);

        cBuffers.smoke = new ComputeBuffer(smoke.Length, sizeof(float));
        cBuffers.smoke.SetData(smoke);

        cBuffers.newSmoke = new ComputeBuffer(smoke.Length, sizeof(float));
        cBuffers.newSmoke.SetData(smoke);
    }

    private void setShaderWideVariables() {
        //ints
        fluidShader.SetInt("resolutionWidth", resolutionWidth);
        fluidShader.SetInt("resolutionHeight", resolutionHeight);

        //float
        fluidShader.SetFloat("dt", dt);
    }

    private void SetupSolveIncompressibilityShader() {

        int kernel = fluidShader.FindKernel("solveIncompressibility");
        kernels.solveIncompressibility = kernel;

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


        plane.GetComponent<Renderer>().material.mainTexture = fluidTex;
    }

    private void callSolveIncompressibilityShader(bool getData) {

        float startTime = Time.realtimeSinceStartup;
        for(int i = 0; i < 1; i++)
            fluidShader.Dispatch(kernels.solveIncompressibility, resolutionHeight / 8, resolutionWidth / 8, 1);
        float endTime  = Time.realtimeSinceStartup;

        // Debug.Log("Solve imcompressibility took " + (endTime - startTime) + " seconds to finnish");

        
        if(getData){
            cBuffers.pressure.GetData(pressure);
            cBuffers.type.GetData(type);
            cBuffers.velocityV.GetData(velocityV);
            cBuffers.velocityH.GetData(velocityH);
        }
    }

    private void SetupExtrapolateShaders(){

        int kernel = fluidShader.FindKernel("extrapolateH");
        kernels.extrapolateH = kernel;
        fluidShader.SetBuffer(kernel, "_velocityH", cBuffers.velocityH);
        fluidShader.SetBuffer(kernel, "_velocityV", cBuffers.velocityV);


        kernel = fluidShader.FindKernel("extrapolateV");
        kernels.extrapolateV = kernel;
        fluidShader.SetBuffer(kernel, "_velocityH", cBuffers.velocityH);
        fluidShader.SetBuffer(kernel, "_velocityV", cBuffers.velocityV);


        kernel = fluidShader.FindKernel("extrapolateCorners");
        kernels.extrapolateCorners = kernel;
        fluidShader.SetBuffer(kernel, "_velocityH", cBuffers.velocityH);
        fluidShader.SetBuffer(kernel, "_velocityV", cBuffers.velocityV);
    }
    private void CallExtrapolateShaders() {

        fluidShader.Dispatch(kernels.extrapolateH, resolutionWidth / 64, 1, 1);
        fluidShader.Dispatch(kernels.extrapolateV, resolutionHeight / 64, 1, 1);
        fluidShader.Dispatch(kernels.extrapolateCorners, 1, 1, 1);
    }

    private void SetupAdvectVelocityShaders() {
        //V
        int kernel = fluidShader.FindKernel("advectVelV");
        kernels.advectVelV = kernel;

        fluidShader.SetBuffer(kernel, "_type", cBuffers.type);
        fluidShader.SetBuffer(kernel, "_velocityV", cBuffers.velocityV);
        fluidShader.SetBuffer(kernel, "_velocityH", cBuffers.velocityH);
        fluidShader.SetBuffer(kernel, "_newV", cBuffers.newV);

        //H
        kernel = fluidShader.FindKernel("advectVelH");
        kernels.advectVelH = kernel;

        fluidShader.SetBuffer(kernel, "_type", cBuffers.type);
        fluidShader.SetBuffer(kernel, "_velocityV", cBuffers.velocityV);
        fluidShader.SetBuffer(kernel, "_velocityH", cBuffers.velocityH);
        fluidShader.SetBuffer(kernel, "_newH", cBuffers.newH);

    }

    private void CallAdvectVeocitysShader(){
        fluidShader.Dispatch(kernels.advectVelH, resolutionHeight / 64, resolutionWidth + 1, 1);
        fluidShader.Dispatch(kernels.advectVelV, resolutionHeight + 1, resolutionWidth / 64, 1);

        float[] tempH = new float[trueResolutionHeight * (trueResolutionWidth + 1)];
        cBuffers.newH.GetData(tempH);
        cBuffers.velocityH.SetData(tempH);

        float[] tempV = new float[(trueResolutionHeight + 1) * trueResolutionWidth];
        cBuffers.newV.GetData(tempV);
        cBuffers.velocityV.SetData(tempV);


    }
    
    private void SetupFillPressure(float newPressure){
        int kernel = fluidShader.FindKernel("fillPressure");
        kernels.fillPressure = kernel;

        fluidShader.SetBuffer(kernel, "_pressure", cBuffers.pressure);
        fluidShader.SetFloat("newPressureFill", newPressure);
    }

    private void CallFillPressure(){
        fluidShader.Dispatch(kernels.fillPressure, trueResolutionHeight / 8 + 1, trueResolutionWidth / 8 + 1, 1);
    }

    private void SetupAdvectSmoke(){
        int kernel = fluidShader.FindKernel("advectSmoke");
        kernels.advectSmoke = kernel;

        fluidShader.SetBuffer(kernel, "_type", cBuffers.type);
        fluidShader.SetBuffer(kernel, "_velocityV", cBuffers.velocityV);
        fluidShader.SetBuffer(kernel, "_velocityH", cBuffers.velocityH);
        fluidShader.SetBuffer(kernel, "_smoke", cBuffers.smoke);
        fluidShader.SetBuffer(kernel, "_newSmoke", cBuffers.newSmoke);
    }

    private void CallAdvectSmoke(){
        fluidShader.Dispatch(kernels.advectSmoke, resolutionHeight / 8, resolutionWidth / 8, 1);

        float[] tempSmoke = new float[trueResolutionHeight * trueResolutionWidth];
        cBuffers.newSmoke.GetData(tempSmoke);
        cBuffers.smoke.SetData(tempSmoke);
    }

    private void SetupColorTexture(){
        int kernel = fluidShader.FindKernel("colorTexture");
        kernels.colorTexture = kernel;

        fluidShader.SetBuffer(kernel, "_smoke", cBuffers.smoke);
        fluidShader.SetBuffer(kernel, "_pressure", cBuffers.pressure);
        fluidShader.SetBuffer(kernel, "_velocityH", cBuffers.velocityH);
        fluidShader.SetBuffer(kernel, "_velocityV", cBuffers.velocityV);
        fluidShader.SetBuffer(kernel, "_type", cBuffers.type);
        fluidShader.SetTexture(kernel, "_color", fluidTex);
    }

    private void CallColorTexture(){
        fluidShader.Dispatch(kernels.colorTexture, resolutionHeight / 8, resolutionWidth / 8, 1);
    }

    public void simulate() {
        if (fluidShader == null) return;
 
        // Array.Fill(pressure, 0.0f);
        // CallFillPressure();
        callSolveIncompressibilityShader(getData: false);
        // CallExtrapolateShaders();
        // CallAdvectVeocitysShader();
        // CallAdvectSmoke();
        CallColorTexture();

        if(debug){
            int[] count = new int[trueResolutionHeight * trueResolutionWidth];
            Array.Fill(count, 0);
            for(int x = 0; x < resolutionHeight; x++){
                for(int y = 0; y < resolutionWidth; y++){
                    count[x * (resolutionWidth + 2) + y]++;
                }
            }
            zeros = 0;
            for (int x = 0; x < resolutionHeight; x++){
                for (int y = 0; y < resolutionWidth; y++){
                    if(count[x * (resolutionWidth + 2) + y] >= 2){
                        zeros++;
                        Debug.Log("It happened");
                    }
                    
                }
            }
        }
    }

    void OnDestroy(){
        cBuffers.pressure.Release();
        cBuffers.type.Release();
        cBuffers.velocityV.Release();
        cBuffers.velocityH.Release();
        cBuffers.newV.Release();
        cBuffers.newH.Release();
        cBuffers.smoke.Release();
        cBuffers.newSmoke.Release();
    }









    private enum FIELD
    {
        U_FIELD,
        V_FIELD,
        S_FIELD
    }
    private int getCoordH_Left(int x, int y)
    {
        return x * trueResolutionWidth + y + x; // + x; is to account for the last edge of a row
    }
    private int getCoordH_Right(int x, int y)
    {
        return x * trueResolutionWidth + y + x + 1; // + x; is to account for the last edge of a row
    }
    private int getCoordV_Up(int x, int y)
    {
        return x * trueResolutionWidth + y;
    }
    private int getCoordV_Down(int x, int y)
    {
        return x * trueResolutionWidth + y + 1;
    }

    private void callCSMain()
    {
        if (fluidShader != null)
        {
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

    //get vector (velocityX || velocityY || smoke_density) on point x,y 
    private float sampleField(float x, float y, FIELD fieldType)
    {




        return 0.0f;
    }

    private Vector3 globalPos(float i, float j)
    {
        float x = origin + (i * gridSpacing);
        float y = origin + (j * gridSpacing);

        return new Vector3(x, y, 0);
    }

    void createGrid()
    {
        visualGrid = new GameObject[trueResolutionHeight * trueResolutionWidth];
        origin = trueResolutionWidth * gridSpacing / 2f * -1f;
        square.transform.localScale = new Vector3(gridSpacing, gridSpacing, gridSpacing);

        for (int i = 0; i < trueResolutionHeight; i++)
        {
            for (int j = 0; j < trueResolutionWidth; j++)
            {


                visualGrid[i * trueResolutionWidth + j] = Instantiate(square, globalPos(i, j), Quaternion.identity);
                //visualGrid[i * resolutionWidth + j].GetComponent<Renderer>().material.color = new Color(100, 100, 100);
            }
        }
    }

    private float avgU(int i, int j)
    {
        return 0.0f;
    }

    private float avgV(int i, int j)
    {
        return 0.0f;
    }

    private void advectVel()
    {

        float[] newH = new float[velocityH.Length];
        float[] newV = new float[velocityV.Length];

        Array.Copy(velocityH, newH, velocityH.Length);
        Array.Copy(velocityV, newV, velocityV.Length);
    }
}

}
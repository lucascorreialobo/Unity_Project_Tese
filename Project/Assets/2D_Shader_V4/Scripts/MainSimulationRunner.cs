using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using Utils;

namespace _2D_Shader_V4 {

public class MainSimulationRunner : MonoBehaviour
{
    [SerializeField] private ComputeShader viewRenderer;
    [SerializeField] private ComputeShader addArrowShader;
    [SerializeField] private ComputeShader individualProjectionShader;
    [SerializeField] private ComputeShader individualAdvectionShader;
    [SerializeField] private ComputeShader individualAdvectionPropertyShader;
    [SerializeField] private ComputeShader projectionShader;
    [SerializeField] private ComputeShader advectionVelocitiesShader;
    [SerializeField] private ComputeShader advectionPropertyShader;
    [SerializeField] private ComputeShader clearPressureShader;
    [SerializeField] private ComputeShader copyFloatsBufferShader;
    [SerializeField] private ComputeShader findMinMaxValueShader;
    [SerializeField] private ComputeShader mergeObjectsShader;
    [SerializeField] private ComputeShader calculateForcesShader;


    [SerializeField] private ComputeShader pressureShader;
    [SerializeField] private ComputeShader typeShader;
    [SerializeField] private ComputeShader objectShader;
    [SerializeField] private ComputeShader smokeShader;
    [SerializeField] private ComputeShader velocityShader;
    [SerializeField] private ComputeShader upscalingShader;
    [SerializeField] private ComputeShader addBordersShader;
    private RenderHandler renderHandler;
    [SerializeField] private int2 simResolution = new int2(500, 500);
    private readonly int viewResMult = 100;
    private int2 viewResolution = new int2(0, 0);

    
    [SerializeField] private RenderTexture tex;
    private GameObject plane;

    private Buf2<float> property;
    [SerializeField] private ClickType activeClickType = ClickType.NO_ACTION;
    [SerializeField] private ViewType activeViewType = ViewType.DEBUG_PROPERTY;
    private ViewType previousViewType = ViewType.DEBUG_PROPERTY;
    private SimulationState simState;
    [SerializeField] private bool showArrows = false;
    [SerializeField] private bool showBorders = false;


    private bool waitingForSecondClick = false;
    private int2 firstClickCoord;

    [SerializeField] private bool simulationPlaying = false;
    [SerializeField] private int activeObject = 0;


    private enum ClickType
    {
        NO_ACTION,
        MIRROR_VALUE,  
        REMOVE_COMPRESSIBILITY,
        ADD_VELOCITIES,
        ADD_VELOCITIES_1CLICK,
        ADD_PRESSURE,
        TOGGLE_WALL,    //set wall | set fluid
        PROJECT_SINGLE_CELL,
        CHECK_CELL_VALUES,
        CHECK_MAX_MIN_PRESSURE,
        CHECK_MAX_MIN_PRESSURE_SHADER,
        INDIVIDUAL_ADVECT_VELOCITIES,
        ADVECT_VELOCITIES,
        MULTIPLE_CELL_ADD_VELOCITIES,
        ADD_SMOKE,
        INDIVIDUAL_SMOKE_ADVECTION,
        ADVECT_SMOKE,
        DRAW_BALL,
        MAKE_TUNNEL,
        ADD_OBJECT,
        CALCULTATE_FORCES
    }

    public enum ViewType
    {
        DEBUG_PROPERTY,
        PRESSURE,
        TYPE,
        SMOKE,
        VELOCITY,
        VIEW_OBJECTS
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        plane = this.gameObject;
        viewResolution = simResolution * viewResMult;


        tex = new RenderTexture(viewResolution.x, viewResolution.y, 0)
        {
            enableRandomWrite = true
        };
        tex.Create();

        ResizePlane();

        plane.GetComponent<Renderer>().material.mainTexture = tex;

        property = new Buf2<float>(simResolution.x, simResolution.y);
        CheckboardProperty();

        //Create simulation state
        simState = new SimulationState(simResolution);
        simState.allToGPU();


        renderHandler = new RenderHandler(pressureShader, typeShader, objectShader, smokeShader, velocityShader, upscalingShader, addArrowShader, addBordersShader, simState, tex, viewResolution);

        UpdateNormal();

        //addObject(new int2(0,0));
        }

    // Update is called once per frame
    void Update()
    {
        //P - Play/Pause simulation
        if (Input.GetKeyDown(KeyCode.P)){
            simulationPlaying = !simulationPlaying;
        }

        //N - next frame of simulation
        if (!simulationPlaying && Input.GetKeyDown(KeyCode.N)) {
            double start = Time.realtimeSinceStartupAsDouble;
            UpdateNormal();
            double end = Time.realtimeSinceStartupAsDouble;
            //Debug.Log("This frame took " + (end - start).ToString("E") + " seconds to calculate.");
        }

        if (simulationPlaying){
            double start = Time.realtimeSinceStartupAsDouble;
            UpdateNormal();
            double end = Time.realtimeSinceStartupAsDouble;
            //Debug.Log("This frame took " + (end - start).ToString("E") + " seconds to calculate.");

        }


        mergeObjects();
        //render results
        renderHandler.RenderFinalTexture(showArrows, showBorders, activeViewType);

    }

    void ResizePlane(){
        float simAspectRatio = (float)viewResolution.x / viewResolution.y;
        if(simAspectRatio < 1)
            plane.transform.localScale = new Vector3(simAspectRatio, 1, 1);
        else
            plane.transform.localScale = new Vector3(1, 1, 1 / simAspectRatio);
    }


    //pos should be normalized between 0 and 1
    public void Click(float2 pos, bool repeatClick){
        int2 gridCoord = (int2) (simResolution * pos);

        //Debug.Log("clicked on cell [" + gridCoord.x + "," + gridCoord.y + "]");


        switch (activeClickType)
        {
            case ClickType.MIRROR_VALUE:
                MirrorValueClick(gridCoord);
                break;
            case ClickType.REMOVE_COMPRESSIBILITY:
                if(!repeatClick)
                    RemoveCompressibilityClick();
                break;
            case ClickType.ADD_VELOCITIES:
                if(!repeatClick)
                    AddVelocitiesClick(gridCoord);
                break;
            case ClickType.ADD_VELOCITIES_1CLICK:
                AddVelocities1Click(gridCoord);
                break;
            case ClickType.ADD_PRESSURE:
                AddPressureClick(gridCoord);
                break;
            case ClickType.TOGGLE_WALL:
                if (!repeatClick)
                    ToggleWalClick(gridCoord);
                break;
            case ClickType.PROJECT_SINGLE_CELL:
                if (!repeatClick)
                    ProjectSingleCellClick(gridCoord);
                break;
            case ClickType.CHECK_CELL_VALUES:
                if (!repeatClick)
                    CheckCellValuesClick(gridCoord);
                break;
            case ClickType.CHECK_MAX_MIN_PRESSURE:
                if (!repeatClick) {
                    double start = Time.realtimeSinceStartupAsDouble;
                    CheckMaxMinClick();
                    double end = Time.realtimeSinceStartupAsDouble;
                    //Debug.Log("This minMax took " + (end - start).ToString("E") + " seconds to calculate.");
                }
                break;
            case ClickType.CHECK_MAX_MIN_PRESSURE_SHADER:
                if (!repeatClick) {
                    double start = Time.realtimeSinceStartupAsDouble;
                    CheckMaxMinShaderClick();
                    double end = Time.realtimeSinceStartupAsDouble;
                    //Debug.Log("This minMax took " + (end - start).ToString("E") + " seconds to calculate.");
                }
                break;
            case ClickType.INDIVIDUAL_ADVECT_VELOCITIES:
                if (!repeatClick)
                    IndividualCellAdvectVelocitiesClick(gridCoord);
                break;
            case ClickType.ADVECT_VELOCITIES:
                AdvectVelocitiesClick();
                break;
            case ClickType.MULTIPLE_CELL_ADD_VELOCITIES:
                multipleCellAddVelocitiesClick(gridCoord);
                break;
            case ClickType.ADD_SMOKE:
                addSmokeClick(gridCoord);
                break;
            case ClickType.INDIVIDUAL_SMOKE_ADVECTION:
                individualadvectsmokeClick(gridCoord);
                break;
            case ClickType.ADVECT_SMOKE:
                advectsmokeClick();
                break;
            case ClickType.DRAW_BALL:
                if(!repeatClick)
                    drawBallClick(gridCoord);
                break;
            case ClickType.MAKE_TUNNEL:
                if(!repeatClick)
                    makeTunnelClick();
                break;
            case ClickType.ADD_OBJECT:
                if(!repeatClick)
                    addObjectClick(gridCoord);
                break;
            case ClickType.CALCULTATE_FORCES:
                if(!repeatClick)
                    calcultateForcesClick();
                break;
            default:
                break;
        }
    }

    private void UpdateNormal(){
        int kernel = viewRenderer.FindKernel("CSMain");

        if(previousViewType != activeViewType){
            previousViewType = activeViewType;
            renderHandler.ChangeActivePropertyShader(activeViewType);
        }
        //set all pressures to 0

        //projection  | Is it worth to measure divergence every projection to end early?
        RemoveCompressibilityClick();

        //extrapolation??

        //advection
        AdvectVelocitiesClick();

        advectsmokeClick();

        
    }

    void FillProperty(){
        
        for (int x = 0; x < simResolution.x; x++)
        {
            for (int y = 0; y < simResolution.y; y++)
            {
                property[x, y] = 1;
                 //property[x,y] = x < 20 ? 0 : 1;
                }
        }
        property.ToGPU();

    }

    void CheckboardProperty(){
        
        for (int x = 0; x < simResolution.x; x++)
        {
            for (int y = 0; y < simResolution.y; y++)
            {
                property[x, y] = (x + y) % 2;
                // property[x,y] = x < 20 ? 0 : 1;
            }
        }
        property.ToGPU();
    }

    void OnDestroy(){
        property.Release();
        simState.Destroy();
        tex.Release();
    }

    void ChangeViewRes(int newSimResMult){
        if(viewResolution.Equals(simResolution * newSimResMult))
            return;
        viewResolution = simResolution * newSimResMult;
        tex = new RenderTexture(viewResolution.x, viewResolution.y, 0)
        {
            enableRandomWrite = true
        };
        tex.Create();
        plane.GetComponent<Renderer>().material.mainTexture = tex;
        int kernel = viewRenderer.FindKernel("CSMain");
        viewRenderer.SetTexture(kernel, "Result", tex);
        viewRenderer.SetInts("_viewRes", new int[]{viewResolution.x, viewResolution.y});
    }

    private void MirrorValueClick(int2 gridCoord){
        property[gridCoord.x, gridCoord.y] = 1 - property[gridCoord.x, gridCoord.y];
        property.ToGPU();
        //ChangeViewRes(10);
    }

    private void RemoveCompressibilityClick() {

        clearPressureShader.SetBuffer(0, "_pressure", simState.pressure.GetComputeBuffer());
        clearPressureShader.Dispatch(0, (simState.simRes.x * simState.simRes.y / 64) + 1, 1, 1);
        

        int kernel = projectionShader.FindKernel("CSMain");
        projectionShader.SetBuffer(kernel, "_pressure", simState.pressure.GetComputeBuffer());
        projectionShader.SetBuffer(kernel, "_type", simState.objectsMerged.GetComputeBuffer());
        projectionShader.SetBuffer(kernel, "_velocityV", simState.velocityV.GetComputeBuffer());
        projectionShader.SetBuffer(kernel, "_velocityH", simState.velocityH.GetComputeBuffer());
        projectionShader.SetInts("_simRes", new int[] { simResolution.x, simResolution.y });

        int threadGroupX = ((simState.simRes.x / 2) / 8) + 1;
        int threadGroupY = ((simState.simRes.y / 2) / 8) + 1;

        int i = 0;
        for(i = 0; i < 100; i++){
            
            projectionShader.SetInts("_offset", new int[] { 0, 0 });
            projectionShader.Dispatch(kernel, threadGroupX, threadGroupY, 1);

            projectionShader.SetInts("_offset", new int[] { 0, 1 });
            projectionShader.Dispatch(kernel, threadGroupX, threadGroupY, 1);

            projectionShader.SetInts("_offset", new int[] { 1, 0 });
            projectionShader.Dispatch(kernel, threadGroupX, threadGroupY, 1);

            projectionShader.SetInts("_offset", new int[] { 1, 1 });
            projectionShader.Dispatch(kernel, threadGroupX, threadGroupY, 1);

            //float maxDivergence = 0;
            //simState.velocityV.FromGPU();
            //simState.velocityH.FromGPU();

            //for(int x = 1; x < simState.simRes.x - 1; x++) {
            //    for(int y = 1; y < simState.simRes.y - 1; y++) {
            //        float down = simState.velocityV[x, y]; float up = simState.velocityV[x, y + 1];
            //        float left = simState.velocityH[x, y]; float right = simState.velocityH[x + 1, y];
            //        float cellDivergence = -down + up - left + right;

            //        if(cellDivergence > maxDivergence)
            //            maxDivergence = cellDivergence;
            //    }
            //}
            //Debug.Log("Max divergence on iteration " + i + ": " + maxDivergence);
            //if(maxDivergence <= 0.001 && false)
            //    break;
        }
        //Debug.Log("number of iteration needed for compressibility: " + i);
    }

    private void AddVelocitiesClick(int2 gridCoord) {
        if (waitingForSecondClick) {
            simState.velocityV.FromGPU();
            simState.velocityH.FromGPU();
            int2 distance = gridCoord - firstClickCoord;
            //Debug.Log("Distance = " + distance);
            if (math.length(distance) == 1) {
                float addedVelocity = 0.3f;
                if (distance.x == 0) {
                    int direction = distance.y;
                    int2 velocityIndex = direction > 0 ? gridCoord : firstClickCoord;
                    simState.velocityV[velocityIndex.x, velocityIndex.y] += addedVelocity * direction;

                } else if (distance.y == 0) {
                    int direction = distance.x;
                    int2 velocityIndex = direction > 0 ? gridCoord : firstClickCoord;
                    simState.velocityH[velocityIndex.x, velocityIndex.y] += addedVelocity * direction;
                }
                // int direction = distance.x == 1 || distance.y == 1 ? 1 : -1;
                // simState.velocityV[gridCoord.x, gridCoord.y] += addedVelocity * direction;
                simState.velocityV.ToGPU();
                simState.velocityH.ToGPU();
            }
            waitingForSecondClick = false;
        } else {
            firstClickCoord = gridCoord;
            waitingForSecondClick = true;
            Debug.Log("First click, now waiting on second click");
        }
    }

    private void AddVelocities1Click(int2 gridCoord) {
        simState.velocityV.FromGPU();
        simState.velocityH.FromGPU();

        
        int2 distance = new int2(1, 0);
        float addedVelocity = 0.9f;

        if (distance.x == 0) {
            int direction = distance.y;
            int2 velocityIndex = direction > 0 ? gridCoord : firstClickCoord;
            simState.velocityV[velocityIndex.x, velocityIndex.y] = addedVelocity * direction;

        } else if (distance.y == 0) {
            int direction = distance.x;
            int2 velocityIndex = direction > 0 ? gridCoord : firstClickCoord;
            simState.velocityH[velocityIndex.x, velocityIndex.y] = addedVelocity * direction;
        }

        simState.velocityV.ToGPU();
        simState.velocityH.ToGPU();
    }

    private void AddPressureClick(int2 gridCoord) {
        float addedPressure = 0.2f;
        property[gridCoord.x, gridCoord.y] = property[gridCoord.x, gridCoord.y] + addedPressure;
        property.ToGPU();

        simState.pressure[gridCoord.x, gridCoord.y] = addedPressure;
        simState.pressure.ToGPU();
    }

    private void ToggleWalClick(int2 gridCoord) {
        simState.wall.FromGPU();
        simState.wall[gridCoord.x, gridCoord.y] = (simState.wall[gridCoord.x, gridCoord.y] - 1) * -1; //toggle between 0 and 1
        simState.wall.ToGPU();
    }

    private void ProjectSingleCellClick(int2 gridCoord) {
        int kernel = individualProjectionShader.FindKernel("CSMain");
        individualProjectionShader.SetBuffer(kernel, "_pressure", simState.pressure.GetComputeBuffer());
        individualProjectionShader.SetBuffer(kernel, "_type", simState.objectsMerged.GetComputeBuffer());
        individualProjectionShader.SetBuffer(kernel, "_velocityV", simState.velocityV.GetComputeBuffer());
        individualProjectionShader.SetBuffer(kernel, "_velocityH", simState.velocityH.GetComputeBuffer());
        individualProjectionShader.SetInts("_simRes", new int[] { simResolution.x, simResolution.y });
        individualProjectionShader.SetInts("_cell", new int[] { gridCoord.x, gridCoord.y });
        individualProjectionShader.Dispatch(kernel, 1, 1, 1);

    }

    private void CheckCellValuesClick(int2 gridCoord) {
        simState.allFromGPU();
        float down = simState.velocityV[gridCoord.x, gridCoord.y]; float up = simState.velocityV[gridCoord.x, gridCoord.y + 1];
        float left = simState.velocityH[gridCoord.x, gridCoord.y]; float right = simState.velocityH[gridCoord.x + 1, gridCoord.y];
        float totalDivergence = -down + up - left + right;

        Debug.Log("Clicked cell: (" + gridCoord.x + "," + gridCoord.y + ")"
        + "\nPressure: " + simState.pressure[gridCoord.x, gridCoord.y].ToString("F2") + " Pa (Pascal)"
        + "\nType: " + simState.objectsMerged[gridCoord.x, gridCoord.y] + " (0 - Wall/Object | 1 - Fluid)"
        + "\nSmoke: " + simState.smoke[gridCoord.x, gridCoord.y]
        + "\nTotal divergence: " + totalDivergence.ToString("F3") + ". (x-1: " + -left + " | y-1: " + -down + " | x+1: " + right + " | y+1: " + up + ")");
    }

    private void CheckMaxMinClick() {
        simState.pressure.FromGPU();
        Buf2<float> pressure = simState.pressure;

        float max = pressure[0,0];
        float min = pressure[0,0];

        for(int x = 0; x < simState.simRes.x; x++){
            for(int y = 0; y < simState.simRes.y; y++) {
                if(pressure[x,y] > max)
                    max = pressure[x,y];
                if(pressure[x,y] < min)
                    min = pressure[x,y];
            }
        }

        Debug.Log("pressure max: " + max + "\npressure min: " + min);
    }

    private void CheckMaxMinShaderClick() {
        int threadSize = 1024;
        int numberOfGroups = (simState.simRes.x * simState.simRes.y / threadSize) + 1;
        Buf2<float> resultBuffer = new Buf2<float>(numberOfGroups * 2, 1);
        Buf2<float> resultBuffer2 = new Buf2<float>(numberOfGroups * 2, 1);

        int kernel = findMinMaxValueShader.FindKernel("CSMain");
        findMinMaxValueShader.SetBuffer(kernel, "_property", simState.pressure.GetComputeBuffer());
        findMinMaxValueShader.SetBuffer(kernel, "_resultBuffer", resultBuffer.GetComputeBuffer());
        findMinMaxValueShader.SetInt("_length", simState.simRes.x * simState.simRes.y);

        findMinMaxValueShader.Dispatch(kernel, numberOfGroups, 1, 1);

        kernel = findMinMaxValueShader.FindKernel("nPlusIterations");
        

        while(numberOfGroups > 1) {
            resultBuffer.FromGPU();

            //float mn = resultBuffer[0, 0];
            //float mx = resultBuffer[1, 0];

            //for(int i = 1; i < numberOfGroups; i++) {
            //    mn = math.min(mn, resultBuffer[i * 2, 0]);
            //    mx = math.max(mx, resultBuffer[i * 2 + 1, 0]);
            //}
            //Debug.Log("The max value is " + mx + " and the min is " + mn);


            findMinMaxValueShader.SetBuffer(kernel, "_property", resultBuffer.GetComputeBuffer());
            findMinMaxValueShader.SetBuffer(kernel, "_resultBuffer2", resultBuffer2.GetComputeBuffer());
            findMinMaxValueShader.SetInt("_length", numberOfGroups);
            numberOfGroups = (numberOfGroups / threadSize) + 1;

            findMinMaxValueShader.Dispatch(kernel, numberOfGroups, 1, 1);

            //Debug.Log("Did a loop ");
            Buf2<float> temp = resultBuffer;
            resultBuffer = resultBuffer2;
            resultBuffer2 = temp;

        }


        //resultBuffer.FromGPU();
        
        float min = resultBuffer[0, 0];
        float max = resultBuffer[1, 0];


        Debug.Log("The max value is " + max + " and the min is " + min);

        resultBuffer.Release();
        resultBuffer2.Release();
    }

    private void IndividualCellAdvectVelocitiesClick(int2 gridCoord) {
        Buf2<float> newVelocityV = new Buf2<float>(simState.simRes.x, simState.simRes.y);
        Buf2<float> newVelocityH = new Buf2<float>(simState.simRes.x, simState.simRes.y);

        copyFloatsBufferShader.SetBuffer(0, "_srcBuffer", simState.velocityV.GetComputeBuffer());
        copyFloatsBufferShader.SetBuffer(0, "_destBuffer", newVelocityV.GetComputeBuffer());
        copyFloatsBufferShader.Dispatch(0, (simState.simRes.x * simState.simRes.y / 64) + 1, 1, 1);

        copyFloatsBufferShader.SetBuffer(0, "_srcBuffer", simState.velocityH.GetComputeBuffer());
        copyFloatsBufferShader.SetBuffer(0, "_destBuffer", newVelocityH.GetComputeBuffer());
        copyFloatsBufferShader.Dispatch(0, (simState.simRes.x * simState.simRes.y / 64) + 1, 1, 1);

        int kernel = individualAdvectionShader.FindKernel("CSMain");
        individualAdvectionShader.SetBuffer(kernel, "_type", simState.objectsMerged.GetComputeBuffer());
        individualAdvectionShader.SetBuffer(kernel, "_velocityV", simState.velocityV.GetComputeBuffer());
        individualAdvectionShader.SetBuffer(kernel, "_velocityH", simState.velocityH.GetComputeBuffer());
        individualAdvectionShader.SetBuffer(kernel, "_newVelocityV", newVelocityV.GetComputeBuffer());
        individualAdvectionShader.SetBuffer(kernel, "_newVelocityH", newVelocityH.GetComputeBuffer());
        individualAdvectionShader.SetInts("_simRes", new int[] { simState.simRes.x, simState.simRes.y });
        individualAdvectionShader.SetFloat("_timeStep", 1);
        individualAdvectionShader.SetInts("cell", new int[] {gridCoord.x, gridCoord.y});

        individualAdvectionShader.Dispatch(kernel, 1, 1, 1);

        copyFloatsBufferShader.SetBuffer(0, "_srcBuffer", newVelocityV.GetComputeBuffer());
        copyFloatsBufferShader.SetBuffer(0, "_destBuffer", simState.velocityV.GetComputeBuffer());
        copyFloatsBufferShader.Dispatch(0, (simState.simRes.x * simState.simRes.y / 64) + 1, 1, 1);

        copyFloatsBufferShader.SetBuffer(0, "_srcBuffer", newVelocityH.GetComputeBuffer());
        copyFloatsBufferShader.SetBuffer(0, "_destBuffer", simState.velocityH.GetComputeBuffer() );
        copyFloatsBufferShader.Dispatch(0, (simState.simRes.x * simState.simRes.y / 64) + 1, 1, 1);

        newVelocityV.FromGPU();
        newVelocityH.FromGPU();
        float calculatedV = newVelocityV[gridCoord.x, gridCoord.y];
        float calculatedH = newVelocityH[gridCoord.x, gridCoord.y];
        Debug.Log("The new value for velocityV is: " + calculatedV + "\nThe new value for velocityH is: " + calculatedH);

        newVelocityV.Release();
        newVelocityH.Release();
    }

    private void AdvectVelocitiesClick() {
        Buf2<float> newVelocityV = new Buf2<float>(simState.simRes.x, simState.simRes.y);
        Buf2<float> newVelocityH = new Buf2<float>(simState.simRes.x, simState.simRes.y);

        copyFloatsBufferShader.SetBuffer(0, "_srcBuffer", simState.velocityV.GetComputeBuffer());
        copyFloatsBufferShader.SetBuffer(0, "_destBuffer", newVelocityV.GetComputeBuffer());
        copyFloatsBufferShader.Dispatch(0, (simState.simRes.x * simState.simRes.y / 64) + 1, 1, 1);

        copyFloatsBufferShader.SetBuffer(0, "_srcBuffer", simState.velocityH.GetComputeBuffer());
        copyFloatsBufferShader.SetBuffer(0, "_destBuffer", newVelocityH.GetComputeBuffer());
        copyFloatsBufferShader.Dispatch(0, (simState.simRes.x * simState.simRes.y / 64) + 1, 1, 1);

        int kernel = advectionVelocitiesShader.FindKernel("CSMain");
        advectionVelocitiesShader.SetBuffer(kernel, "_type", simState.objectsMerged.GetComputeBuffer());
        advectionVelocitiesShader.SetBuffer(kernel, "_velocityV", simState.velocityV.GetComputeBuffer());
        advectionVelocitiesShader.SetBuffer(kernel, "_velocityH", simState.velocityH.GetComputeBuffer());
        advectionVelocitiesShader.SetBuffer(kernel, "_newVelocityV", newVelocityV.GetComputeBuffer());
        advectionVelocitiesShader.SetBuffer(kernel, "_newVelocityH", newVelocityH.GetComputeBuffer());
        advectionVelocitiesShader.SetInts("_simRes", new int[] { simState.simRes.x, simState.simRes.y });
        advectionVelocitiesShader.SetFloat("_timeStep", 1);
        //advectionShader.SetInts("cell", new int[] { gridCoord.x, gridCoord.y });
        
        advectionVelocitiesShader.Dispatch(kernel, (simState.simRes.x / 8) + 1, (simState.simRes.y / 8) + 1, 1);


        copyFloatsBufferShader.SetBuffer(0, "_srcBuffer", newVelocityV.GetComputeBuffer());
        copyFloatsBufferShader.SetBuffer(0, "_destBuffer", simState.velocityV.GetComputeBuffer());
        copyFloatsBufferShader.Dispatch(0, (simState.simRes.x * simState.simRes.y / 64) + 1, 1, 1);

        copyFloatsBufferShader.SetBuffer(0, "_srcBuffer", newVelocityH.GetComputeBuffer());
        copyFloatsBufferShader.SetBuffer(0, "_destBuffer", simState.velocityH.GetComputeBuffer());
        copyFloatsBufferShader.Dispatch(0, (simState.simRes.x * simState.simRes.y / 64) + 1, 1, 1);

        newVelocityV.Release();
        newVelocityH.Release();
    }

    private void multipleCellAddVelocitiesClick(int2 gridCoord) {
        if(waitingForSecondClick){
            waitingForSecondClick = false;
            int radius = 4;
            int2 distance = gridCoord - firstClickCoord;

            float addedVelocity = 0.3f;
            bool horizontalDirection = math.abs(distance.x) > math.abs(distance.y); 
            addedVelocity *= horizontalDirection ? distance.x/math.abs(distance.x) : distance.y / math.abs(distance.y);

            Buf2<float> velocity = horizontalDirection ? simState.velocityH : simState.velocityV;

            
            simState.velocityV.FromGPU();
            simState.velocityH.FromGPU();

            for(int x = -radius; x < radius; x++){
                for(int y = -radius; y < radius; y++) {
                    velocity[firstClickCoord.x + x, firstClickCoord.y + y] += addedVelocity;
                }
            }
            simState.velocityV.ToGPU();
            simState.velocityH.ToGPU();

        } else {
            firstClickCoord = gridCoord;
            waitingForSecondClick = true;
        }


        return;
    }

    private void addSmokeClick(int2 gridCoord) {
        simState.smoke.FromGPU();
        simState.smoke[gridCoord.x, gridCoord.y] += 1f;
        simState.smoke.ToGPU();
        return;
    }

    private void individualadvectsmokeClick(int2 gridCoord) {
        Buf2<float> newSmoke = new Buf2<float>(simState.simRes.x, simState.simRes.y);

        copyFloatsBufferShader.SetBuffer(0, "_srcBuffer", simState.smoke.GetComputeBuffer());
        copyFloatsBufferShader.SetBuffer(0, "_destBuffer", newSmoke.GetComputeBuffer());
        copyFloatsBufferShader.Dispatch(0, (simState.simRes.x * simState.simRes.y / 64) + 1, 1, 1);


        int kernel = advectionPropertyShader.FindKernel("CSMain");
        advectionPropertyShader.SetBuffer(kernel, "_type", simState.objectsMerged.GetComputeBuffer());
        advectionPropertyShader.SetBuffer(kernel, "_velocityV", simState.velocityV.GetComputeBuffer());
        advectionPropertyShader.SetBuffer(kernel, "_velocityH", simState.velocityH.GetComputeBuffer());
        advectionPropertyShader.SetBuffer(kernel, "_advectedProperty", simState.smoke.GetComputeBuffer());
        advectionPropertyShader.SetBuffer(kernel, "_newAdvectedProperty", newSmoke.GetComputeBuffer());
        advectionPropertyShader.SetInts("_simRes", new int[] { simState.simRes.x, simState.simRes.y });
        advectionPropertyShader.SetFloat("_timeStep", 1);
        advectionPropertyShader.SetInts("cell", new int[] { gridCoord.x, gridCoord.y });
        
        advectionPropertyShader.Dispatch(kernel, (simState.simRes.x / 8) + 1, (simState.simRes.y / 8) + 1, 1);

        copyFloatsBufferShader.SetBuffer(0, "_srcBuffer", newSmoke.GetComputeBuffer());
        copyFloatsBufferShader.SetBuffer(0, "_destBuffer", simState.smoke.GetComputeBuffer());
        copyFloatsBufferShader.Dispatch(0, (simState.simRes.x * simState.simRes.y / 64) + 1, 1, 1);

        Debug.Log("advectSmoke performed");

        newSmoke.Release();
    }

    private void advectsmokeClick() {
        Buf2<float> newSmoke = new Buf2<float>(simState.simRes.x, simState.simRes.y);

        copyFloatsBufferShader.SetBuffer(0, "_srcBuffer", simState.smoke.GetComputeBuffer());
        copyFloatsBufferShader.SetBuffer(0, "_destBuffer", newSmoke.GetComputeBuffer());
        copyFloatsBufferShader.Dispatch(0, (simState.simRes.x * simState.simRes.y / 64) + 1, 1, 1);


        int kernel = advectionPropertyShader.FindKernel("CSMain");
        advectionPropertyShader.SetBuffer(kernel, "_type", simState.objectsMerged.GetComputeBuffer());
        advectionPropertyShader.SetBuffer(kernel, "_velocityV", simState.velocityV.GetComputeBuffer());
        advectionPropertyShader.SetBuffer(kernel, "_velocityH", simState.velocityH.GetComputeBuffer());
        advectionPropertyShader.SetBuffer(kernel, "_advectedProperty", simState.smoke.GetComputeBuffer());
        advectionPropertyShader.SetBuffer(kernel, "_newAdvectedProperty", newSmoke.GetComputeBuffer());
        advectionPropertyShader.SetInts("_simRes", new int[] { simState.simRes.x, simState.simRes.y });
        advectionPropertyShader.SetFloat("_timeStep", 1);
        //advectionShader.SetInts("cell", new int[] { gridCoord.x, gridCoord.y });
        
        advectionPropertyShader.Dispatch(kernel, (simState.simRes.x / 8) + 1, (simState.simRes.y / 8) + 1, 1);

        copyFloatsBufferShader.SetBuffer(0, "_srcBuffer", newSmoke.GetComputeBuffer());
        copyFloatsBufferShader.SetBuffer(0, "_destBuffer", simState.smoke.GetComputeBuffer());
        copyFloatsBufferShader.Dispatch(0, (simState.simRes.x * simState.simRes.y / 64) + 1, 1, 1);


        newSmoke.Release();
    }

    private void drawBallClick(int2 gridCoord) {
        simState.objectsSeperated.FromGPU();
        simState.velocityV.FromGPU();
        simState.velocityH.FromGPU();

        int radius = 10;
        int2 center = gridCoord;

        for(int x = -radius; x < radius; x++) {
            for(int y = -radius; y < radius; y++) {
                float distanceToCenter = math.square(x) + math.square(y);
                //Debug.Log("relative point (" + x + "," + y + ") distance to center (" + center.x + "," + center.y + ") is " + distanceToCenter);
                if(distanceToCenter < math.square(radius))
                    simState.addObjectNoGPU(activeObject, gridCoord + new int2(x,y));
                    //simState.wall[gridCoord.x + x, gridCoord.y + y] = 0;
            }
        }

        simState.objectsSeperated.ToGPU();
        simState.velocityV.ToGPU();
        simState.velocityH.ToGPU();
    }

    private void makeTunnelClick() {
        simState.velocityH.FromGPU();

        for(int y = 1; y < simState.simRes.y - 1; y++){
            simState.velocityH[1, y] = 0.6f;
            simState.velocityH[simState.simRes.x - 1, y] = 0.6f;
        }


        simState.velocityH.ToGPU();
    }

    private void addObjectClick(int2 gridCoord) {

        simState.addObject(activeObject, gridCoord);
    }

    private void calcultateForcesClick() {
        int threadSize = 1024;
        int numberOfGroups = (simState.simRes.x * simState.simRes.y / threadSize) + 1;
        Buf2<float2> resultForces = new Buf2<float2>(numberOfGroups * threadSize, 1);
        Buf2<float2> resultForcesSummed = new Buf2<float2>(numberOfGroups, 1);
        int kernel = calculateForcesShader.FindKernel("CSMain");

        calculateForcesShader.SetBuffer(kernel, "_objects", simState.objectsSeperated.GetComputeBuffer());
        calculateForcesShader.SetBuffer(kernel, "_pressure", simState.pressure.GetComputeBuffer());
        calculateForcesShader.SetBuffer(kernel, "_resultForce", resultForces.GetComputeBuffer());
        calculateForcesShader.SetInt("_objectIndex", activeObject);
        calculateForcesShader.SetInts("_simRes", new int[] { simState.simRes.x, simState.simRes.y });

        calculateForcesShader.Dispatch(kernel, numberOfGroups, 1, 1);

        kernel = calculateForcesShader.FindKernel("SumAll");

        while(numberOfGroups > 1){
            calculateForcesShader.SetBuffer(kernel, "_resultForce", resultForces.GetComputeBuffer());
            calculateForcesShader.SetBuffer(kernel, "_resultForceSummed", resultForcesSummed.GetComputeBuffer());
            calculateForcesShader.SetInt("_length", numberOfGroups);

            numberOfGroups = (numberOfGroups / threadSize) + 1;
            calculateForcesShader.Dispatch(kernel, numberOfGroups, 1, 1);

            Buf2<float2> temp = resultForces;
            resultForces = resultForcesSummed;
            resultForcesSummed = temp;
        }

        
        resultForces.FromGPU();

        float2 totalForce = resultForces[0,0];

        Debug.Log($"The force applied on object {activeObject} has vector ({totalForce.x:F2}; {totalForce.y:F2})N");

        resultForces.Release();
        resultForcesSummed.Release();

    }

    private void mergeObjects() {
        //Buf2<float> merged = new Buf2<float>(simState.simRes.x, simState.simRes.y);
        int kernel = mergeObjectsShader.FindKernel("CSMain");
        mergeObjectsShader.SetBuffer(kernel, "_objects", simState.objectsSeperated.GetComputeBuffer());
        mergeObjectsShader.SetBuffer(kernel, "_walls", simState.wall.GetComputeBuffer());
        mergeObjectsShader.SetBuffer(kernel, "_merged", simState.objectsMerged.GetComputeBuffer());
        mergeObjectsShader.SetInt("_numberOfObjects", simState.numberOfObjects);

        mergeObjectsShader.Dispatch(kernel, (simState.simRes.x * simState.simRes.y / 64) + 1, 1, 1);
    }

    private void addObject(int2 gridCoord) {
        int objectIndex = 0;

        simState.objectsMerged.FromGPU();
        simState.objectsMerged[0,0] = 0;
        simState.objectsMerged.ToGPU();
    }

}
}
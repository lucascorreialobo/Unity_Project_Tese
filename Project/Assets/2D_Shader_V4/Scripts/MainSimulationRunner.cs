using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using Utils;

namespace _2D_Shader_V4 {

public class MainSimulationRunner : MonoBehaviour
{
    [SerializeField] private ComputeShader viewRenderer;
    [SerializeField] private ComputeShader addArrowShader;
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

    private bool waitingForSecondClick = false;
    private int2 firstClickCoord;

    private enum ClickType
    {
        NO_ACTION,
        MIRROR_VALUE,
        REMOVE_COMPRESSIBILITY,
        ADVECT_VELOCITIES,
        ADD_PRESSURE
    }

    private enum ViewType
    {
        DEBUG_PROPERTY,
        PRESSURE,
        TYPE,
        SMOKE
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

        plane.GetComponent<Renderer>().material.mainTexture = tex;

        property = new Buf2<float>(simResolution.x, simResolution.y);
        CheckboardProperty();

        //Create simulation state
        simState = new SimulationState(simResolution);
        simState.allToGPU();

        //run shader for first time
        int kernel = viewRenderer.FindKernel("CSMain");
        viewRenderer.SetTexture(kernel, "Result", tex);
        viewRenderer.SetBuffer(kernel, "_property", property.GetComputeBuffer());
        viewRenderer.SetInts("_viewRes", new int[]{viewResolution.x, viewResolution.y});
        viewRenderer.SetInts("_simRes", new int[]{simResolution.x, simResolution.y});
        
        viewRenderer.Dispatch(kernel, (viewResolution.x / 8) + 1, (viewResolution.y / 8) + 1, 1);


        kernel = addArrowShader.FindKernel("CSMain");
        addArrowShader.SetTexture(kernel, "Result", tex);
        addArrowShader.SetBuffer(kernel, "_verticalProperty", simState.velocityV.GetComputeBuffer());
        addArrowShader.SetBuffer(kernel, "_horizontalProperty", simState.velocityH.GetComputeBuffer());
        addArrowShader.SetInts("_simRes", new int[]{simResolution.x, simResolution.y});
    }

    // Update is called once per frame
    void Update()
    {
        UpdateNormal();
    }

    //pos should be normalized between 0 and 1
    public void Click(float2 pos){
        int2 gridCoord = (int2) (simResolution * pos);

        Debug.Log("clicked on cell [" + gridCoord.x + "," + gridCoord.y + "]");


        switch (activeClickType)
        {
            case ClickType.MIRROR_VALUE:
                property[gridCoord.x, gridCoord.y] = 1 - property[gridCoord.x, gridCoord.y];
                property.ToGPU();
                ChangeViewRes(10);
                break;
            case ClickType.REMOVE_COMPRESSIBILITY:
                break;
            case ClickType.ADVECT_VELOCITIES:
                if(waitingForSecondClick){
                    int2 distance = gridCoord - firstClickCoord;
                    Debug.Log("Distance = " + distance);
                    if(math.length(distance) == 1){
                        float addedVelocity = 0.1f;
                        if(distance.x == 0){
                            int direction = distance.y;
                            simState.velocityV[gridCoord.x, gridCoord.y] += addedVelocity * direction;
                            
                        }
                        // int direction = distance.x == 1 || distance.y == 1 ? 1 : -1;
                        // simState.velocityV[gridCoord.x, gridCoord.y] += addedVelocity * direction;
                        simState.velocityV.ToGPU();
                    }
                    waitingForSecondClick = false;
                } else {
                    waitingForSecondClick = true;
                    firstClickCoord = gridCoord;
                    Debug.Log("First click, now waiting on second click");
                }
                

                break;
            case ClickType.ADD_PRESSURE:
                float addedPressure = 0.2f;
                property[gridCoord.x, gridCoord.y] = property[gridCoord.x, gridCoord.y] + addedPressure;
                property.ToGPU();

                simState.pressure[gridCoord.x, gridCoord.y] = addedPressure;
                simState.pressure.ToGPU();
                break;
            default:
                break;
        }
    }

    private void UpdateNormal(){
        int kernel = viewRenderer.FindKernel("CSMain");

        if(previousViewType != activeViewType){
            ComputeBuffer newCB;

            switch (activeViewType)
            {
                case ViewType.DEBUG_PROPERTY: newCB = property.GetComputeBuffer(); break;
                case ViewType.PRESSURE: newCB = simState.pressure.GetComputeBuffer(); break;
                case ViewType.SMOKE: newCB = simState.smoke.GetComputeBuffer(); break;
                case ViewType.TYPE: newCB = simState.type.GetComputeBuffer(); break;
                default: Debug.Log("No valid active View Type."); newCB = property.GetComputeBuffer();break;
            }
            previousViewType = activeViewType;
            viewRenderer.SetBuffer(kernel, "_property", newCB);

        }

        
        viewRenderer.Dispatch(kernel, (viewResolution.x / 8) + 1, (viewResolution.y / 8) + 1, 1);
        addArrowShader.Dispatch(kernel, (simResolution.x / 8) + 1, (simResolution.y / 8) + 1, 1);
    }

    void FillProperty(){
        
        for (int x = 0; x < simResolution.x; x++)
        {
            for (int y = 0; y < simResolution.y; y++)
            {
                property[x, y] = 1;
                // property[x,y] = x < 20 ? 0 : 1;
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
}
}
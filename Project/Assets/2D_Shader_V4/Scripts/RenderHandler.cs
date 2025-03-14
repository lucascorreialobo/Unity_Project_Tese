using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEditor.Search;
using UnityEngine;
using Utils;
using static _2D_Shader_V4.MainSimulationRunner;


namespace _2D_Shader_V4 {

    public class RenderHandler
    {
        //property shaders
        private ComputeShader pressureShader;
        private ComputeShader typeShader;
        private ComputeShader objectShader;
        private ComputeShader smokeShader;
        private ComputeShader velocityShader;

        private ComputeShader activePropertyShader;


        private ComputeShader upscalingShader; //each cell will be represented by multiple
        private ComputeShader addArrowShader;
        private ComputeShader addBordersShader;

        private SimulationState simState;

        private RenderTexture colorTexture;
        private RenderTexture finalTexture;

        private int2 viewRes;


        public RenderHandler(ComputeShader pS, ComputeShader tS, ComputeShader oS, ComputeShader sS, ComputeShader vS, ComputeShader uS,
                             ComputeShader aAS, ComputeShader aBS, SimulationState simS, RenderTexture tex, int2 vRes) {
            pressureShader= pS;
            typeShader = tS;
            objectShader = oS;
            smokeShader = sS;
            velocityShader = vS;

            upscalingShader = uS;
            addArrowShader = aAS;
            addBordersShader = aBS;

            simState = simS;

            finalTexture = tex;

            viewRes = vRes;

            InitializeShaders();
        }

        public void RenderFinalTexture(bool showArrows, bool showBorders, ViewType viewType = ViewType.PRESSURE) {
            int kernel = pressureShader.FindKernel("CSMain");

            //pressureShader.Dispatch(kernel, (simState.simRes.x / 8) + 1, (simState.simRes.y / 8) + 1, 1);
            ComputeShader propertyShader;

            switch(viewType){
                case ViewType.PRESSURE:
                    propertyShader = pressureShader;
                    break;
                case ViewType.SMOKE:
                    propertyShader = smokeShader;
                    break;
                case ViewType.TYPE:
                    propertyShader = typeShader;
                    break;
                case ViewType.VELOCITY:
                    propertyShader = velocityShader;
                    break;
                case ViewType.VIEW_OBJECTS:
                    propertyShader = objectShader;
                    break;
                default:
                    propertyShader = pressureShader;
                    break;
            }
            propertyShader.Dispatch(0, (simState.simRes.x / 8) + 1, (simState.simRes.y / 8) + 1, 1);

            upscalingShader.Dispatch(kernel, (viewRes.x / 8) + 1, (viewRes.y / 8), 1);
            if(showBorders)
                addBordersShader.Dispatch(kernel, (viewRes.x / 8) + 1, (viewRes.y / 8), 1);
            if(showArrows)
                addArrowShader.Dispatch(kernel, (simState.simRes.x / 8) + 1, (simState.simRes.y / 8) + 1, 1);
        }

        public void ChangeActivePropertyShader(ViewType type){
            //Debug.Log("changed property shader");
            switch(type){
                case ViewType.PRESSURE: 
                    activePropertyShader = pressureShader;
                    break;
                case ViewType.TYPE:
                    activePropertyShader = typeShader;
                    break;
                case ViewType.SMOKE:
                    activePropertyShader = smokeShader;
                    break;
                case ViewType.VELOCITY:
                    activePropertyShader = velocityShader;
                    break;
                default:
                    return;

            }
        }

        private void InitializeShaders(){
            colorTexture = new RenderTexture(simState.simRes.x, simState.simRes.y, 0) {
                enableRandomWrite = true
            };
            colorTexture.Create();

            InitializePressureShader();
            InitializeTypeShader();
            InitializeObjectShader();
            InitializeSmokeShader();
            InitializeVelocityShader();

            activePropertyShader = pressureShader;

            InitializeUpscalingShader();
            InitializeArrowShader();
            InitializeBorderShader();
        }

        private void InitializePressureShader() {
            int kernel = pressureShader.FindKernel("CSMain");
            pressureShader.SetTexture(kernel, "Result", colorTexture);
            pressureShader.SetBuffer(kernel, "_pressure", simState.pressure.GetComputeBuffer());
            pressureShader.SetBuffer(kernel, "_type", simState.objectsMerged.GetComputeBuffer());
            pressureShader.SetInts("_simRes", new int[] {simState.simRes.x, simState.simRes.y});

            //pressureShader.Dispatch(kernel, (simState.simRes.x / 8) + 1, (simState.simRes.y / 8) + 1, 1);
        }

        private void InitializeTypeShader(){
            int kernel = typeShader.FindKernel("CSMain");
            typeShader.SetTexture(kernel, "Result", colorTexture);
            //typeShader.SetBuffer(kernel, "_type", simState.wall.GetComputeBuffer());
            typeShader.SetBuffer(kernel, "_type", simState.objectsMerged.GetComputeBuffer());
            typeShader.SetInts("_simRes", new int[] { simState.simRes.x, simState.simRes.y });

            //typeShader.Dispatch(kernel, (simState.simRes.x / 8) + 1, (simState.simRes.y / 8) + 1, 1);
        }

        private void InitializeObjectShader() {
            int kernel = objectShader.FindKernel("CSMain");
            objectShader.SetTexture(kernel, "Result", colorTexture);
            objectShader.SetBuffer(kernel, "_isObject", simState.objectsSeperated.GetComputeBuffer());
            objectShader.SetInt("_numberOfObjects", simState.numberOfObjects);
            objectShader.SetInts("_simRes", new int[] { simState.simRes.x, simState.simRes.y });

            //objectShader.Dispatch(kernel, (simState.simRes.x / 8) + 1, (simState.simRes.y / 8) + 1, 1);
        }

        private void InitializeSmokeShader(){
            int kernel = smokeShader.FindKernel("CSMain");
            smokeShader.SetTexture(kernel, "Result", colorTexture);
            smokeShader.SetBuffer(kernel, "_smokePressure", simState.smoke.GetComputeBuffer());
            smokeShader.SetBuffer(kernel, "_type", simState.objectsMerged.GetComputeBuffer());
            smokeShader.SetInts("_simRes", new int[] { simState.simRes.x, simState.simRes.y });

            //smokeShader.Dispatch(kernel, (simState.simRes.x / 8) + 1, (simState.simRes.y / 8) + 1, 1);
        }

        private void InitializeVelocityShader(){
            int kernel = velocityShader.FindKernel("CSMain");
            velocityShader.SetTexture(kernel, "Result", colorTexture);
            velocityShader.SetBuffer(kernel, "_velocityH", simState.velocityH.GetComputeBuffer());
            velocityShader.SetBuffer(kernel, "_velocityV", simState.velocityV.GetComputeBuffer());
            velocityShader.SetBuffer(kernel, "_type", simState.objectsMerged.GetComputeBuffer());
            velocityShader.SetInts("_simRes", new int[] { simState.simRes.x, simState.simRes.y });

            //velocityShader.Dispatch(kernel, (simState.simRes.x / 8) + 1, (simState.simRes.y / 8) + 1, 1);

        }

        private void InitializeUpscalingShader() {
            int kernel = upscalingShader.FindKernel("CSMain");
            upscalingShader.SetTexture(kernel, "Result", finalTexture);
            upscalingShader.SetTexture(kernel, "_color", colorTexture);
            upscalingShader.SetInts("_viewRes", new int[] {viewRes.x, viewRes.y});
            upscalingShader.SetInts("_simRes", new int[] {simState.simRes.x, simState.simRes.y});

            //upscalingShader.Dispatch(kernel, (viewRes.x / 8) + 1, (viewRes.y / 8), 1);
        }

        private void InitializeArrowShader() {
            int kernel = addArrowShader.FindKernel("CSMain");
            addArrowShader.SetTexture(kernel, "Result", finalTexture);
            addArrowShader.SetBuffer(kernel, "_verticalProperty", simState.velocityV.GetComputeBuffer());
            addArrowShader.SetBuffer(kernel, "_horizontalProperty", simState.velocityH.GetComputeBuffer());
            addArrowShader.SetInts("_simRes", new int[] { simState.simRes.x, simState.simRes.y });

            //addArrowShader.Dispatch(kernel, (simState.simRes.x / 8) + 1, (simState.simRes.y / 8) + 1, 1);
        }

        private void InitializeBorderShader() {
            int kernel = addBordersShader.FindKernel("CSMain");
            addBordersShader.SetTexture(kernel, "Result", finalTexture);
            addBordersShader.SetInts("_viewRes", new int[] { viewRes.x, viewRes.y });
            addBordersShader.SetInts("_simRes", new int[] { simState.simRes.x, simState.simRes.y });

            //addBordersShader.Dispatch(kernel, (viewRes.x / 8) + 1, (viewRes.y / 8), 1);
        }
    }

}
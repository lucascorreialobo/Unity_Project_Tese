using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class SimulateFluid : MonoBehaviour
{
    GameObject[] visualGrid;

    [Tooltip("Base element of grid (cell)")]
    public GameObject square;

    [Tooltip("Obstacle")]
    public GameObject obstacle;


    private float gravity = -9.81f;
	private float dt = 1.0f / 60.0f;
	private int numIters = 100;
	private int frameNr = 0;
	private float overRelaxation = 1.9f;
	private float obstacleX = 0.0f;
	private float obstacleY = 0.0f;
	private float obstacleRadius = 0.10f;
	private bool paused = false;
	private int sceneNr = 0;
	private bool showObstacle = false;
	private bool showStreamlines = false;
	private bool showVelocities = false;	
	private bool showPressure = false;
	private bool showSmoke = true;
	private Fluid fluid = null;




    // Start is called before the first frame update
    void Start()
    {
		float density = 1000.0f;
		float h = 0.01f;
		int numX = (int) Math.Floor(1 / h);
		int numY = (int) Math.Floor(1 / h);



		Fluid f = fluid = new Fluid(density, numX, numY, h);



        int n = f.numY;

        float inVel = 2.0f; //initial velocity

        for (var i = 0; i < f.numX; i++) {
            for (var j = 0; j < f.numY; j++) {
                int s = 1;      // fluid
                if (i == 0 || j == 0 || j == f.numY - 1)
                    s = 0;      // solid

                f.s[i * n + j] = s;


                if (i == 1) {
                    f.u[i * n + j] = inVel;
                }
            }
        }

        float pipeH = 0.1f * f.numY;
        int minJ = (int) Math.Floor(0.5 * f.numY - 0.5 * pipeH);
        int maxJ = (int) Math.Floor(0.5 * f.numY + 0.5 * pipeH);

        for (var j = minJ; j < maxJ; j++)
            f.m[j] = 0.0f;

        setObstacle(0.4f, 0.5f, true);
        DrawObstacle(0.4f, 0.5f);

        gravity = 0.0f;
        //scene.showPressure = false;
        //scene.showSmoke = true;
        //scene.showStreamlines = false;
        //scene.showVelocities = false;

        visualGrid = new GameObject[f.numCells];

        for (int i = 0; i < f.numX; i++) {
            for (int j = 0; j < f.numY; j++) {


                visualGrid[i * f.numY + j] = Instantiate(square, globalPos(i, j), Quaternion.identity);
                visualGrid[i * f.numY + j].GetComponent<Renderer>().material.color = new Color(100, 100, 100);
            }
        }

    }

    // Update is called once per frame
    void Update()
    {
        simulate();
        draw();
    }

    private void simulate() {
        fluid.simulate(dt, gravity, numIters);
        frameNr++;
    }

    private void draw() {
        //float cScale = 1; // canvas.height / simHeight = cell Scale
        //float cellScale = 1.1f; //this part of the code is wierd

        //float h = fluid.h;

        for (int i = 0; i < fluid.numX; i++)
            for (int j = 0; j < fluid.numY; j++) {
                float color = fluid.m[i * fluid.numY + j]; //color in range [0,1]
                color = fluid.u[i * fluid.numY + j]; //color in range [0,1]


                //int x = (int) Math.Floor(i * h);
                //int y = (int) Math.Floor((j + 1) * h);


                visualGrid[i * fluid.numY + j].GetComponent<Renderer>().material.SetColor("_Color", new Color(color, color, color));


            }


    }

    private void DrawObstacle(float x, float y) {

        //find center in cell coordinates
        float centerX = x / fluid.h - 0.5f; 
        float centerY = y / fluid.h - 0.5f;

        //Vector3 center = globalPos(centerX, centerY);
        obstacle = Instantiate(obstacle, globalPos(centerX, centerY), Quaternion.identity);
        obstacle.transform.localScale = Vector3.one / 0.5f;

    }


    private void setObstacle(float x, float y, bool reset) {

        float vx = 0.0f;
        float vy = 0.0f;

        if (!reset) {
            vx = (x - this.obstacleX) / this.dt;
            vy = (y - this.obstacleY) / this.dt;
        }

        this.obstacleX = x;
        this.obstacleY = y;
        float r = this.obstacleRadius;
        Fluid f = this.fluid;
        int n = f.numY;
        float cd = (float) Math.Sqrt(2) * f.h;

        for (var i = 1; i < f.numX - 2; i++) {
            for (var j = 1; j < f.numY - 2; j++) {

                f.s[i * n + j] = 1; //make every cell fluid

                float dx = (i + 0.5f) * f.h - x;
                float dy = (j + 0.5f) * f.h - y;

                if (dx * dx + dy * dy < r * r) {   // Is inside circle
                    f.s[i * n + j] = 0; //make solid

                    if (this.sceneNr == 2)      //not used, it was made for a paint simulation
                        f.m[i * n + j] = 0.5f + 0.5f * (float)Math.Sin(0.1f * this.frameNr);
                    else
                        f.m[i * n + j] = 1.0f;


                    f.u[i * n + j] = vx;
                    f.u[(i + 1) * n + j] = vx;
                    f.v[i * n + j] = vy;
                    f.v[i * n + j + 1] = vy;
                }
            }
        }

        this.showObstacle = true;
    }



    private Vector3 globalPos(float i, float j) {
        float vertExtent = Camera.main.orthographicSize;
        float horzExtent = vertExtent * Screen.width / Screen.height;

        float side = Math.Min(vertExtent, horzExtent);
        float squareSize = side * 2 / fluid.numY;

        square.transform.localScale = new Vector3(squareSize, squareSize, squareSize);

        float x = (-side + squareSize * i) + (squareSize / 2);
        float y = (-side + squareSize * j) + (squareSize / 2);

        return new Vector3(x, y, 0);
    }

    
}

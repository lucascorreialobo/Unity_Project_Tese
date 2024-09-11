using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class SimulateFluid : MonoBehaviour
{
    GameObject[] visualGrid;
    //prefab gameobject
    public GameObject square;

    private float gravity = -9.81f;
	private float dt = 1.0f / 120.0f;
	private int numIters = 100;
	private int frameNr = 0;
	private float overRelaxation = 1.9f;
	private float obstacleX = 0.0f;
	private float obstacleY = 0.0f;
	private float obstacleRadius = 0.15f;
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

        Debug.Log(numX);


		Fluid f = fluid = new Fluid(density, numX, numY, h);

        Debug.Log(f.numX);


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

        //setObstacle(0.4, 0.5, true)


        gravity = 0.0f;
        //scene.showPressure = false;
        //scene.showSmoke = true;
        //scene.showStreamlines = false;
        //scene.showVelocities = false;

        visualGrid = new GameObject[100*100];

        for (int i = 0; i < f.numX; i++) {
            for (int j = 0; j < f.numY; j++) {

                //Debug.Log("i: " + i + "; j: " + j);

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
        float cScale = 1; // canvas.height / simHeight = cell Scale
        float cellScale = 1.1f; //this part of the code is wierd

        float h = fluid.h;

        for (int i = 0; i < fluid.numX; i++)
            for (int j = 0; j < fluid.numY; j++) {
                float color = fluid.m[i * fluid.numY + j]; //color in range [0,1]

                //Debug.Log(color);

                int x = (int) Math.Floor(i * h);
                int y = (int) Math.Floor((j + 1) * h);


                visualGrid[i * fluid.numY + j].GetComponent<Renderer>().material.SetColor("_Color", new Color(color, color, color));


            }


    }


    private Vector3 globalPos(int i, int j) {
        float vertExtent = Camera.main.orthographicSize;
        float horzExtent = vertExtent * Screen.width / Screen.height;

        float side = Math.Min(vertExtent, horzExtent);
        float squareSize = side * 2 / 100;

        square.transform.localScale = new Vector3(squareSize, squareSize, squareSize);

        float x = (-side + squareSize * i) + (squareSize / 2);
        float y = (-side + squareSize * j) + (squareSize / 2);

        return new Vector3(x, y, 0);
    }
}

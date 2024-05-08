using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour {
    public int N = 40;

    public float dt = 0.01f;
    public float diff = 1;
    public float visc = 0;

    [Range(-100f, 100f)]
    public float velciX = 0;
    [Range(-100f, 100f)]
    public float velciY = 0;



    private float[] s;
    private float[] density;

    private float[] Vx;
    private float[] Vy;

    private float[] Vx0;
    private float[] Vy0;


    //prefab gameobject
    public GameObject square;
    //Grid of GameObjects
    private GameObject[] visualGrid;

    private int IX(int x, int y) {
        return N * y + x;
    }

    void AddDensity(int x, int y, float amount) {
        density[IX(x, y)] += amount;
    }

    void AddVelocity(int x, int y, float amountX, float amountY) {
        int index = IX(x, y);

        Vx[index] += amountX;
        Vy[index] += amountY;
    }

    void FluidStep() {

        diffuse(1, Vx0, Vx, visc, dt, 4);
        diffuse(2, Vy0, Vy, visc, dt, 4);

        project(Vx0, Vy0, Vx, Vy, 4);

        advect(1, Vx, Vx0, Vx0, Vy0, dt);
        advect(2, Vy, Vy0, Vx0, Vy0, dt);

        project(Vx, Vy, Vx0, Vy0, 4);

        diffuse(0, s, density, diff, dt, 4);
        advect(0, density, s, Vx, Vy, dt);
    }

    void set_bnd(int b, float[] x) {

        for (int i = 1; i < N - 1; i++) {
            x[IX(i, 0)] = b == 2 ? -x[IX(i, 1)] : x[IX(i, 1)];
            x[IX(i, N - 1)] = b == 2 ? -x[IX(i, N - 2)] : x[IX(i, N - 2)];
        }

        for (int j = 1; j < N - 1; j++) {
            x[IX(0, j)] = b == 1 ? -x[IX(1, j)] : x[IX(1, j)];
            x[IX(N - 1, j)] = b == 1 ? -x[IX(N - 2, j)] : x[IX(N - 2, j)];
        }


        x[IX(0, 0)] = 0.5f * (x[IX(1, 0)] + x[IX(0, 1)]);

        x[IX(0, N - 1)] = 0.5f * (x[IX(1, N - 1)] + x[IX(0, N - 2)]);

        x[IX(N - 1, 0)] = 0.5f * (x[IX(N - 2, 0)] + x[IX(N - 1, 1)]);

        x[IX(N - 1, N - 1)] = 0.5f * (x[IX(N - 2, N - 1)] + x[IX(N - 1, N - 2)]);
    }

    void lin_solve(int b, float[] x, float[] x0, float a, float c, int iter) {
        float cRecip = 1.0f / c;
        for (int k = 0; k < 8; k++) {
            for (int j = 1; j < N - 1; j++) {
                for (int i = 1; i < N - 1; i++) {
                    x[IX(i, j)] =
                        (x0[IX(i, j)] +
                            a * (
                                  x[IX(i + 1, j)]
                                + x[IX(i - 1, j)]
                                + x[IX(i, j + 1)]
                                + x[IX(i, j - 1)]
                                + x[IX(i, j)]
                                + x[IX(i, j)]
                            )
                        ) * cRecip;
                }
            }

            set_bnd(b, x);
        }
    }

    void diffuse(int b, float[] x, float[] x0, float diff, float dt, int iter) {
        float a = dt * diff * (N - 2) * (N - 2);
        lin_solve(b, x, x0, a, 1 + 6 * a, iter);
    }

    void project(float[] velocX, float[] velocY, float[] p, float[] div, int iter) {

        for (int j = 1; j < N - 1; j++) {
            for (int i = 1; i < N - 1; i++) {
                div[IX(i, j)] = -0.5f * (
                         velocX[IX(i + 1, j)]
                        - velocX[IX(i - 1, j)]
                        + velocY[IX(i, j + 1)]
                        - velocY[IX(i, j - 1)]
                    ) / N;
                p[IX(i, j)] = 0;
            }
        }

        set_bnd(0, div);
        set_bnd(0, p);
        lin_solve(0, p, div, 1, 6, iter);

        for (int j = 1; j < N - 1; j++) {
            for (int i = 1; i < N - 1; i++) {
                velocX[IX(i, j)] -= 0.5f * (p[IX(i + 1, j)]
                                                - p[IX(i - 1, j)]) * N;
                velocY[IX(i, j)] -= 0.5f * (p[IX(i, j + 1)]
                                                - p[IX(i, j - 1)]) * N;
            }
        }
        set_bnd(1, velocX);
        set_bnd(2, velocY);
    }

    void advect(int b, float[] d, float[] d0, float[] velocX, float[] velocY, float dt) {
        float i0, i1, j0, j1;

        float dtx = dt * (N - 2);
        float dty = dt * (N - 2);

        float s0, s1, t0, t1;
        float tmp1, tmp2, x, y;

        float Nfloat = N;
        float ifloat, jfloat;
        int i, j;

        for (j = 1, jfloat = 1; j < N - 1; j++, jfloat++) {
            for (i = 1, ifloat = 1; i < N - 1; i++, ifloat++) {
                tmp1 = dtx * velocX[IX(i, j)];
                tmp2 = dty * velocY[IX(i, j)];
                x = ifloat - tmp1;
                y = jfloat - tmp2;

                if (x < 0.5f) x = 0.5f;
                if (x > Nfloat + 0.5f) x = Nfloat + 0.5f;
                i0 = Mathf.Floor(x);
                i1 = i0 + 1.0f;
                if (y < 0.5f) y = 0.5f;
                if (y > Nfloat + 0.5f) y = Nfloat + 0.5f;
                j0 = Mathf.Floor(y);
                j1 = j0 + 1.0f;

                s1 = x - i0;
                s0 = 1.0f - s1;
                t1 = y - j0;
                t0 = 1.0f - t1;

                int i0i = (int) i0;
                int i1i = (int) i1;
                int j0i = (int) j0;
                int j1i = (int) j1;

                d[IX(i, j)] =
                    s0 * (t0 * d0[IX(i0i, j0i)] + t1 * d0[IX(i0i, j1i)])
                  + s1 * (t0 * d0[IX(i1i, j0i)] + t1 * d0[IX(i1i, j1i)]);
            }
        }

        set_bnd(b, d);
    }


    // Start is called before the first frame update
    void Start() {
        s = new float[N * N];
        density = new float[N * N];

        Vx = new float[N * N];
        Vy = new float[N * N];

        Vx0 = new float[N * N];
        Vy0 = new float[N * N];

        visualGrid = new GameObject[N * N];

        for (int j = 0; j < N; j++) {
            for (int i = 0; i < N; i++) {

                visualGrid[IX(i, j)] = Instantiate(square, globalPos(i, j), Quaternion.identity);
                visualGrid[IX(i, j)].GetComponent<Renderer>().material.color = new Color(100, 100, 100);

                AddVelocity(i, j, 5, 0);

            }

        }

        AddDensity(N / 2, N / 2, 100);
    }

    // Update is called once per frame
    void Update() {
        FluidStep();
        draw_dens();
        fadeDens();
        AddDensity(N / 2, N / 2, 100);
        AddVelocity(N / 2, N / 2, velciX, velciY);

        if (Input.GetMouseButton(0)) {
            var mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0f; // zero z
        }
    }

    

    private void draw_dens() {
        for (int j = 0; j < N; j++) {
            for (int i = 0; i < N; i++) {
                //float normalizedDensity = 1 - Math.Min(density[IX(i, j)] / 10, 1f);
                float d = density[IX(i, j)];


                visualGrid[IX(i, j)].GetComponent<Renderer>().material.SetColor("_Color", new Color(d, d, d));
            }
        }
    }
    private Vector3 globalPos(int i, int j) {
        float vertExtent = Camera.main.orthographicSize;
        float horzExtent = vertExtent * Screen.width / Screen.height;

        float side = Math.Min(vertExtent, horzExtent);
        float squareSize = side * 2 / N;

        square.transform.localScale = new Vector3(squareSize, squareSize, squareSize);

        float x = (-side + squareSize * i) + (squareSize / 2);
        float y = (-side + squareSize * j) + (squareSize / 2);

        return new Vector3(x, y, 0);
    }

    private void fadeDens() {
        for(int i = 0; i < density.Length; i++) {
            density[i] -= 0.01f;
            density[i] = Mathf.Max(density[i], 0);
        }
    }
}

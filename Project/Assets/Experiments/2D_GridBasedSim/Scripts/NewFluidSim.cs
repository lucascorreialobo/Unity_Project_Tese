using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using Color = UnityEngine.Color;

public class NewFluidSim : MonoBehaviour
{

    public const int const_N = 20;

    //velocity in each axis
    private float[] global_u = new float[(const_N + 2) * (const_N + 2)];
    private float[] global_v = new float[(const_N + 2) * (const_N + 2)];
    private float[] global_u_prev = new float[(const_N + 2) * (const_N + 2)];
    private float[] global_v_prev = new float[(const_N + 2) * (const_N + 2)];

    //density
    private float[] dens = new float[(const_N + 2) * (const_N + 2)];
    private float[] dens_prev = new float[(const_N + 2) * (const_N + 2)];


    public float global_visc = 1;
    public float global_dt = 0.01f;
    public float global_diff = 1;


    //prefab gameobject
    public GameObject square;
    

    //Grid of GameObjects
    private GameObject[] visualGrid = new GameObject[(const_N + 2) * (const_N + 2)];

    void add_source(int N, float[] x, float[] s, float dt) {
        int i, size = (N + 2) * (N + 2);
        for (i = 0; i < size; i++) 
            x[i] += dt * s[i];
    }

    void diffuse(int N, int b, float[] x, float[] x0, float diff, float dt) {
        int i, j, k;
        float a = dt * diff * N * N;
        for (k = 0; k < 20; k++) {
            for (i = 1; i <= N; i++) {
                for (j = 1; j <= N; j++) {
                    x[G(i, j)] = (x0[G(i, j)] + a * (x[G(i - 1, j)] + x[G(i + 1, j)] +
                    x[G(i, j - 1)] + x[G(i, j + 1)])) / (1 + 4 * a);
                }
            }
            set_bnd(N, b, x);
        }
    }

    void advect(int N, int b, float[] d, float[] d0, float[] u, float[] v, float dt) {
        int i, j, i0, j0, i1, j1;
        float x, y, s0, t0, s1, t1, dt0;
        dt0 = dt * N;
        for (i = 1; i <= N; i++) {
            for (j = 1; j <= N; j++) {
                x = i - dt0 * u[G(i, j)]; y = j - dt0 * v[G(i, j)];
                if (x < 0.5) x = 0.5f; if (x > N + 0.5) x = N + 0.5f; i0 = (int)x; i1 = i0 + 1;
                if (y < 0.5) y = 0.5f; if (y > N + 0.5) y = N + 0.5f; j0 = (int)y; j1 = j0 + 1;
                s1 = x - i0; s0 = 1 - s1; t1 = y - j0; t0 = 1 - t1;
                d[G(i, j)] = s0 * (t0 * d0[G(i0, j0)] + t1 * d0[G(i0, j1)]) +
                s1 * (t0 * d0[G(i1, j0)] + t1 * d0[G(i1, j1)]);
            }
        }
        set_bnd(N, b, d);
    }

    void project(int N, float[] u, float[] v, float[] p, float[] div) {
        int i, j, k;
        float h;
        h = 1.0f / N;
        for (i = 1; i <= N; i++) {
            for (j = 1; j <= N; j++) {
                div[G(i, j)] = -0.5f * h * (u[G(i + 1, j)] - u[G(i - 1, j)] +
                v[G(i, j + 1)] - v[G(i, j - 1)]);
                p[G(i, j)] = 0;
            }
        }
        set_bnd(N, 0, div); set_bnd(N, 0, p);
        for (k = 0; k < 20; k++) {
            for (i = 1; i <= N; i++) {
                for (j = 1; j <= N; j++) {
                    p[G(i, j)] = (div[G(i, j)] + p[G(i - 1, j)] + p[G(i + 1, j)] +
                    p[G(i, j - 1)] + p[G(i, j + 1)]) / 4;
                }
            }
            set_bnd(N, 0, p);
        }
        for (i = 1; i <= N; i++) {
            for (j = 1; j <= N; j++) {
                u[G(i, j)] -= 0.5f * (p[G(i + 1, j)] - p[G(i - 1, j)]) / h;
                v[G(i, j)] -= 0.5f * (p[G(i, j + 1)] - p[G(i, j - 1)]) / h;
            }
        }
        set_bnd(N, 1, u); set_bnd(N, 2, v);
    }

    void set_bnd(int N, int b, float[] x) {
        int i;
        for (i = 1; i <= N; i++) {
            x[G(0, i)]      = b == 1 ? -x[G(1, i)] : x[G(1, i)];
            x[G(N + 1, i)]  = b == 1 ? -x[G(N, i)] : x[G(N, i)];
            x[G(i, 0)]      = b == 2 ? -x[G(i, 1)] : x[G(i, 1)];
            x[G(i, N + 1)]  = b == 2 ? -x[G(i, N)] : x[G(i, N)];
        }
        x[G(0, 0)] = 0.5f * (x[G(1, 0)] + x[G(0, 1)]);
        x[G(0, N + 1)] = 0.5f * (x[G(1, N + 1)] + x[G(0, N)]);
        x[G(N + 1, 0)] = 0.5f * (x[G(N, 0)] + x[G(N + 1, 1)]);
        x[G(N + 1, N + 1)] = 0.5f * (x[G(N, N + 1)] + x[G(N + 1, N)]);
    }




    void dens_step(int N, float[] x, float[] x0, float[] u, float[] v, float diff, float dt) {
        //add_source(N, x, x0, dt);
        diffuse(N, 0, x0, x, diff, dt);//instead of swap I'm simply switching the order
        advect(N, 0, x, x0, u, v, dt);
    }

    void vel_step(int N, float[] u, float[] v, float[] u0, float[] v0, float visc, float dt) {
        //add_source(N, u, u0, dt); add_source(N, v, v0, dt);

        //SWAP(u0, u); SWAP(v0, v);
        diffuse(N, 1, u0, u, visc, dt); diffuse(N, 2, v0, v, visc, dt);

        project(N, u0, v0, u, v);
        //SWAP(u0, u); SWAP(v0, v);
        advect(N, 1, u, u0, u0, v0, dt); advect(N, 2, v, v0, u0, v0, dt);
        project(N, u, v, u0, v0);
    }

    private int G(int i, int j) {
        return (const_N + 2) * j + i;
    }

    private Vector3 globalPos(int i, int j) {
        float vertExtent = Camera.main.orthographicSize;
        float horzExtent = vertExtent * Screen.width / Screen.height;

        float side = Math.Min(vertExtent, horzExtent);
        float squareSize = side * 2 / (const_N + 2);

        square.transform.localScale = new Vector3(squareSize, squareSize, squareSize);

        float x = (-side + squareSize * i) + (squareSize/2);
        float y = (-side + squareSize * j) + (squareSize/2);

        return new Vector3(x, y, 0);
    }

    private void Update() {
        smokeMachine(const_N, dens, global_u);

        vel_step(const_N, global_u, global_v, global_u_prev, global_v_prev, global_visc, global_dt);
        dens_step(const_N, dens, dens_prev, global_u, global_v, global_diff, global_dt);
        draw_dens(const_N, dens);

        

        Debug.Log(dens[G(const_N / 2, 1)]);
    }

    private void smokeMachine(int N, float[] d, float[] u) {
        d[G(N / 2, 1)] += 1;
        u[G(N / 2, 1)] += 1;
    }

    private void fade(int N, float[] dens) {
        for(int i = 0; i < N; i++) {
            dens[i] -= 5;
        }
    }


    private void draw_dens(int const_N, float[] dens) {
        for (int j = 0; j < const_N + 2; j++) {
            for (int i = 0; i < const_N + 2; i++) {
                float normalizedDensity = 1 - Math.Min(dens[G(i, j)] / 10, 1f);

                visualGrid[G(i, j)].GetComponent<Renderer>().material.SetColor("_Color", new Color(normalizedDensity, normalizedDensity, normalizedDensity));
            }
        }


    }

    private void Start() {
        


        for(int j = 0; j < const_N + 2; j++) {
            for (int i = 0; i < const_N + 2; i++) {

                visualGrid[G(i, j)] = Instantiate(square, globalPos(i,j), Quaternion.identity);
                visualGrid[G(i, j)].GetComponent<Renderer>().material.color = new UnityEngine.Color(100, 100, 100);

                

                dens[G(i, j)] = 5;
            }

        }

    }

}

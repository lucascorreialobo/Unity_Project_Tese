

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public class velocityGrid : MonoBehaviour
{
    
    [SerializeField]
    private static int width = 10;
    [SerializeField]
    private static int height = 10;



    private Vector2[] grid = new Vector2[(width + 2) * (height + 2)]; //grid represented in a single array; IX(i,j) = i + width*j |   grid.x - density | grid.y - velocity
    private Vector2[] grid_prev = new Vector2[(width + 2) * (height + 2)]; // +2 to account for boundary cells

    private float[] s = new float[(width + 2) * (height + 2)]; //sources of density, for each cell
    private float[] u = new float[(width + 2) * (height + 2)]; //velocity.x ????????
    //private float[] x = new float[(width + 2) * (height + 2)]; //density ????????
    private float[] v = new float[(width + 2) * (height + 2)]; //velocity.y ????????

    private float dt = 0.1f; // time step
    private float diff = 0.1f; //natural diffusion?


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /**
     * Adds density to "grid" if in "s"
     */
    void add_source() {
        for(int i  = 0; i < grid.Length; i++) {
            grid[i].x += s[i] * dt;
        }
    }

    //void diffuse() {


    //    int i, j, k;
    //    float a = dt * diff * width * height;
    //    for (k = 0; k < 20; k++) {
    //        for (i = 1; i <= width; i++) {
    //            for (j = 1; j <= height; j++) {
    //                grid[G(i, j)] = (grid_prev[G(i, j)] + a * (grid[G(i - 1, j)] + grid[G(i + 1, j)] + grid[G(i, j - 1)] + grid[G(i, j + 1)])) / (1 + 4 * a);
    //            }
    //        }
    //        set_bnd(N, b, grid);
    //    }
    //}

    //void advect() {
    //    int i, j, i0, j0, i1, j1;
    //    float x, y, s0, t0, s1, t1, dt0;
    //    dt0 = dt * width;
    //    for (i = 1; i <= width; i++) {
    //        for (j = 1; j <= height; j++) {
    //            x = i - dt0 * u[G(i, j)]; 
    //            y = j - dt0 * v[G(i, j)];

    //            if (x < 0.5) x = 0.5f; if (x > N + 0.5) x = N + 0.5; i0 = (int)x; i1 = i0 + 1;

    //            if (y < 0.5) y = 0.5f; if (y > N + 0.5) y = N + 0.5; j0 = (int)y; j1 = j0 + 1;

    //            s1 = x - i0; s0 = 1 - s1; t1 = y - j0; t0 = 1 - t1;
    //            d[G(i, j)] = s0 * (t0 * d0[G(i0, j0)] + t1 * d0[G(i0, j1)]) +
    //            s1 * (t0 * d0[G(i1, j0)] + t1 * d0[G(i1, j1)]);
    //        }
    //    }
    //    set_bnd(N, b, d);
    //}

    private void set_bnd(object n, object b, object d) {
        throw new NotImplementedException();
    }

    /**
     * get array position equivalent to grid position(i,j)
     */
    private int G(int i, int j) {
        return ((i) + (width + 2) * (j));
    }
}

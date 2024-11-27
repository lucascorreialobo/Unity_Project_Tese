using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public class Fluid {

    public float density;
    public int numX;       //simulation space width  (in number of pixels)
    public int numY;       //simulation space height (in number of pixels)
    public int numCells;   //total number of cells/pixels (numX * numY)
    public float h;        //grid spacing (ex value: 0.01)
    public float[] u;      //horizontal velocity
    public float[] v;      //vertical velocity
    public float[] newU;   
    public float[] newV;
    public float[] p;       //pressure
    public int[] s;         //type of cell: 0: walls | 1: fluid
    public float[] m;       //contains smoke information
    public float[] newM;

    float overRelaxation = 1.9f; //relaxation factor to solve Gauss-Seidel

    private enum FIELD {
        U_FIELD,
        V_FIELD,
        S_FIELD
    }

    public Fluid(float density, int numX, int numY, float h) {
        this.density    = density;
        this.numX       = numX + 2; 
        this.numY       = numY + 2;
        this.numCells   = this.numX * this.numY;
        this.h          = h;
        this.u          = new float[numCells];
        this.v          = new float[numCells];
        this.newU       = new float[numCells];
        this.newV       = new float[numCells];
        this.p          = new float[numCells];
        this.s          = new int  [numCells];
        this.m          = new float[numCells];
        this.newM       = new float[numCells];
        Array.Fill(m, 1);


        float num = numX * numY;
    }

    private void integrate(float dt, float gravity) {
        var n = this.numY;
        for (var i = 1; i < this.numX; i++) {
            for (var j = 1; j < this.numY - 1; j++) {
                if (this.s[i * n + j] != 0.0 && this.s[i * n + j - 1] != 0.0)
                    this.v[i * n + j] += gravity * dt;
            }
        }
    }

    private void solveIncompressibility(int numIters, float dt) {
        int n = this.numY;
        float cp = this.density * this.h / dt;

        for (var iter = 0; iter < numIters; iter++) {

            for (var i = 1; i < this.numX - 1; i++) {
                for (var j = 1; j < this.numY - 1; j++) {

                    if (this.s[i * n + j] == 0.0)
                        continue;

                    //get the 4 orthogonaly connected cells
                    int s = this.s[i * n + j];
                    int sx0 = this.s[(i - 1) * n + j];
                    int sx1 = this.s[(i + 1) * n + j];
                    int sy0 = this.s[i * n + j - 1];
                    int sy1 = this.s[i * n + j + 1];
                    s = sx0 + sx1 + sy0 + sy1;
                    if (s == 0.0) //if all orthogonaly connected cells are solid cell doesn't change
                        continue;

                    //calculate total outflow
                    float div = this.u[(i + 1) * n + j] - this.u[i * n + j] +
                        this.v[i * n + j + 1] - this.v[i * n + j]; 

                    float p = -div / s;
                    p *= this.overRelaxation;
                    this.p[i * n + j] += cp * p;

                    this.u[i * n + j] -= sx0 * p;
                    this.u[(i + 1) * n + j] += sx1 * p;
                    this.v[i * n + j] -= sy0 * p;
                    this.v[i * n + j + 1] += sy1 * p;
                }
            }
        }
    }

    //take care of border cells
    private void extrapolate() {
        int n = this.numY;
        for (var i = 0; i < this.numX; i++) {
            this.u[i * n + 0] = this.u[i * n + 1];
            this.u[i * n + this.numY - 1] = this.u[i * n + this.numY - 2];
        }
        for (var j = 0; j < this.numY; j++) {
            this.v[0 * n + j] = this.v[1 * n + j];
            this.v[(this.numX - 1) * n + j] = this.v[(this.numX - 2) * n + j];
        }
    }

    private float sampleField(float x, float y, FIELD field) {  //get vector (velocityX || velocityY || smoke_density) on point x,y 
        int n = this.numY;
        float h = this.h;
        float h1 = 1.0f / h;
        float h2 = 0.5f * h;

        x = Math.Max(Math.Min(x, this.numX * h), h);
        y = Math.Max(Math.Min(y, this.numY * h), h);

        

        float dx = 0.0f;
        float dy = 0.0f;

        float[] f;

        switch (field) {
            case FIELD.U_FIELD: f = this.u; dy = h2; break;
            case FIELD.V_FIELD: f = this.v; dx = h2; break;
            case FIELD.S_FIELD: f = this.m; dx = h2; dy = h2; break;
            default: f = this.u; break;
        }

 

        int x0 = Math.Min((int) Math.Floor((x - dx) * h1), this.numX - 1); //get X (in pixels) of cell containing point(x,y) (in real coordinate)
        float tx = ((x - dx) - x0 * h) * h1;
        int x1 = Math.Min(x0 + 1, this.numX - 1);                          //get X + 1

        int y0 = Math.Min((int) Math.Floor((y - dy) * h1), this.numY - 1);
        float ty = ((y - dy) - y0 * h) * h1;                                  
        int y1 = Math.Min(y0 + 1, this.numY - 1);

        float sx = 1.0f - tx;
        float sy = 1.0f - ty;

        float val = sx * sy * f[x0 * n + y0] +
                tx * sy * f[x1 * n + y0] +
                tx * ty * f[x1 * n + y1] +
                sx * ty * f[x0 * n + y1];

        return val;
    }


    private float avgU(int i,int  j) {
        var n = this.numY;
        var u = (this.u[i * n + j - 1] + this.u[i * n + j] +
            this.u[(i + 1) * n + j - 1] + this.u[(i + 1) * n + j]) * 0.25f;
        return u;
    }

    private float avgV(int i,int j) {
        var n = this.numY;
        var v = (this.v[(i - 1) * n + j] + this.v[i * n + j] +
            this.v[(i - 1) * n + j + 1] + this.v[i * n + j + 1]) * 0.25f;
        return v;
    }


    private void advectVel(float dt) {



        Array.Copy(this.u, this.newU, this.u.Length);
        Array.Copy(this.v, this.newV, this.u.Length);

        //this.newU.set(this.u);
        //this.newV.set(this.v);

        var n = this.numY;
        //var h = this.h;
        var h2 = 0.5f * h;

        for (var i = 1; i < this.numX; i++) {
            for (var j = 1; j < this.numY; j++) {

                // cnt++;

                // u component
                if (this.s[i * n + j] != 0.0 && this.s[(i - 1) * n + j] != 0.0 && j < this.numY - 1) {
                    var x = i * h;
                    var y = j * h + h2;
                    var u = this.u[i * n + j];
                    var v = this.avgV(i, j);
                    //						var v = this.sampleField(x,y, V_FIELD);
                    x = x - dt * u;
                    y = y - dt * v;
                    u = this.sampleField(x, y, FIELD.U_FIELD);
                    this.newU[i * n + j] = u;
                }
                // v component
                if (this.s[i * n + j] != 0.0 && this.s[i * n + j - 1] != 0.0 && i < this.numX - 1) {
                    var x = i * h + h2;
                    var y = j * h;
                    var u = this.avgU(i, j);
                    //						var u = this.sampleField(x,y, U_FIELD);
                    var v = this.v[i * n + j];
                    x = x - dt * u;
                    y = y - dt * v;
                    v = this.sampleField(x, y, FIELD.V_FIELD);
                    this.newV[i * n + j] = v;
                }
            }
        }

        Array.Copy(this.newU, this.u, this.u.Length);
        Array.Copy(this.newV, this.v, this.u.Length);
    }

    private void advectSmoke(float dt) {

        //this.newM.set(this.m);
        Array.Copy(this.m, this.newM, this.m.Length);



        int n = this.numY;
        float h = this.h;
        float h2 = 0.5f * h;

        for (var i = 1; i < this.numX - 1; i++) {
            for (var j = 1; j < this.numY - 1; j++) {

                if (this.s[i * n + j] != 0.0) {
                    float u = (this.u[i * n + j] + this.u[(i + 1) * n + j]) * 0.5f;
                    float v = (this.v[i * n + j] + this.v[i * n + j + 1]) * 0.5f;
                    float x = i * h + h2 - dt * u;
                    float y = j * h + h2 - dt * v;

                    this.newM[i * n + j] = this.sampleField(x, y, FIELD.S_FIELD);
                }
            }
        }

        //this.m.set(this.newM);
        Array.Copy(this.newM, this.m, this.m.Length);
    }


    public void simulate(float dt, float gravity, int numIters) {

        this.integrate(dt, gravity);


        Array.Fill(p, 0.0f);
        this.solveIncompressibility(numIters, dt);

        // this.extrapolate();
        // this.advectVel(dt);
        // this.advectSmoke(dt);
    }

}

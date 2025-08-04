using UnityEngine;
using Utils;

namespace _2D_Shader_V3 {

public class SimulationState
{
    public Buf2<float> pressure;
    public Buf2<float> velocityV;
    public Buf2<float> velocityH;
    public Buf2<int> type; // 0 - wall | 1 - fluid
    public Buf2<float> smoke;

    public SimulationState(Vector2Int simRes){
        pressure = new Buf2<float>(simRes.x, simRes.y);
        velocityV = new Buf2<float>(simRes.x, simRes.y);
        velocityH = new Buf2<float>(simRes.x, simRes.y);
        type = new Buf2<int>(simRes.x, simRes.y);
        smoke = new Buf2<float>(simRes.x, simRes.y);

        
    }


    public void Destroy(){
        pressure.Release();
        velocityV.Release();
        velocityH.Release();
        type.Release();
        smoke.Release();
        
    }
}
}
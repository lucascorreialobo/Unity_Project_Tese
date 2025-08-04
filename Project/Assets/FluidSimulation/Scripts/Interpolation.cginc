

float interpolateProperty(RWStructuredBuffer<float> property, uint2 cell, float2 decimalOffset)
{
    int simXY_1D = cell.x * _simRes.y + cell.y;
    
    //original values
    float p_bl = property[simXY_1D];
    float p_br = property[RIGHT(simXY_1D)];
    float p_tl = property[UP(simXY_1D)];
    float p_tr = property[UP(RIGHT(simXY_1D))];
    
    //weighted affect on interpolated value
    float bl = p_bl * (1 - decimalOffset.x) * (1 - decimalOffset.y);
    float br = p_br *     (decimalOffset.x) * (1 - decimalOffset.y);
    float tl = p_tl * (1 - decimalOffset.x) *      decimalOffset.y;
    float tr = p_tr *     (decimalOffset.x) *      decimalOffset.y;
    
    float interpolatedValue = bl + br + tl + tr;
    return interpolatedValue;
}

float2 velocityXY_of_V_edge(RWStructuredBuffer<float> velocityV, RWStructuredBuffer<float> velocityH, uint2 index, uint simXY_1D)
{
    bool hasRight = index.x < _simRes.x - 1;
    bool hasDown = index.y > 0;
    
    float h_tl = velocityV[simXY_1D];
    float h_tr = hasRight ? velocityV[RIGHT(simXY_1D)] : 0;
    float h_bl = hasDown ? velocityV[DOWN(simXY_1D)] : 0;
    float h_br = hasRight && hasDown ? velocityV[DOWN(RIGHT(simXY_1D))] : 0;
    
    float velocityH_interpolated = (h_tl + h_tr + h_bl + h_br ) / 4;
    
    return float2(velocityH_interpolated, velocityV[simXY_1D]);
}

float2 velocityXY_of_V_edge_NoChecks(RWStructuredBuffer<float> velocityV, RWStructuredBuffer<float> velocityH, uint2 index, uint simXY_1D)
{
    float h_tl = velocityV[simXY_1D];
    float h_tr = velocityV[RIGHT(simXY_1D)];
    float h_bl = velocityV[DOWN(simXY_1D)];
    float h_br = velocityV[DOWN(RIGHT(simXY_1D))];
    
    float velocityH_interpolated = (h_tl + h_tr + h_bl + h_br ) / 4;
    
    return float2(velocityH_interpolated, velocityV[simXY_1D]);
}

float2 velocityXY_of_cell_center(RWStructuredBuffer<float> velocityV, RWStructuredBuffer<float> velocityH, uint2 index, uint simXY_1D)
{
    bool hasRight = index.x < _simRes.x - 1;
    bool hasUP = index.y < _simRes.y - 1;
    
    float interpolatedV = hasRight ? (velocityV[simXY_1D] + velocityV[UP   (simXY_1D)]) / 2 : 0;
    float interpolatedH = hasUP    ? (velocityH[simXY_1D] + velocityH[RIGHT(simXY_1D)]) / 2 : 0;

    
    return float2(interpolatedH, interpolatedV);
}
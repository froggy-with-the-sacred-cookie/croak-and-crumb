// Bruits et vent partagés par les shaders de Froggy.
// Le vent a une seule source (skill tech-art) : VentJardin pose _FroggyVent chaque image.
#ifndef FROGGY_BRUIT
#define FROGGY_BRUIT

float4 _FroggyVent;   // xy : direction du vent sur le sol (x, z du monde) ; z : force 0-1 ; w : horloge du vent

float FroggyHorloge() { return _FroggyVent.w > 0 ? _FroggyVent.w : _Time.y; }

float2 FroggyDirectionVent()
{
    float l = length(_FroggyVent.xy);
    return l > 0.001 ? _FroggyVent.xy / l : float2(1, 0);
}

float Hachage1(float2 p) { return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453); }

float2 Hachage2(float2 p)
{
    p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
    return frac(sin(p) * 43758.5453);
}

float BruitValeur(float2 p)
{
    float2 i = floor(p), f = frac(p);
    float2 u = f * f * (3 - 2 * f);
    return lerp(lerp(Hachage1(i), Hachage1(i + float2(1, 0)), u.x),
                lerp(Hachage1(i + float2(0, 1)), Hachage1(i + float2(1, 1)), u.x), u.y);
}

float Fbm(float2 p)
{
    float s = 0, a = 0.5;
    for (int k = 0; k < 4; k++) { s += a * BruitValeur(p); p = p * 2.03 + 17.1; a *= 0.5; }
    return s;
}

// Distances au point le plus proche et au deuxième (F1, F2) ; les points tournent lentement avec t.
float2 Voronoi(float2 x, float t)
{
    float2 n = floor(x), f = frac(x);
    float f1 = 8, f2 = 8;
    for (int j = -1; j <= 1; j++)
    for (int i = -1; i <= 1; i++)
    {
        float2 g = float2(i, j);
        float2 o = 0.5 + 0.5 * sin(t + 6.2831 * Hachage2(n + g));
        float2 r = g + o - f;
        float d = dot(r, r);
        if (d < f1) { f2 = f1; f1 = d; } else if (d < f2) f2 = d;
    }
    return sqrt(float2(f1, f2));
}

#endif

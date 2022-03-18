using System.Collections;
using System.Collections.Generic;
using UnityEngine;


using UnityEditor;


public class OrbitaEllittica: MonoBehaviour
{
    struct Constants    
    {
        public const float G = 6.67f*10;
    }

    struct Math         
    {
        public const float TAU = 6.2831f;
    }

    //Parametri orbitali 
    [SerializeField] float SemiAsseMaggiore = 20f;        //a 
    [SerializeField] [Range(0f, 0.99f)] float Eccentricita;             //e 
    [SerializeField] [Range(0f, Math.TAU)] float Inclinazione = 0f;         //i 
    [SerializeField] [Range(0f, Math.TAU)] float LongitudineDelNodoAscendente;  //n 
    [SerializeField] [Range(0f, Math.TAU)] float ArgomentoDelPerielio;      //w 
    [SerializeField] float LongitudineMediaSferica;             //L 
    [SerializeField] OnRailsReferenceBody referenceBody;
    [SerializeField] float AnomliaMedia;
    [Space]
    
    [SerializeField] float accuracyTolerance = 1e-6f;
    [SerializeField] int maxIterations = 5;           //converge dopo 3/5 iterazioni

    
    [HideInInspector] [SerializeField] float mu;
    [HideInInspector] [SerializeField] float n, cosLOAN, sinLOAN, sinI, cosI, AnomaliaVeraCostante;

    private void OnValidate() => PuntiOrbitali.Clear();
    void Awake() => CalcolaCostanti();
    public float F(float E, float e, float M)  
    {
        return (M - E + e * Mathf.Sin(E));
    }
    public float DF(float E, float e)      
    {
        return (-1f) + e * Mathf.Cos(E);
    }
    public void CalcolaCostanti()    //NumeroCostanti
    {
        mu = Constants.G * referenceBody.mass;
        n = Mathf.Sqrt(mu / Mathf.Pow(SemiAsseMaggiore, 3));
        AnomaliaVeraCostante = Mathf.Sqrt((1 + Eccentricita) / (1 - Eccentricita));
        cosLOAN = Mathf.Cos(LongitudineDelNodoAscendente);
        sinLOAN = Mathf.Sin(LongitudineDelNodoAscendente);
        cosI = Mathf.Cos(Inclinazione);
        sinI = Mathf.Sin(Inclinazione);
    }

    void Update()
    {
        CalcolaCostanti();

        AnomliaMedia = (float)(n * (Time.time/* - LongitudineMediaSferica*/));

        float E1 = AnomliaMedia;   
        float difference = 1f;
        for (int i = 0; difference > accuracyTolerance && i < maxIterations; i++)
        {
            float E0 = E1;
            E1 = E0 - F(E0, Eccentricita, AnomliaMedia) / DF(E0, Eccentricita);
            difference = Mathf.Abs(E1 - E0);
        }
        float AnomaliaEccentrica = E1;

        float AnomaliaVera = 2 * Mathf.Atan(AnomaliaVeraCostante * Mathf.Tan(AnomaliaEccentrica / 2));
        float distanza = SemiAsseMaggiore * (1 - Eccentricita * Mathf.Cos(AnomaliaEccentrica));

        float cosAOPPlusTA = Mathf.Cos(ArgomentoDelPerielio + AnomaliaVera);
        float sinAOPPlusTA = Mathf.Sin(ArgomentoDelPerielio + AnomaliaVera);

        float x = distanza * ((cosLOAN * cosAOPPlusTA) - (sinLOAN * sinAOPPlusTA * cosI));
        float z = distanza * ((sinLOAN * cosAOPPlusTA) + (cosLOAN * sinAOPPlusTA * cosI));      //Switching z e y to per piano da xz a xy (viceversa)
        float y = distanza * (sinI * sinAOPPlusTA);

        transform.position = new Vector3(x, y, z) + referenceBody.transform.position;
    }


    int orbitResolution = 50;
    List<Vector3> PuntiOrbitali = new List<Vector3>();
    
    private void OnDrawGizmos()
    {
        if (PuntiOrbitali.Count == 0)
        {
            if (referenceBody == null)
            {
                Debug.LogWarning($"Add a reference body to {gameObject.name}");
                return;
            }

            CalcolaCostanti();
            Vector3 pos = referenceBody.transform.position;
            float orbitFraction = 1f / orbitResolution;

            for (int i = 0; i < orbitResolution + 1; i++)
            {
                float EccentricAnomaly = i * orbitFraction * Math.TAU;

                float trueAnomaly = 2 * Mathf.Atan(AnomaliaVeraCostante * Mathf.Tan(EccentricAnomaly / 2));
                float distance = SemiAsseMaggiore * (1 - Eccentricita * Mathf.Cos(EccentricAnomaly));

                float cosAOPPlusTA = Mathf.Cos(ArgomentoDelPerielio + trueAnomaly);
                float sinAOPPlusTA = Mathf.Sin(ArgomentoDelPerielio + trueAnomaly);

                float x = distance * ((cosLOAN * cosAOPPlusTA) - (sinLOAN * sinAOPPlusTA * cosI));
                float z = distance * ((sinLOAN * cosAOPPlusTA) + (cosLOAN * sinAOPPlusTA * cosI));
                float y = distance * (sinI * sinAOPPlusTA);

                float meanAnomaly = EccentricAnomaly - Eccentricita * Mathf.Sin(EccentricAnomaly);

                PuntiOrbitali.Add(pos + new Vector3(x, y, z));
            }
        }
         Handles.DrawAAPolyLine(PuntiOrbitali.ToArray());
    }
    

}
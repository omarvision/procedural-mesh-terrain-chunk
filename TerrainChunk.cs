using UnityEngine;

/*
    E ---------- F
    |    A --------- B
    |    |       |   |
    |    |       |   |
    G ---|------ H   |
         C --------- D

    C is the origin point for the mesh
*/

public class TerrainChunk : MonoBehaviour
{
    #region --- helper ---
    private enum enumQuad
    {
        ABDC_back,
        EFHG_front,
        EFBA_top,
        GHCD_bottom,
        EAGC_left,
        FBDH_right,
    }
    private static class Point
    {
        public static Vector3 A = new Vector3(0, OFFSET * PCT, 0);
        public static Vector3 B = new Vector3(OFFSET * PCT, OFFSET * PCT, 0);
        public static Vector3 C = new Vector3(0, 0, 0);
        public static Vector3 D = new Vector3(OFFSET * PCT, 0, 0);
        public static Vector3 E = new Vector3(0, OFFSET * PCT, OFFSET * PCT);
        public static Vector3 F = new Vector3(OFFSET * PCT, OFFSET * PCT, OFFSET * PCT);
        public static Vector3 G = new Vector3(0, 0, OFFSET * PCT);
        public static Vector3 H = new Vector3(OFFSET * PCT, 0, OFFSET * PCT);
    }
    [System.Serializable]
    public struct TerrainParameters
    {
        [Range(0.1f,5.0f)]
        public float perlinscale;
        [Range(0.1f,1f)]
        public float terrainheight;
        [HideInInspector]
        public float xperlinoffset;
        [HideInInspector]
        public float zperlinoffset;
    }
    private class ChunkObject
    {
        public bool[,,] cubeobjects = null;
        private Vector3[] vertices = null;
        private int[] triangles = null;
        private Vector2[] uv = null;
        private int sizex = 0;
        private int sizey = 0;
        private int sizez = 0;
        private TerrainParameters[] tp = null;

        public ChunkObject(int x, int y, int z, TerrainParameters[] TP)
        {
            int arraysize = x * y * z * 36;
            vertices = new Vector3[arraysize];
            triangles = new int[arraysize];
            uv = new Vector2[arraysize];

            sizex = x;
            sizey = y;
            sizez = z;

            tp = new TerrainParameters[TP.Length];
            for (int i = 0; i < TP.Length; i++)
            {
                tp[i].perlinscale = TP[i].perlinscale;
                tp[i].terrainheight = TP[i].terrainheight;
            }

            cubeobjects = new bool[x, y, z];

            InitCubes();
        }
        private void InitCubes()
        {
            for (int pl = 0; pl < tp.Length; pl++)
            {
                tp[pl].xperlinoffset = Random.Range(0f, 1f);
                tp[pl].zperlinoffset = Random.Range(0f, 1f);
            }

            for (int z = 0; z < sizez; z++)
            {
                for (int y = 0; y < sizey; y++)
                {
                    for (int x = 0; x < sizex; x++)
                    {
                        //Note: 
                        //  perlin noise algorithm needs values between 0..1 to work. integer values produce the same result.

                        // Apply perlin noise in loops adds more variety
                        float height = 0f;
                        for (int pl = 0; pl < tp.Length; pl++)
                        {
                            float xval = ((float)x / (float)sizex) + tp[pl].xperlinoffset;
                            float zval = ((float)z / (float)sizez) + tp[pl].zperlinoffset;
                            float perlin = Mathf.PerlinNoise(xval * tp[pl].perlinscale, zval * tp[pl].perlinscale);
                            height += Mathf.RoundToInt(perlin * sizey) * tp[pl].terrainheight;
                        }

                        cubeobjects[x, y, z] = (y < height);
                    }
                }
            }
        }
        public void DefineMeshData()
        {
            bool show = false;

            //chunk
            for (int z = 0; z < sizez; z++)
            {
                for (int y = 0; y < sizey; y++)
                {
                    for (int x = 0; x < sizex; x++)
                    {
                        //cube at offset                        
                        foreach (enumQuad quad in System.Enum.GetValues(typeof(enumQuad)))
                        {
                            int v = (int)((x + (y * sizex) + (z * sizex * sizey)) * 36);
                            Vector3 p = new Vector3(x, y, z);

                            if (cubeobjects[x, y, z] == false)
                            {
                                //cube off
                                DefineQuad(quad, v, p, false);
                            }
                            else
                            {
                                //cube on, if neighbor on (quad off)  
                                switch (quad)
                                {
                                    case enumQuad.ABDC_back:
                                        show = (z > 0) ? !cubeobjects[x, y, z - 1] : true;
                                        break;
                                    case enumQuad.EFHG_front:
                                        show = (z < sizez - 1) ? !cubeobjects[x, y, z + 1] : true;
                                        break;
                                    case enumQuad.EFBA_top:
                                        show = (y < sizey - 1) ? !cubeobjects[x, y + 1, z] : true;
                                        break;
                                    case enumQuad.GHCD_bottom:
                                        show = (y > 0) ? !cubeobjects[x, y - 1, z] : true;
                                        break;
                                    case enumQuad.EAGC_left:
                                        show = (x > 0) ? !cubeobjects[x - 1, y, z] : true;
                                        break;
                                    case enumQuad.FBDH_right:
                                        show = (x < sizex - 1) ? !cubeobjects[x + 1, y, z] : true;
                                        break;
                                }
                                DefineQuad(quad, v, p, show);
                            }
                        }
                    }
                }
            }
        }
        public void SetMesh(ref Mesh m)
        {
            m.Clear();

            m.vertices = vertices;
            m.triangles = triangles;
            m.uv = uv;

            m.RecalculateNormals();
            m.RecalculateBounds();
            m.RecalculateTangents();
        }
        private void DefineQuad(enumQuad code, int v, Vector3 p, bool show)
        {
            switch (code)
            {
                case enumQuad.ABDC_back:
                    if (show == true)
                    {
                        vertices[v + 0] = p + Point.A; //ADC
                        vertices[v + 1] = p + Point.D;
                        vertices[v + 2] = p + Point.C;
                        vertices[v + 3] = p + Point.A; //ABD
                        vertices[v + 4] = p + Point.B;
                        vertices[v + 5] = p + Point.D;
                    }
                    else
                    {
                        vertices[v + 0] = Vector3.zero;
                        vertices[v + 1] = Vector3.zero;
                        vertices[v + 2] = Vector3.zero;
                        vertices[v + 3] = Vector3.zero;
                        vertices[v + 4] = Vector3.zero;
                        vertices[v + 5] = Vector3.zero;
                    }
                    triangles[v + 0] = v + 0;
                    triangles[v + 1] = v + 1;
                    triangles[v + 2] = v + 2;
                    triangles[v + 3] = v + 3;
                    triangles[v + 4] = v + 4;
                    triangles[v + 5] = v + 5;
                    uv[v + 0] = new Vector2(0, 1);
                    uv[v + 1] = new Vector2(1, 0);
                    uv[v + 2] = new Vector2(0, 0);
                    uv[v + 3] = new Vector2(0, 1);
                    uv[v + 4] = new Vector2(1, 1);
                    uv[v + 5] = new Vector2(1, 0);
                    break;
                case enumQuad.EFHG_front:
                    if (show == true)
                    {
                        vertices[v + 6] = p + Point.F; //FGH
                        vertices[v + 7] = p + Point.G;
                        vertices[v + 8] = p + Point.H;
                        vertices[v + 9] = p + Point.F; //FEG
                        vertices[v + 10] = p + Point.E;
                        vertices[v + 11] = p + Point.G;
                    }
                    else
                    {
                        vertices[v + 6] = Vector3.zero;
                        vertices[v + 7] = Vector3.zero;
                        vertices[v + 8] = Vector3.zero;
                        vertices[v + 9] = Vector3.zero;
                        vertices[v + 10] = Vector3.zero;
                        vertices[v + 11] = Vector3.zero;
                    }

                    triangles[v + 6] = v + 6;
                    triangles[v + 7] = v + 7;
                    triangles[v + 8] = v + 8;
                    triangles[v + 9] = v + 9;
                    triangles[v + 10] = v + 10;
                    triangles[v + 11] = v + 11;
                    uv[v + 6] = new Vector2(0, 1);
                    uv[v + 7] = new Vector2(1, 0);
                    uv[v + 8] = new Vector2(0, 0);
                    uv[v + 9] = new Vector2(0, 1);
                    uv[v + 10] = new Vector2(1, 1);
                    uv[v + 11] = new Vector2(1, 0);
                    break;
                case enumQuad.EFBA_top:
                    if (show == true)
                    {
                        vertices[v + 12] = p + Point.E; //EBA
                        vertices[v + 13] = p + Point.B;
                        vertices[v + 14] = p + Point.A;
                        vertices[v + 15] = p + Point.E; //EFB
                        vertices[v + 16] = p + Point.F;
                        vertices[v + 17] = p + Point.B;
                    }
                    else
                    {
                        vertices[v + 12] = Vector3.zero;
                        vertices[v + 13] = Vector3.zero;
                        vertices[v + 14] = Vector3.zero;
                        vertices[v + 15] = Vector3.zero;
                        vertices[v + 16] = Vector3.zero;
                        vertices[v + 17] = Vector3.zero;
                    }

                    triangles[v + 12] = v + 12;
                    triangles[v + 13] = v + 13;
                    triangles[v + 14] = v + 14;
                    triangles[v + 15] = v + 15;
                    triangles[v + 16] = v + 16;
                    triangles[v + 17] = v + 17;
                    uv[v + 12] = new Vector2(0, 1);
                    uv[v + 13] = new Vector2(1, 0);
                    uv[v + 14] = new Vector2(0, 0);
                    uv[v + 15] = new Vector2(0, 1);
                    uv[v + 16] = new Vector2(1, 1);
                    uv[v + 17] = new Vector2(1, 0);
                    break;
                case enumQuad.GHCD_bottom:
                    if (show == true)
                    {
                        vertices[v + 18] = p + Point.C; //CHG
                        vertices[v + 19] = p + Point.H;
                        vertices[v + 20] = p + Point.G;
                        vertices[v + 21] = p + Point.C; //CDH
                        vertices[v + 22] = p + Point.D;
                        vertices[v + 23] = p + Point.H;
                    }
                    else
                    {
                        vertices[v + 18] = Vector3.zero; //CHG
                        vertices[v + 19] = Vector3.zero;
                        vertices[v + 20] = Vector3.zero;
                        vertices[v + 21] = Vector3.zero; //CDH
                        vertices[v + 22] = Vector3.zero;
                        vertices[v + 23] = Vector3.zero;
                    }
                    triangles[v + 18] = v + 18;
                    triangles[v + 19] = v + 19;
                    triangles[v + 20] = v + 20;
                    triangles[v + 21] = v + 21;
                    triangles[v + 22] = v + 22;
                    triangles[v + 23] = v + 23;
                    uv[v + 18] = new Vector2(0, 1);
                    uv[v + 19] = new Vector2(1, 0);
                    uv[v + 20] = new Vector2(0, 0);
                    uv[v + 21] = new Vector2(0, 1);
                    uv[v + 22] = new Vector2(1, 1);
                    uv[v + 23] = new Vector2(1, 0);
                    break;
                case enumQuad.EAGC_left:
                    if (show == true)
                    {
                        vertices[v + 24] = p + Point.E; //ECG
                        vertices[v + 25] = p + Point.C;
                        vertices[v + 26] = p + Point.G;
                        vertices[v + 27] = p + Point.E; //EAC
                        vertices[v + 28] = p + Point.A;
                        vertices[v + 29] = p + Point.C;
                    }
                    else
                    {
                        vertices[v + 24] = Vector3.zero;
                        vertices[v + 25] = Vector3.zero;
                        vertices[v + 26] = Vector3.zero;
                        vertices[v + 27] = Vector3.zero;
                        vertices[v + 28] = Vector3.zero;
                        vertices[v + 29] = Vector3.zero;
                    }
                    triangles[v + 24] = v + 24;
                    triangles[v + 25] = v + 25;
                    triangles[v + 26] = v + 26;
                    triangles[v + 27] = v + 27;
                    triangles[v + 28] = v + 28;
                    triangles[v + 29] = v + 29;
                    uv[v + 24] = new Vector2(0, 1);
                    uv[v + 25] = new Vector2(1, 0);
                    uv[v + 26] = new Vector2(0, 0);
                    uv[v + 27] = new Vector2(0, 1);
                    uv[v + 28] = new Vector2(1, 1);
                    uv[v + 29] = new Vector2(1, 0);
                    break;
                case enumQuad.FBDH_right:
                    if (show == true)
                    {
                        vertices[v + 30] = p + Point.B; //BHD
                        vertices[v + 31] = p + Point.H;
                        vertices[v + 32] = p + Point.D;
                        vertices[v + 33] = p + Point.B; //BFH
                        vertices[v + 34] = p + Point.F;
                        vertices[v + 35] = p + Point.H;
                    }
                    else
                    {
                        vertices[v + 30] = Vector3.zero;
                        vertices[v + 31] = Vector3.zero;
                        vertices[v + 32] = Vector3.zero;
                        vertices[v + 33] = Vector3.zero;
                        vertices[v + 34] = Vector3.zero;
                        vertices[v + 35] = Vector3.zero;
                    }
                    triangles[v + 30] = v + 30;
                    triangles[v + 31] = v + 31;
                    triangles[v + 32] = v + 32;
                    triangles[v + 33] = v + 33;
                    triangles[v + 34] = v + 34;
                    triangles[v + 35] = v + 35;
                    uv[v + 30] = new Vector2(0, 1);
                    uv[v + 31] = new Vector2(1, 0);
                    uv[v + 32] = new Vector2(0, 0);
                    uv[v + 33] = new Vector2(0, 1);
                    uv[v + 34] = new Vector2(1, 1);
                    uv[v + 35] = new Vector2(1, 0);
                    break;
            }
        }
    }
    #endregion

    private const float OFFSET = 1.0f;
    private const float PCT = 1.0f;

    public Material material = null;
    public Vector3 chunksize = new Vector3();
    public TerrainParameters[] PerlinLoops = null;
    private ChunkObject chunkobject = null;

    private void Start()
    {
        //add mesh filter, mesh renderer
        Mesh mesh = GetMesh();

        //define mesh data
        chunkobject = new ChunkObject((int)chunksize.x, (int)chunksize.y, (int)chunksize.z, PerlinLoops);
        chunkobject.DefineMeshData();
        
        //set mesh data
        chunkobject.SetMesh(ref mesh);

        //add mesh collider
        SetCollider(ref mesh);
    }
    private Mesh GetMesh()
    {
        Mesh m = null;
        MeshFilter mf = this.gameObject.AddComponent<MeshFilter>();         //holds mesh data
        MeshRenderer mr = this.gameObject.AddComponent<MeshRenderer>();     //renders mesh data

        mr.material = material;

        if (Application.isEditor == true)
        {
            m = mf.sharedMesh;
            if (m == null)
            {
                mf.sharedMesh = new Mesh();
                m = mf.sharedMesh;
            }
        }
        else
        {
            m = mf.mesh;
            if (m == null)
            {
                mf.mesh = new Mesh();
                m = mf.mesh;
            }
        }

        m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.name = "TerrainChunk";

        return m;
    }
    private void SetCollider(ref Mesh m)
    {
        MeshCollider mc = this.gameObject.AddComponent<MeshCollider>();
        mc.sharedMesh = m;
    }
}

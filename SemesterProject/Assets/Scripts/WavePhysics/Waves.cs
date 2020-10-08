using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Waves : MonoBehaviour
{

    public int dimension = 10;
    public Octave[] octaves;
    public float UVScale;

    protected MeshFilter meshFilter;
    protected Mesh mesh;

    // Start is called before the first frame update
    void Start()
    {
        //Mesh Setup
        mesh = new Mesh();
        //The mesh name is the same as the object name
        mesh.name = gameObject.name;

        //We specify the vertices and triangles and generate them via custom methods
        mesh.vertices = generateVerts();
        mesh.triangles = generateTries();
        mesh.uv = generateUVs();
        //Then recalculate the bounds afterwards
        mesh.RecalculateBounds();
        //Recalculates the normals to allow for shadows to be present
        mesh.RecalculateNormals();

        //Create the meshfilter in order to render it
        meshFilter = gameObject.AddComponent<MeshFilter>();
        //Set the newly created mesh to the meshfilter
        meshFilter.mesh = mesh;
    }

    public float GetHeight(Vector3 position)
    {
        //scale factor and position in local space
        var scale = new Vector3(1 / transform.lossyScale.x, 0, 1 / transform.lossyScale.z);
        var localPos = Vector3.Scale((position - transform.position), scale);

        //get edge points
        var p1 = new Vector3(Mathf.Floor(localPos.x), 0, Mathf.Floor(localPos.z));
        var p2 = new Vector3(Mathf.Floor(localPos.x), 0, Mathf.Ceil(localPos.z));
        var p3 = new Vector3(Mathf.Ceil(localPos.x), 0, Mathf.Floor(localPos.z));
        var p4 = new Vector3(Mathf.Ceil(localPos.x), 0, Mathf.Ceil(localPos.z));

        //clamp if the position is outside the plane
        p1.x = Mathf.Clamp(p1.x, 0, dimension);
        p1.z = Mathf.Clamp(p1.z, 0, dimension);
        p2.x = Mathf.Clamp(p2.x, 0, dimension);
        p2.z = Mathf.Clamp(p2.z, 0, dimension);
        p3.x = Mathf.Clamp(p3.x, 0, dimension);
        p3.z = Mathf.Clamp(p3.z, 0, dimension);
        p4.x = Mathf.Clamp(p4.x, 0, dimension);
        p4.z = Mathf.Clamp(p4.z, 0, dimension);

        //get the max distance to one of the edges and take that to compute max - dist
        var max = Mathf.Max(Vector3.Distance(p1, localPos), Vector3.Distance(p2, localPos), Vector3.Distance(p3, localPos), Vector3.Distance(p4, localPos) + Mathf.Epsilon);
        var dist = (max - Vector3.Distance(p1, localPos))
                 + (max - Vector3.Distance(p2, localPos))
                 + (max - Vector3.Distance(p3, localPos))
                 + (max - Vector3.Distance(p4, localPos) + Mathf.Epsilon);
        //weighted sum
        var height = mesh.vertices[index(p1.x, p1.z)].y * (max - Vector3.Distance(p1, localPos))
                   + mesh.vertices[index(p2.x, p2.z)].y * (max - Vector3.Distance(p2, localPos))
                   + mesh.vertices[index(p3.x, p3.z)].y * (max - Vector3.Distance(p3, localPos))
                   + mesh.vertices[index(p4.x, p4.z)].y * (max - Vector3.Distance(p4, localPos));

        //scale
        return height * transform.lossyScale.y / dist;

    }

    private Vector3[] generateVerts()
    {
        //Creating a vector3 array with the dimension + 1: example if dimension is 10 it make the grid 11x11
        var verts = new Vector3[(dimension + 1) * (dimension + 1)];

        //Equaly distributes the verts
        for (int x = 0; x <= dimension; x++)
            for (int z = 0; z <= dimension; z++)
                verts[index(x, z)] = new Vector3(x, 0, z);

        return verts;
    }

    private int index(int x, int z)
    {
        //The logic here is to index each position in the array
        //So if x=0 && z=0 then the index is 0, if x=0 && z=9 then the index is 9, if x=1 and z=0 then the index is 12
        return x * (dimension + 1) + z;
    }

    private int index(float x, float z)
    {
        return index((int)x, (int)z);
    }

    private int[] generateTries()
    {
        //This line makes each square in the mesh into 2 triangles each with 3 vertices so 6 in total
        var tries = new int[mesh.vertices.Length * 6];

        //two triangles are one tile or square. This step can be confusing so watch https://www.youtube.com/watch?v=_Ij24zRI9J0&t=12s at 5:30
        for (int x = 0; x < dimension; x++)
        {
            for (int z = 0; z < dimension; z++)
            {
                tries[index(x, z) * 6 + 0] = index(x, z);
                tries[index(x, z) * 6 + 1] = index(x + 1, z + 1);
                tries[index(x, z) * 6 + 2] = index(x + 1, z);
                tries[index(x, z) * 6 + 3] = index(x, z);
                tries[index(x, z) * 6 + 4] = index(x, z + 1);
                tries[index(x, z) * 6 + 5] = index(x + 1, z + 1);
            }
        }

        return tries;
    }

    private Vector2[] generateUVs()
    {
        var uvs = new Vector2[mesh.vertices.Length];

        //Always set one uv over n tiles than flip the uv and set it again
        for (int x = 0; x <= dimension; x++)
        {
            for (int z = 0; z <= dimension; z++)
            {
                var vec = new Vector2((x / UVScale) % 2, (z / UVScale) % 2);
                uvs[index(x, z)] = new Vector2(vec.x <= 1 ? vec.x : 2 - vec.x, vec.y <= 1 ? vec.y : 2 - vec.y);
            }
        }

        return uvs;
    }

    // Update is called once per frame
    void Update()
    {
        //We first grab our vertices in the mesh.vertices and store them in verts
        var verts = mesh.vertices;
        //We itterate through all the dimensions and we set a height value for our vertices
        for (int x = 0; x <= dimension; x++)
        {
            for (int z = 0; z <= dimension; z++)
            {
                //This is the height value "y"
                var y = 0f;
                //We itterate through all of our octaves...
                for (int o = 0; o < octaves.Length; o++)
                {
                    if (octaves[o].alternate)
                    {
                        //...And make an alternate animation
                        //We make a perl noise vectore which is basically just x*z but multiplied by a scale factor and then divided by the dimension of the complete plane
                        //and finally multiplied by pi*2
                        var perl = Mathf.PerlinNoise((x * octaves[o].scale.x) / dimension, (z * octaves[o].scale.y) / dimension) * Mathf.PI * 2f;
                        //We make a cosinus wave and pass it the time and multiply that by the speeds magnitude and multiply the result of that by the height
                        y += Mathf.Cos(perl + octaves[o].speed.magnitude * Time.time) * octaves[o].height;
                    }
                    else
                    {
                        //This section is a bit tough so see 9:30 of https://www.youtube.com/watch?v=_Ij24zRI9J0&t=12s
                        //The idea here is that it's nearly the same as above but instead of using the cosinus wave we just use the perlin noise and multiply it by the height
                        //We then move the time value that was previously in the cosinus calculation into the perlin noise equation
                        //At the end of the equation we subtract 0.5f to normalize the wave so it alternates around 0 from -0.5 to 0.5 instead of alternating between 0 and 1
                        //The time value ensures that the wave moves forward for this you might be able to think back at 4th semester and remember how sound waves work
                        var perl = Mathf.PerlinNoise((x * octaves[o].scale.x + Time.time * octaves[o].speed.x) / dimension, (z * octaves[o].scale.y + Time.time * octaves[o].speed.y) / dimension) - 0.5f;
                        y += perl * octaves[o].height;
                    }
                }
                verts[index(x, z)] = new Vector3(x, y, z);
            }
        }

        //Here we put it back together
        mesh.vertices = verts;
        //Recalculates the normals to allow for shadows to be present
        mesh.RecalculateNormals();
    }

    [Serializable]
    public struct Octave
    {
        public Vector2 speed;
        public Vector2 scale;
        public float height;
        public bool alternate;
    }
}

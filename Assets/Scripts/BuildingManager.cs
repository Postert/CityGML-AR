using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;



/// <summary>
/// Deserializes a CityGML file and manages and displays the derived Building
/// </summary>
public class BuildingManager : MonoBehaviour
{
    /// <value>DatabaseService to perform to query the local SQLite database.</value>
    public DatabaseService DatabaseService { get; set; }

    /// <value>The edge length of the square <c>BoundingBox</c> in meters.</value>
    private const int boundingBoxDimension = 300;

    /// <value>Creates the <c>Building</c>'s geometry in the Unity3D scene.</value>
    private BuildingMeshFactory BuildingMeshFactory;

    /// <value>Contains the <c>Building</c> meshes the <seealso cref="BuildingMeshFactory"/> creates.</value>
    private MeshFilter MeshFilter;

    /// <summary>
    /// Instantiates a <c>BuildingMeshFactory</c> and a <c>MeshFilter</c>.
    /// </summary>
    private void Awake()
    {
        BuildingMeshFactory = new BuildingMeshFactory();
        MeshFilter = gameObject.GetComponent<MeshFilter>();
    }




    /// <summary>
    /// Creates a mesh with all <c>Building</c>s arround the anchor point.
    /// </summary>
    /// <param name="targetRealWorldCoordinates"></param>
    /// <returns></returns>
    public Dictionary<string, Building> CreateBuildingMeshAroundAnchorPoint(double3 targetRealWorldCoordinates)
    {
        //Debug.Log(MyTimer.GetSecondsSiceStartAsString() + ": BuildingManager determining BoundinBox around detected target");

        /// Calculate the BoundingBox arround the given <c>anchorPoint</c> with the dimension of the predefined <c>boundingBoxDimension</c>. 
        double3 lowerLeftCorner = new double3(targetRealWorldCoordinates.x - (double)(0.5 * boundingBoxDimension), targetRealWorldCoordinates.y - (double)(0.5 * boundingBoxDimension), 0);
        double3 upperRightCorner = new double3(targetRealWorldCoordinates.x + (double)(0.5 * boundingBoxDimension), targetRealWorldCoordinates.y + (double)(0.5 * boundingBoxDimension), 0);
        BoundingBox boundingBoxAroundTarget = new BoundingBox(lowerLeftCorner, upperRightCorner);

        //Debug.Log(MyTimer.GetSecondsSiceStartAsString() + ": Querying buildings within " + boundingBoxAroundTarget.ToString());

        /// Contains all <c>Buidling</c>s to be rendered by the <c>BuildingMeshFactory</c>
        List<Building> BuildingRenderingList = new List<Building>();

        /// Query and instantiation to get the <c>Building</c>s from the database within the <c>BoundingBox</c>
        Dictionary<string, Building> buildingsWithinBoundingBox = DatabaseService.GetBuildings(boundingBoxAroundTarget);
        BuildingRenderingList.AddRange(buildingsWithinBoundingBox.Values);

        /// Creation of a single mesh with all <c>Building</c>s included
        MeshFilter.mesh = BuildingMeshFactory.CreateMesh(BuildingRenderingList, targetRealWorldCoordinates, gameObject);

        //Debug.Log(MyTimer.GetSecondsSiceStartAsString() + ": Building Mesh was created");

        return buildingsWithinBoundingBox;
    }


    /// <summary>
    /// Updates the position of the mesh with all <c>Building</c>s associated to the <c>BuildingManager</c>'s GameObjects in the Unity3D scene.
    /// </summary>
    /// <remarks>Typically used to compensate drift repeated target detection.</remarks>
    /// <param name="anchorCoordinates">Target Unity3D coordinates to place the <c>AnnotationManager</c>'s GameObject</param>
    /// <param name="anchorOrientation">Target orientation of the <c>AnnotationManager</c>'s GameObject</param>
    public void UpdateMeshPositionInUnity(Vector3 anchorCoordinates, Quaternion anchorOrientation)
    {
        MeshFilter.transform.position = anchorCoordinates;
        //MeshFilter.transform.rotation = trackedImageRotation;
    }
}












/// <summary>
/// Derives a single mesh from (multiple) <c>Building</c> objects
/// </summary>
public class BuildingMeshFactory
{
    /// <summary>
    /// Returns a new <c>Surface</c> with specific anchor related Unity3D coordinates.
    /// </summary>
    /// <param name="surfaceWithUTMCoordinates"><c>Surface</c> with ETRS/UTM coordiante points</param>
    /// <param name="utmAnchorPoint">ETRS/UTM coordinate for anchoring</param>
    /// <returns><<c>Surface</c> with Unity3D coordinates</returns>
    private Surface GetUnityCoordinates(Surface surfaceWithUTMCoordinates, double3 utmAnchorPoint)
    {
        List<double3> unityCoordinates = new List<double3>();

        try
        {
            foreach (double3 point in surfaceWithUTMCoordinates.Polygon)
            {
                unityCoordinates.Add((float3)CoordinateTransformer.GetUnityCoordinates(point, utmAnchorPoint));
            }

            return new Surface(surfaceWithUTMCoordinates.CityGMLID, surfaceWithUTMCoordinates.Type, unityCoordinates);
        }
        catch (Exception e)
        {
            throw new Exception("Surface UTM coordinates cannot be transformed into Unity coordinates:\n" + e);
        }
    }


    /// <summary>
    /// Return new <c>Surface</c>s with specific anchor related Unity3D coodrinates.
    /// </summary>
    /// <param name="building"><c>Building</c> with <c>Surface</c>s with ETRS/UTM coordiante points</param>
    /// <param name="utmAnchorPoint">ETRS/UTM coordinate for anchoring</param>
    /// <returns><c>Surface</c>s as values assiciated CityGML id as key</returns>
    private Dictionary<string, Surface> GetUnityCoordinates(Building building, double3 utmAnchorPoint)
    {
        Dictionary<string, Surface> surfacesWithUnityCoordinates = new Dictionary<string, Surface>();

        foreach (KeyValuePair<string, Surface> uniqueKeyAssociatedSurface in building.ExteriorSurfaces)
        {
            surfacesWithUnityCoordinates.Add(uniqueKeyAssociatedSurface.Key, GetUnityCoordinates(uniqueKeyAssociatedSurface.Value, utmAnchorPoint));
        }

        return surfacesWithUnityCoordinates;
    }


    /// <summary>
    /// Creates meshes for each building contained in the class owned building list "Buildings".
    /// </summary>
    /// <remarks>Assumes that the polygons are planar, but oriented arbitrarily in 3D space.</remarks>
    /// <param name="building"><c>Building</c> with <c>Surface</c>s with ETRS/UTM coordiante points</param>
    /// <param name="utmAnchorPoint">ETRS/UTM coordinate for anchoring</param>
    /// <param name="gameObjectBuildingManger">Parent node in the Unity3D hierarchie</param>
    /// <returns></returns>
    private Mesh GetBuildingMesh(Building building, double3 utmAnchorPoint, GameObject gameObjectBuildingManger)
    {
        try
        {
            Dictionary<string, Surface> surfacesWithUnityCoordinates = GetUnityCoordinates(building, utmAnchorPoint);

            /// The points must be arranged clockwise from the point of view of the desired viewing direction. 

            /// Contains mesh for each <c>Surface</c> of the <c>Building</c>
            List<Mesh> surfaceMeshList = new List<Mesh>();

            foreach (Surface surface in surfacesWithUnityCoordinates.Values)
            {
                //Debug.Log("SurfacesCityGMLID: " + surface.CityGMLID +  ", " + surface.Type);

                Mesh surfaceMesh = new Mesh();

                /// Set the <c>Surface</c>s points with Unity3D (!) coordinates as the mesh vertices
                Vector3[] surfaceVertices = new Vector3[surface.Polygon.Count];
                try
                {
                    for (int i = 0; i < surface.Polygon.Count; i++)
                    {
                        surfaceVertices[i] = ((float3)surface.Polygon[i]);
                    }
                    surfaceMesh.vertices = surfaceVertices;
                }
                catch (InvalidCastException e)
                {
                    Debug.LogError("Cannot determin Vector3 from double3\nAffected Building:" + building.CityGMLID + "\nBuildung will not be displayed\n" + e);
                    continue;
                }

                /// Calculate triangles with copied original ETRS/UTM vertices
                surfaceMesh.triangles = PolygonTriangulator.GetTriangles(surfaceVertices, surface);
                surfaceMesh.RecalculateNormals();

                surfaceMeshList.Add(surfaceMesh);
            }

            return CombineMeshes(surfaceMeshList, gameObjectBuildingManger);
        }
        catch (ArgumentException e)
        {
            Debug.LogError("Error when converting the projected UTM coordinates into coordinates of the Unity coordinate system: " + e);
            return null;
        }
    }

    /// <summary>
    /// Combines several meshes into one mesh
    /// </summary>
    /// <param name="meshes">Meshes to be combined</param>
    /// <returns>One single mesh</returns>
    private Mesh CombineMeshes(List<Mesh> meshes, GameObject gameObjectBuildingManger)
    {
        CombineInstance[] combine = new CombineInstance[meshes.Count];

        for (int i = 0; i < meshes.Count; i++)
        {
            combine[i].mesh = meshes[i];
            combine[i].transform = gameObjectBuildingManger.transform.localToWorldMatrix;
        }

        Mesh mesh = new Mesh
        {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };
        mesh.CombineMeshes(combine);
        return mesh;
    }


    /// <summary>
    /// Generates one single mesh from the <c>Building</c>s.
    /// </summary>
    /// <param name="buildings"><c>Buildings</c> to derive the mesh from</param>
    /// <param name="utmAnchorPoint">ETRS/UTM coordinate for anchoring</param>
    /// <param name="gameObjectBuildingManger">Parent node in the Unity3D hierarchie</param>
    /// <returns>One single mesh containing the exterior geometries of all <c>Building</c>s</returns>
    public Mesh CreateMesh(List<Building> buildings, double3 utmAnchorPoint, GameObject gameObjectBuildingManger)
    {
        List<Mesh> buildingMeshes = new List<Mesh>();

        foreach (Building building in buildings)
        {
            buildingMeshes.Add(GetBuildingMesh(building, utmAnchorPoint, gameObjectBuildingManger));
        }

        Mesh allBuildingMeshes = new Mesh
        {
            /// Change the index buffer format to render a mesh with a huge amount of vertices
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };
        allBuildingMeshes = CombineMeshes(buildingMeshes, gameObjectBuildingManger);
        allBuildingMeshes.Optimize();
        allBuildingMeshes.RecalculateNormals();

        return allBuildingMeshes;
    }
}
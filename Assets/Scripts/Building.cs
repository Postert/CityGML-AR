using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


/// <summary>
/// Contains the CityGML id, the <c>Polygon</c> points spanning the <c>Surface</c>, and the <c>SurfaceType</c> and calculates the its normal vector.
/// </summary>
public class Surface
{
    /// <value>Unique ID provided by the source dataset</value>
    public string CityGMLID { get; private set; } = null;

    /// <value>Contains the polygon points spanning the <c>Surface</c>. The polygon course is not closed, the first and last point of the list do not equal. The last edge of the polygon from the first point to the last point in the list is only modeled implicitly.</value>
    public List<double3> Polygon { get; private set; } = new List<double3>();

    /// Specifies the type of the <c>Surface</c>
    public SurfaceType Type { get; set; }

    /// <summary>
    /// Creates a Surface with a given unique CityGML id.
    /// </summary>
    /// <param name="surfaceCityGMLID">Unique CityGML id</param>
    public Surface(string surfaceCityGMLID) =>
        (CityGMLID) = (surfaceCityGMLID);

    /// <summary>
    /// Creates a Surface with a given unique CityGML id and the <c>SurfaceType</c>.
    /// </summary>
    /// <param name="surfaceCityGMLID">Unique CityGML id</param>
    /// <param name="surfaceType">Type of the <c>Surface</c></param>
    public Surface(string surfaceCityGMLID, SurfaceType surfaceType) : this(surfaceCityGMLID) =>
        (Type) = (surfaceType);

    /// <summary>
    /// Creates a Surface with a given unique CityGML id, the <c>SurfaceType</c> and the polygon points spanning the <c>Surface</c>.
    /// </summary>
    /// <param name="surfaceCityGMLID">Unique CityGML id</param>
    /// <param name="surfaceType">Type of the <c>Surface</param>
    /// <param name="polygon">List of polygon points spanning the <c>Surface</c></param>
    public Surface(string surfaceCityGMLID, SurfaceType surfaceType, List<double3> polygon) : this(surfaceCityGMLID, surfaceType) =>
        (Polygon) = (polygon);


    /// <summary>
    /// Calculates a normal vector.
    /// </summary>
    /// <remarks>Given an exterior <c>Surface</c> (with points specified counter-clockwise), the vector points in viewing direction.</remarks>
    /// <returns>Normal vector of the <c>Surface</c>'s polygon</returns>
    public Vector3 GetSurfaceNormal()
    {
        /// Calculate the surface normal of the planar polygon to determine its orientation in 3D space.

        /// In order to interpret concave polygons correctly as well, the normal vector is determined by cross producting all neighboring polygon points.
        Vector3 firstTraverseNormal = Vector3.Cross((float3)(Polygon[2] - Polygon[0]), (float3)(Polygon[1] - Polygon[0]));
        firstTraverseNormal.Normalize();

        Vector3 inverseFirstTraverseNormal = -1 * firstTraverseNormal;

        int traverseNormalCounter = 0, inverseTraverseNormalCounter = 0;

        for (int i = 0; i < Polygon.Count - 2; i++)
        {
            Vector3 currentTraverseNormal = Vector3.Cross((float3)(Polygon[i + 1] - Polygon[i]), (float3)(Polygon[i + 2] - Polygon[i]));
            currentTraverseNormal.Normalize();

            if (currentTraverseNormal.Equals(firstTraverseNormal))
            {
                traverseNormalCounter++;
            }
            else if (currentTraverseNormal.Equals(inverseFirstTraverseNormal))
            {
                inverseTraverseNormalCounter++;
            }
            else
            {
                /// Warning if a surface is not planar. This is often the case due to rounding.
                Debug.LogWarning(
                    "Invalid polygon: Points do not lie in one plane\nSurface:\t\t\t" + firstTraverseNormal + "\ninverseFirstTraverseNormal:\t" + inverseFirstTraverseNormal + "\ncurrentTraverseNormal:\t\t" + currentTraverseNormal + "\ntraverseNormalCounter:\t\t" + traverseNormalCounter + "\ninverseTraverseNormalCounter:\t" + inverseTraverseNormalCounter + "\n\n" + this.ToString() + "\nThis may be due to the rounding of coordinate values. A correct surface normal calculation cannot be guaranteed, but an incorrect determination of the normal direction is unlikely.");
            }
        }

        Vector3 surfaceNormal = (traverseNormalCounter > inverseTraverseNormalCounter) ? firstTraverseNormal : -1 * firstTraverseNormal;

        return surfaceNormal;
    }


    public override string ToString()
    {
        string surfaceToString =
            "CityGML Surface ID: " + CityGMLID + "\n" +
            "SurfaceType: " + Type + "\n" +
            "Polygonpunkte:\n";

        foreach (double3 point in Polygon)
        {
            surfaceToString += "   " + point.ToString() + "\n";
        }

        surfaceToString += "\n";

        return surfaceToString;
    }
}

/// <summary>
/// Types of a <c>SUrface</c> described in the CityGML specifications and an additional 'UNDEFINED' type for <c>Surface</c>s without explicit declaration in the source dataset.
/// </summary>
public enum SurfaceType
{
    GroundSurface, WallSurface, RoofSurface, UNDEFINED
}

/// <summary>
/// Contains the the unique CityGML id provided by the given dataset and further selected properties and manages the geometries.
/// </summary>
public class Building
{
    /// <value>Unique CityGML id provided by the source dataset.</value>
    public string CityGMLID { get; private set; }

    /// <value>Exterior surfaces of the building as the values and the associated CityGML ids as the keys.</value>
    public Dictionary<string, Surface> ExteriorSurfaces { get; private set; } = new Dictionary<string, Surface>();

    /// <value>Heigh that describes the diffenrece between the terrain and the highest building part. For further information also read the <see href="http://wiki.quality.sig3d.org/index.php/Handbuch_f%C3%BCr_die_Modellierung_von_3D_Objekten_-_Teil_2:_Modellierung_Geb%C3%A4ude_%28LOD1,_LOD2_und_LOD3%29#H.C3.B6henangaben">SIG3D Documentation</see></value>
    public float MeasuredHeight { get; set; }

    /// BoundingBox enclosing the <c>Buidling</c>. 
    public BoundingBox BoundingBox = new BoundingBox();


    /// <summary>
    /// Creates a <c>Building</c> and initailizes its CityGML id.
    /// </summary>
    /// <param name="cityGMLID">Unique CityGML id provided by the source dataset.</param>
    public Building(string cityGMLID)
    {
        CityGMLID = cityGMLID;
    }

    /// <summary>
    /// Creates a <c>Building</c> and initializes its CityGML id and its measuredHeight.
    /// </summary>
    /// <param name="cityGMLID">Unique CityGML id provided by the source dataset.</param>
    /// <param name="measuredHeight">Mesaured Height of the <c>Building</c> provided by the source dataset. For futher information also read <seealso cref="MeasuredHeight"></seealso></param>
    public Building(string cityGMLID, float measuredHeight) : this(cityGMLID)
    {
        MeasuredHeight = measuredHeight;
    }


    #region Surfaces

    /// <summary>
    /// Replaces the <c>Building</c>'s exterior surfaces and updates the <c>Building</c>'s <c>BoundingBox</c>.
    /// </summary>
    /// <param name="exteriorSurfaces">Exterior surfaces with their associated CityGML ids as the key</param>
    public void SetSurfaces(Dictionary<string, Surface> exteriorSurfaces)
    {
        ExteriorSurfaces.Clear();
        BoundingBox.Clear();
        AddSurface(exteriorSurfaces);
    }

    /// <summary>
    /// Adds the contained <c>Surface</c>s to the <c>Building</c> and updates the <c>BoundingBox</c> the argument is not null.
    /// </summary>
    /// <param name="exteriorSurfaces">Exterior surfaces with their associated CityGML ids as the key</param>
    public void AddSurface(Dictionary<string, Surface> exteriorSurfaces)
    {
        if (exteriorSurfaces == null)
        {
            return;
        }

        foreach (KeyValuePair<string, Surface> uniqueKeyAssociatedSurface in exteriorSurfaces)
        {
            AddSurface(uniqueKeyAssociatedSurface.Key, uniqueKeyAssociatedSurface.Value);
        }
    }

    /// <summary>
    /// Adds the <c>Surface</c> to the <c>Building</c> and updates the <c>BoundingBox</c> the arguments are not null.
    /// </summary>
    /// <param name="uniqueSurfaceID">Unique CityGML íd associated to the <c>Surface</c> provided by the source dataset</param>
    /// <param name="surface">Exterior surface</param>
    public void AddSurface(string uniqueSurfaceID, Surface surface)
    {
        ExteriorSurfaces.Add(uniqueSurfaceID, surface);
        BoundingBox.UpdateWith(surface);
    }

    /// <summary>
    /// Adds the ETRS/UTM surface points to the <c>Surface</c> the given CityGML id ís associated with. If the <c>Surface</c> has not been added to the <seealso cref="ExteriorSurfaces"/>, a new <c>Surface</c> is created and added. The <c>BoudingBox</c> is not updated.
    /// </summary>
    /// <param name="uniqueSurfaceID">CityGML of the <c>Surface</c> the points are to be added</param>
    /// <param name="surfaceType">Type of the associated <c>Surface</c></param>
    /// <param name="surfacePoints">Points to be added to the <c>Surface</c></param>
    public void AddSurfacePoint(string uniqueSurfaceID, SurfaceType surfaceType, List<double3> surfacePoints)
    {
        if (!ExteriorSurfaces.TryGetValue(uniqueSurfaceID, out Surface surface))
        {
            ExteriorSurfaces.Add(uniqueSurfaceID, new Surface(uniqueSurfaceID, surfaceType, surfacePoints));
        }
        else
        {
            surface.Polygon.AddRange(surfacePoints);
        }
    }

    /// <summary>
    /// Adds the ETRS/UTM surface point to the <c>Surface</c> the given CityGML id ís associated with. If the <c>Surface</c> has not been added to the <seealso cref="ExteriorSurfaces"/>, a new <c>Surface</c> is created and added. The <c>BoudingBox</c> is not updated.
    /// </summary>
    /// <param name="uniqueSurfaceID">CityGML of the <c>Surface</c> the points are to be added</param>
    /// <param name="surfaceType">Type of the associated <c>Surface</c></param>
    /// <param name="surfacePoint">Points to be added to the <c>Surface</c></param>
    public void AddSurfacePoint(string uniqueSurfaceID, SurfaceType surfaceType, double3 surfacePoint)
    {
        List<double3> surfacePointList = new List<double3>();
        surfacePointList.Add(surfacePoint);
        AddSurfacePoint(uniqueSurfaceID, surfaceType, surfacePointList);
    }

    /// <summary>
    /// Returns all <seealso cref="ExteriorSurfaces"/> with the selected type.
    /// </summary>
    /// <param name="surfaceType">Type to filter the associated <c>Surface</c>s</param>
    /// <returns>Associated <c>Surface</c>s with the given type</returns>
    public Dictionary<string, Surface> GetSurfaces(SurfaceType surfaceType)
    {
        Dictionary<string, Surface> surfacesOfSelectedType = new Dictionary<string, Surface>();

        foreach (KeyValuePair<string, Surface> surfaceDictionaryEntry in ExteriorSurfaces)
        {
            if (surfaceDictionaryEntry.Value.Type == surfaceType)
            {
                surfacesOfSelectedType.Add(surfaceDictionaryEntry.Key, surfaceDictionaryEntry.Value);
            }
        }

        return surfacesOfSelectedType;
    }

    #endregion


    public override string ToString()
    {
        string buildingToString = null;
        buildingToString += "\nCityGML building ID: " + CityGMLID
            + "\nMeasuredHeight: " + MeasuredHeight
            + "\nBuilding-BoudingBox: " + BoundingBox.ToString();

        buildingToString += "\n\nThe Building consisting of the following " + ExteriorSurfaces.Count + " Surfaces:\n";


        int counter = 1;
        foreach (KeyValuePair<string, Surface> uniqueKeyAssociatedSurface in ExteriorSurfaces)
        {
            buildingToString += "\n " + counter++ + ". Surface:\n" + "uniqueInternalSurfaceID: " + uniqueKeyAssociatedSurface.Key + "\n" + uniqueKeyAssociatedSurface.Value.ToString();
        }

        buildingToString += "\n\n";

        return buildingToString;
    }
}
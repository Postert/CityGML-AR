using System;
using System.Collections.Generic;
using Unity.Mathematics;

/// <summary>
/// Contains the 3D points of the cuboid bounding box that best enclose the data set and the derived center point on the projected x-y plane.
/// </summary>
public class BoundingBox
{
    public double3? ButtomLowerLeftCorner { get; private set; }
    public double3? TopUpperRightCorner { get; private set; }

    /// <summary>
    /// Resets the <c>BoudingBox</c> by deleting the spanning points. 
    /// <remarks>Existing information of the <c>BoundingBox</c> cannot be restored.</remarks>
    /// </summary>
    public void Clear()
    {
        this.ButtomLowerLeftCorner = null;
        this.TopUpperRightCorner = null;
    }

    /// <summary>
    /// Creates a <c>BoudingBox</c> without initialization.
    /// </summary>
    public BoundingBox()
    {
        this.ButtomLowerLeftCorner = null;
        this.TopUpperRightCorner = null;
    }

    /// <summary>
    /// Creates a <c>BoundingBox</c> with initialization.
    /// </summary>
    /// <param name="buttomLowerLeftCorner"></param>
    /// <param name="topUpperRightCorner"></param>
    public BoundingBox(double3 buttomLowerLeftCorner, double3 topUpperRightCorner)
    {
        this.UpdateWith(new List<double3> { buttomLowerLeftCorner, topUpperRightCorner});
    }


    /// <summary>
    /// Updates <c>BoundingBox</c> with a single point of type <c>doube3</c>.
    /// </summary>
    /// <param name="point">Point to update the BoudingBoy with</param>
    private void UpdateWith(double3 point)
    {
        if (!this.ButtomLowerLeftCorner.HasValue || !this.TopUpperRightCorner.HasValue)
        {
            ButtomLowerLeftCorner = point;
            TopUpperRightCorner = point;
        }
        else
        {
            double buttomLowerLeftCorner_X = Math.Min(this.ButtomLowerLeftCorner.Value.x, point.x);
            double buttomLowerLeftCorner_Y = Math.Min(this.ButtomLowerLeftCorner.Value.y, point.y);
            double buttomLowerLeftCorner_Z = Math.Min(this.ButtomLowerLeftCorner.Value.z, point.z);
            double topUpperRightCorner_X = Math.Max(this.TopUpperRightCorner.Value.x, point.x);
            double topUpperRightCorner_Y = Math.Max(this.TopUpperRightCorner.Value.y, point.y);
            double topUpperRightCorner_Z = Math.Max(this.TopUpperRightCorner.Value.z, point.z);

            ButtomLowerLeftCorner = new double3(
                buttomLowerLeftCorner_X,
                buttomLowerLeftCorner_Y,
                buttomLowerLeftCorner_Z
                );
            TopUpperRightCorner = new double3(
                topUpperRightCorner_X, 
                topUpperRightCorner_Y, 
                topUpperRightCorner_Z
                );
        }
    }

    /// <summary>
    /// Updates <c>BoundingBox</c> with a list of points of type <c>doube3</c>.
    /// </summary>
    /// <param name="points">List of points to update the BoudingBox with</param>
    public void UpdateWith(List<double3> points)
    {
        foreach (double3 point in points)
        {
            this.UpdateWith(point);
        }
    }

    /// <summary>
    /// Updates <c>BoudingBox</c> with an array of points of the type <c>double3</c>.
    /// </summary>
    /// <param name="points">Array with points to update the BoudingBox</param>
    public void UpdateWith(double3[] points)
    {
        foreach (double3 point in points)
        {
            this.UpdateWith(point);
        }
    }

    /// <summary>
    /// Updates <c>BoudingBox</c> with a list of surfaces which are spanned by polygons of points of the type <c>double3</c>.
    /// </summary>
    /// <param name="surfaces">Surfaces spanned by polygon points of the type double3</param>
    public void UpdateWith(List<Surface> surfaces)
    {
        foreach (Surface surface in surfaces)
        {
            this.UpdateWith(surface);
        }
    }

    /// <summary>
    /// Updates <c>BoudingBox</c> with a surface spanned by a polygon of points of the type <c>double3</c>.
    /// </summary>
    /// <param name="surface">Surface spanned by polygon points of the type double3</param>
    public void UpdateWith(Surface surface)
    {
        foreach (double3 point in surface.Polygon)
        {
            this.UpdateWith(point);
        }
    }

    /// <summary>
    /// Updates <c>BoudingBox</c> with an existing <c>BoundingBox</c> spanned by two <c>double3</c>.
    /// </summary>
    /// <param name="boundingBox">Existing BoudingBox to update the Current one with</param>
    public void UpdateWith(BoundingBox boundingBox)
    {
        if (boundingBox.ButtomLowerLeftCorner.HasValue && boundingBox.TopUpperRightCorner.HasValue)
        {
            this.UpdateWith(boundingBox.ButtomLowerLeftCorner.Value);
            this.UpdateWith(boundingBox.TopUpperRightCorner.Value);
        }
    }

    /// <summary>
    /// Returns the center of the <c>BoundingBox</c>
    /// </summary>
    /// <returns>ETRS/UTM coordinates of the center of the <c>BoundingBox</c></returns>
    public double3? GetGroundSurfaceCenter()
    {
        if (this.IsInitialized())
        {
            return new double3(
                ButtomLowerLeftCorner.Value.x + 0.5 * (TopUpperRightCorner.Value.x - ButtomLowerLeftCorner.Value.x),
                ButtomLowerLeftCorner.Value.y + 0.5 * (TopUpperRightCorner.Value.y - ButtomLowerLeftCorner.Value.y),
                ButtomLowerLeftCorner.Value.z + 0.5 * (TopUpperRightCorner.Value.z - ButtomLowerLeftCorner.Value.z)
                );
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Returns true if both points that span the BoundungBox have been initialized and are not identical. Otherwise false is returned. 
    /// </summary>
    /// <returns></returns>
    public bool IsInitialized()
    {
        if (!this.ButtomLowerLeftCorner.HasValue || !this.TopUpperRightCorner.HasValue)
        {
            return false;
        }
        else
        {
            return !ButtomLowerLeftCorner.Equals(TopUpperRightCorner);
        }
    }


    public override string ToString()
    {
        string describtion = "Boundingbox with cordinates"
            + "\nButtomLowerLeftCorner:\n"
            + ButtomLowerLeftCorner
            + "\nTopUpperRightCorner:\n"
            + TopUpperRightCorner;

        return describtion;
    }
}
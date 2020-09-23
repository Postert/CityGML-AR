using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


/// <summary>
/// Contains methods to transform ETRS/UTM coordinates into Unity3D coordinates and can switch left and right handed coordinate system values.
/// <remarks>
/// ETRS/UTM coordinates can be converted into the Unity3D coordinate system.
/// </remarks>
/// </summary>
public static class CoordinateTransformer
{
    /// <summary>
    /// Calculates the position of the point relative to the specified anchor point and returns left handed coordinate values.
    /// </summary>
    /// <remarks>
    /// Use this method to calculate the coordinates based on an anchor point, as Unity restricts the coordinates to float values, which excludes the direct processing of ETRS/UTM coordinates in Unity3D. 
    /// </remarks>
    /// <param name="pointsUTMCoordinate">The ETRS/UTM coordinates of a point to be displayed in Unity.</param>
    /// <param name="anchorsUTMCoordinate">The ETRS/UTM coordinates of an anchor point within a distance smaller than 100 km to the anchor for each axis.</param>
    /// <returns>Return coordinate values that can be processes within the Unity3D's scene which are already converted to Unity3D's left handed coordinate system.</returns>
    /// <exception cref="System.ArgumentException">Thrown when a coordinate component exceeds a distance of 100 km on one of the axes. The latter exceeds the capacity of a float.</exception>
    public static Vector3 GetUnityCoordinates(double3 pointsUTMCoordinate, double3 anchorsUTMCoordinate)
    {
        float3 positionRelativeToAnchorPoint = new float3();
        for (int i = 0; i < 3; i++)
        {
            double coordinateComponent = pointsUTMCoordinate[i] - anchorsUTMCoordinate[i];
            if (coordinateComponent < 100 || coordinateComponent * -1 < 100)
            {
                positionRelativeToAnchorPoint[i] = (float)coordinateComponent;
            }
            else
            {
                throw new ArgumentException("The point to be converted must not exceed a distance of 100 km from the anchor point on any axis. pointsUTMCoordinate value: " + pointsUTMCoordinate[i] + ", anchorsUTMCoordinate value: " + pointsUTMCoordinate[i] + ", position relative to anchor point on this axis: " + positionRelativeToAnchorPoint[i]);
            }
        }

        return SwitchLeftHandedRightHandedCoordinates((Vector3)positionRelativeToAnchorPoint);
    }

    /// <summary>
    /// Switches coordinates left or right handed coordinate system values to the other system.
    /// </summary>
    /// <param name="coordinatesValues">Coordinate values to be converted.</param>
    /// <returns>Converted coordinate values.</returns>
    public static Vector3 SwitchLeftHandedRightHandedCoordinates(Vector3 coordinatesValues)
    {
        return new Vector3(coordinatesValues.x, coordinatesValues.z, coordinatesValues.y);
    }

    /// <summary>
    /// Switches coordinates left or right handed coordinate system values to the other system.
    /// </summary>
    /// <param name="coordinatesValues">Coordinate values to be converted.</param>
    /// <returns>Converted coordinate values.</returns>
    public static double3 SwitchLeftHandedRightHandedCoordinates(double3 coordinateValues)
    {
        return new double3(coordinateValues.x, coordinateValues.z, coordinateValues.y);
    }
}
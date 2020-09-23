using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// Manages and displays <c>Annotation</c>s in the Unity3D scene.
/// </summary>
public class AnnotationManager : MonoBehaviour
{
    /// <value>DatabaseService to perform to query the local SQLite database.</value>
    public DatabaseService DatabaseService { get; set; }

    /// <value>The edge length of the square <c>BoundingBox</c> in meters.</value>
    private const int boundingBoxDimension = 300;

    /// <summary>
    /// Initializes <c>DatabaseService</c> after instatiation.
    /// </summary>
    /// <exception cref="System.ApplicationException">Thrown when <c>DatabaseService</c> is not added to the AR Session Origin GameObject.</exception>
    private void Awake()
    {
        DatabaseService = GameObject.Find("AR Session Origin").GetComponent<TargetDetector>().DatabaseService;

        if (DatabaseService == null)
        {
            throw new ApplicationException("Cannot find GameObject with DatabaseService. Add it to the 'AR Session Origin' GameObject.");
        }
    }

    /// <summary>
    /// Instantiates <c>Annotation</c> GameObjects arround the anchor point.
    /// </summary>
    /// <param name="utmAnchorPoint">ETRS/UTM coordinate used to anchor the annotations</param>
    /// <param name="buildingsWithinBoundingBox"><c>Building</c>s within the <c>BoundingBox</c> which are the <c>BuildingAnnotation</c>s or the <c>SurfaceAnnotation</c>s are associated with.</param>
    public void CreateAnnotationsAroundAnchorPoint(double3 utmAnchorPoint, Dictionary<string, Building> buildingsWithinBoundingBox)
    {
        //Debug.Log(MyTimer.GetSecondsSiceStartAsString() + ": AnnotationManager determining BoundinBox around detected target");

        /// Calculate the BoundingBox arround the given <c>anchorPoint</c> with the dimension of the predefined <c>boundingBoxDimension</c>. 
        double3 lowerLeftCorner = new double3(utmAnchorPoint.x - (double)(0.5 * boundingBoxDimension), utmAnchorPoint.y - (double)(0.5 * boundingBoxDimension), 0);
        double3 upperRightCorner = new double3(utmAnchorPoint.x + (double)(0.5 * boundingBoxDimension), utmAnchorPoint.y + (double)(0.5 * boundingBoxDimension), 0);
        BoundingBox boundingBoxAroundTarget = new BoundingBox(lowerLeftCorner, upperRightCorner);

        /// BuildingAnnatitions: querying and instantiation
        List<BuildingAnnotation> buildingAnnotations = DatabaseService.GetBuildingAnnotation(boundingBoxAroundTarget, buildingsWithinBoundingBox);

        foreach (BuildingAnnotation buildingAnnotation in buildingAnnotations)
        {
            //Debug.Log("BuildingAnnotation associated with:\n" + buildingAnnotation.AssociatedBuilding.ToString());

            /// A <c>BuildingAnnotation</c> is just procesed, if the <c>BoundingBox</c> of this <c>Building</c> is defined.
            if (buildingAnnotation.AssociatedBuilding.BoundingBox.GetGroundSurfaceCenter().HasValue)
            {
                /// Determination of the <c>BuildingAnnotation</c>'s anchor point utilizing its associated <c>Building</c>.
                double3 groundSurfaceCenter = buildingAnnotation.AssociatedBuilding.BoundingBox.GetGroundSurfaceCenter().Value;
                double3 utmCoordinates = new double3(groundSurfaceCenter.x, groundSurfaceCenter.y, groundSurfaceCenter.z + buildingAnnotation.AssociatedBuilding.MeasuredHeight + BuildingAnnotation.meterAboveBuilding);

                InstantiateAnnotationUnityTemplate(utmCoordinates, utmAnchorPoint, buildingAnnotation.AnnotationProperties, buildingAnnotation.AnnotationComponent);
            }
            else
            {
                Debug.Log("Cannot determin the ground surface center of Building with CityGMLID: " + buildingAnnotation.AssociatedBuilding.CityGMLID);
            }
        }


        /// SurfaceAnnatitions: querying and instantiation

        /// Determination of the <c>Surface</c>s 
        Dictionary<string, Surface> surfacesWithinBoundingBox = new Dictionary<string, Surface>();
        foreach (Building building in buildingsWithinBoundingBox.Values)
        {
            foreach (KeyValuePair<string, Surface> surface in building.ExteriorSurfaces)
            {
                surfacesWithinBoundingBox.Add(surface.Key, surface.Value);
            }
        }

        List<SurfaceAnnotation> surfaceAnnotations = DatabaseService.GetSurfaceAnnotations(boundingBoxAroundTarget, surfacesWithinBoundingBox);

        //Debug.Log("Anzahl SurfaceAnnotation innerhalb der BoundingBox: " + surfaceAnnotations.Count);

        /// Determination of the <c>SurfaceAnnotation</c>'s anchor point utilizing its associated <c>Surface</c> and queried <c>SurfaceAnnotation</c>'s parameters.
        foreach (SurfaceAnnotation surfaceAnnotation in surfaceAnnotations)
        {
            /// Determination of the base line using two adjacent polygon points of the <c>Surface</c>.
            double3 fristBaselinePoint = surfaceAnnotation.AssociatedSurface.Polygon[surfaceAnnotation.AnnotationAnchorPointIndex];
            double3 secondBaselinePoint = surfaceAnnotation.AssociatedSurface.Polygon[surfaceAnnotation.AnnotationAnchorPointIndex + 1];
            Vector3 directionVectorOfTheBaseLine = (float3)(secondBaselinePoint - fristBaselinePoint);

            /// The <c>Surface</c>'s normal vector is facing in the opposite direction of the one the <c>SurfaceAnnotation</c> is facing.
            Vector3 surfaceNormal = surfaceAnnotation.AssociatedSurface.GetSurfaceNormal();

            /// Determination of the starting position on the base line using relative position information between 0 and 1
            Vector3 vectorToStartingPointOnTheBaseline = ((Vector3)(float3)(directionVectorOfTheBaseLine) * (float)surfaceAnnotation.RelativePositionBetweenBasePoints);
            double3 startingPointOnTheBaseLine = (fristBaselinePoint + (float3)vectorToStartingPointOnTheBaseline);

            /// Determination of the initial anchor point within the surface by addition perpendicular to the baseline and the surface normal vector.
            double3 surfaceAnnotationAnchorPoint = startingPointOnTheBaseLine;
            surfaceAnnotationAnchorPoint += (float3)(Vector3.Cross(surfaceNormal, directionVectorOfTheBaseLine).normalized) * (float)surfaceAnnotation.HeightAboveBaseLine;

            /// Subtraction of an offset to move the annotation away from the wall. As the direction vector the inverted surface normal vector (opposite direction to the viewing direction) is used.
            surfaceAnnotationAnchorPoint -= (float3)(-1 * surfaceNormal.normalized * SurfaceAnnotation.SurfaceOffset);

            /// The rotation of the annotation is either specified by a predefined vector (x,y,z) != (0,0,0). If the latter is the case, the opposite direction of the surface normal is used to align the annotation with the wall. 
            if (surfaceAnnotation.AnnotationProperties.PointingDirection.Equals(Vector3.zero))
            {
                surfaceAnnotation.AnnotationProperties.PointingDirection = surfaceNormal * -1;
            }

            InstantiateAnnotationUnityTemplate(surfaceAnnotationAnchorPoint, utmAnchorPoint, surfaceAnnotation.AnnotationProperties, surfaceAnnotation.AnnotationComponent);
        }


        /// WorldCoordinateAnnatitions: querying and instantiation
        List<WorldCoordinateAnnotation> worldCoordinateAnnotations = DatabaseService.GetWorldCoordinateAnnotation(boundingBoxAroundTarget);

        /// The given ETRS/UTM coordinate is used as the <c>WorldCoordinateAnnotation</c>'s anchor point.
        foreach (WorldCoordinateAnnotation worldCoordinateAnnotation in worldCoordinateAnnotations)
        {
            InstantiateAnnotationUnityTemplate(worldCoordinateAnnotation.AnchorUTMCoordinates, utmAnchorPoint, worldCoordinateAnnotation.AnnotationProperties, worldCoordinateAnnotation.AnnotationComponent);
        }

        //Debug.Log(MyTimer.GetSecondsSiceStartAsString() + ": Creating Annotations around target succeeded");
    }


    /// <summary>
    /// Creates an <c>BuildingAnnotation</c>, <c>SurfaceAnnotation</c> or a <c>WorldCoordinateAnnotation</c> with the <c>AnnotationContent</c> and instantiates it in the Unity3D scene.
    /// </summary>
    /// <param name="utmCoordinates">ETRS/UTM coordinates of the <c>Annotation</c></param>
    /// <param name="utmAnchor">ETRS/UTM coordinates of the cnchor point.</param>
    /// <param name="annotationProperties">Properties of the <c>Annotation.</c></param>
    /// <param name="annotationContent">Content of the <c>Annotation.</c></param>
    private void InstantiateAnnotationUnityTemplate(double3 utmCoordinates, double3 utmAnchor, AnnotationProperties annotationProperties, AnnotationContent annotationContent)
    {
        this.InstantiateAnnotationUnityTemplate(CoordinateTransformer.GetUnityCoordinates(utmCoordinates, utmAnchor), annotationProperties, annotationContent);
    }


    /// <summary>
    /// Creates an <c>BuildingAnnotation</c>, <c>SurfaceAnnotation</c> or a <c>WorldCoordinateAnnotation</c> with the <c>AnnotationContent</c> and instantiates it in the Unity3D scene.
    /// </summary>
    /// <param name="unityCoordinates">Unity3D coordinates of the <c>Annotation</c></param>
    /// <param name="annotationProperties">Properties of the <c>Annotation.</c></param>
    /// <param name="annotationContent">Content of the <c>Annotation.</c></param>
    private void InstantiateAnnotationUnityTemplate(Vector3 unityCoordinates, AnnotationProperties annotationProperties, AnnotationContent annotationContent)
    {
        /// Creation of new Gameobject as child of the <c>AnnotationManager</c>'s GameObject
        SimpleTextAnnotationUnityTemplate annotationUnityTemplate = new GameObject().AddComponent<SimpleTextAnnotationUnityTemplate>();
        annotationUnityTemplate.gameObject.transform.parent = transform;
        annotationUnityTemplate.gameObject.transform.position = unityCoordinates;
        
        /// Set pointing direction of the <c>Annotation</c> defined by the given vector or always facing to the user specified by the vector (0,0,0)
        Vector3 annotationRotation = CoordinateTransformer.SwitchLeftHandedRightHandedCoordinates((Vector3)annotationProperties.PointingDirection);
        if (annotationRotation.Equals(Vector3.zero))
        {
            annotationUnityTemplate.IsLookingToARCamera = true;
        }
        else
        {
            annotationUnityTemplate.IsLookingToARCamera = false;
            annotationUnityTemplate.gameObject.transform.rotation = Quaternion.LookRotation(annotationRotation);
        }
        
        annotationUnityTemplate.IsScalingWithARCameraDistance = annotationProperties.ScaleWithCameraDistance;

        /// Creation of <c>Annotation</c> content
        switch (annotationContent)
        {
            case TextAnnotationComponent annotationTextComponent:

                annotationUnityTemplate.SetAnnotationText(annotationTextComponent.Text);
                annotationUnityTemplate.SetLocalScale(annotationTextComponent.TextSize);

                break;

            default: throw new NotImplementedException();
        }
    }


    /// <summary>
    /// Updates the position of all <c>Annotation</c> GameObjects in the Unity3D scene.
    /// </summary>
    /// <remarks>Typically used to compensate drift repeated target detection.</remarks>
    /// <param name="anchorCoordinates">Target Unity3D coordinates to place the <c>AnnotationManager</c>'s GameObject</param>
    /// <param name="anchorOrientation">Target orientation of the <c>AnnotationManager</c>'s GameObject</param>
    public void UpdateAnnotationsPositionInUnity(Vector3 anchorCoordinates, Quaternion anchorOrientation)
    {
        transform.position = anchorCoordinates;
        //transform.rotation = trackedImageRotation;
    }
}
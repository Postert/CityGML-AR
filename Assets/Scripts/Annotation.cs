using Unity.Mathematics;

/// <summary>
/// Being the template of all <c>Annotation</c>s, it manages AnnotationProperties.
/// </summary>
public abstract class Annotation
{
    public AnnotationProperties AnnotationProperties { get; set; }

    /// <summary>
    /// Initializes the Annotation with AnnotationProperties.
    /// </summary>
    /// <param name="annotationProperties">AnnotationProperties that describe</param>
    public Annotation(AnnotationProperties annotationProperties) =>
        (AnnotationProperties) = (annotationProperties);

    /// <summary>
    /// Initializes the Annotation with AnnotationProperties.
    /// </summary>
    /// <param name="scaleWithCameraDistance">Specifies whether the annotation increases in size in the 3D scene depending on the distance to the user, so that the display size on the end device remains unchanged when the user moves.</param>
    /// <param name="scaleBySelection">Specifies whether the display size of an annotation increases for the user after a selection</param>
    /// <param name="pointingDirection">Specifies whether the annotation always orients itself in the direction of the user to provide the best possible representation regardless of perspective (vector (0,0,0)) or has a constant orientation in space (vector other than (0,0,0)).</param>
    public Annotation(bool scaleWithCameraDistance, bool scaleBySelection, float3 pointingDirection) =>
        (AnnotationProperties) = (new AnnotationProperties(scaleWithCameraDistance, scaleBySelection, pointingDirection));

    public override string ToString()
    {
        return AnnotationProperties.ToString();
    }
}

/// <summary>
/// Defines basic properties of an annotation. 
/// </summary>
public class AnnotationProperties
{
    /// <summary>
    /// <value>Specifies whether the annotation increases in size in the 3D scene depending on the distance to the user, so that the display size on the end device remains unchanged when the user moves.</value>
    /// </summary>
    public bool ScaleWithCameraDistance { get; set; }

    /// <summary>
    /// <value>Specifies whether the display size of an annotation increases for the user after a selection</value>
    /// </summary>
    public bool ScaleBySelection { get; set; }

    /// <summary>
    /// <value>Specifies whether the annotation always orients itself in the direction of the user to provide the best possible representation regardless of perspective (vector (0,0,0)) or has a constant orientation in space (vector other than (0,0,0)).</value>
    /// </summary>
    public float3 PointingDirection { get; set; }

    /// <summary>
    /// Initializes object with general annotation properties.
    /// </summary>
    /// <param name="scaleWithCameraDistance">Specifies whether the annotation increases in size in the 3D scene depending on the distance to the user, so that the display size on the end device remains unchanged when the user moves.</param>
    /// <param name="scaleBySelection">Specifies whether the display size of an annotation increases for the user after a selection</param>
    /// <param name="pointingDirection">Specifies whether the annotation always orients itself in the direction of the user to provide the best possible representation regardless of perspective (vector (0,0,0)) or has a constant orientation in space (vector other than (0,0,0)).</param>
    public AnnotationProperties(bool scaleWithCameraDistance, bool scaleBySelection, float3 pointingDirection) =>
        (ScaleWithCameraDistance, ScaleBySelection, PointingDirection) =
        (scaleWithCameraDistance, scaleBySelection, pointingDirection);

    public override string ToString()
    {
        return "\n--AnnotationProperties----------------------------------"
            + "\nScaleWithCameraDistance: " + ScaleWithCameraDistance
            + "\nScaleBySelection: " + ScaleBySelection
            + "\nPointingDirection " + PointingDirection;
    }
}


/// <summary>
/// Contains the annotation's content.
/// </summary>
public abstract class AnnotationContent { }

// TODO: Implementation of multimedia components


/// <summary>
/// Describes a text component of an annotation consisting of a text and a test size.
/// </summary>
public class TextAnnotationComponent : AnnotationContent
{
    /// <summary>
    /// <value>Text to be displayed.</value>
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// <value>Size of the text to be displayed.</value>
    /// </summary>
    public float TextSize { get; set; }

    /// <summary>
    /// Initializes the TextAnnotationComponent with the text to be displayed and the text size.
    /// </summary>
    /// <param name="text">Text to be displayed within the annotation.</param>
    /// <param name="textSize">Size of the annotation text.</param>
    public TextAnnotationComponent(string text, float textSize) =>
        (Text, TextSize) = (text, textSize);

    public override string ToString()
    {
        return "\n--AnnotationTextComponent----------------------------------"
            + "\nText: " + Text
            + "\nLocalScale: " + TextSize;
    }
}


/// <summary>
/// <c>Annotation</c> hovering over an associated <c>Building</c>.
/// </summary>
public class BuildingAnnotation : Annotation
{
    /// <summary>
    /// <value>Constant and predefined offset to be added to the highest <c>Building</c> coordinate to that enable visibility.</value>
    /// </summary>
    public const int meterAboveBuilding = 1;

    /// <summary>
    /// <c>Building</c> associated with the <c>Annotation</c>.
    /// </summary>
    public Building AssociatedBuilding { get; set; }

    /// <summary>
    /// Component (like <c>TextAnnotationComponent</c>) with the <c>Annotation</c>'s content.
    /// </summary>
    // TODO: Implementation of nesting
    public AnnotationContent AnnotationComponent { get; set; }

    /// <summary>
    /// Creates and completely initializes a <c>BuildingAnnotation</c>. 
    /// </summary>
    /// <param name="associatedBuilding"><c>Building</c> used for anchoring and the <c>Annotation</c> is associated with.</param>
    /// <param name="annotationComponent">Content of the <c>BuildingAnnotation</c> to be displayed.</param>
    /// <param name="annotationProperties">Genral properties of an <c>Annotation</c>.</param>
    public BuildingAnnotation(Building associatedBuilding, AnnotationContent annotationComponent, AnnotationProperties annotationProperties) : base(annotationProperties) =>
        (AssociatedBuilding, AnnotationComponent) = (associatedBuilding, annotationComponent);


    public override string ToString()
    {
        return "BuildingAnnotation associated to building " + AssociatedBuilding.CityGMLID
            + base.ToString()
            + AnnotationComponent.ToString()
            + "\n--associated Building----------------------------------"
            + "\n\n";
    }
}


/// <summary>
/// <c>Annotation</c> displayed on an associated <c>Surface</c>.
/// </summary>
public class SurfaceAnnotation : Annotation
{
    /// <summary>
    /// Constant and predefined distance to the <c>AssociatedSurface</c>.
    /// <remarks>It prevents the <c>Surface</c> from occluding the <c>Annotation</c>.</remarks>
    /// </summary>
    public const float SurfaceOffset = 0.01f;

    /// <summary>
    /// <c>Surface</c> the <c>Annotation</c> is associated with.
    /// </summary>
    public Surface AssociatedSurface { get; set; }

    /// <summary>
    /// Index of one polygon point of the <c>AssociatedSurface</c>.
    /// <remarks>The referenced point and its successor are used to for anchoring the <c>Annotation</c> between these adjacent points.</remarks>
    /// </summary>
    public int AnnotationAnchorPointIndex { get; set; }

    /// <summary>
    /// Describes the relative position [0,1] between the two adjacent points used for anchoring the <c>Annotation</c>.
    /// <remarks>The base points are derived from the <c>AssociatedSurface</c>'s polygon with the help of the <c>AnnotationAnchorPointIndex</c>.</remarks>
    /// </summary>
    public double RelativePositionBetweenBasePoints { get; set; }

    /// <summary>
    /// Height above the baseline spanned by two adjacent polygon points.
    /// <remarks>Distance of the anchoring of the <c>SurfaceAnnotation</c> perpendicular to the base line spanned by two points of the <c>AssociatedSurface</c>'s polygon and perpendicular to the <c>Surface</c>'s normal vector.</remarks>
    /// </summary>
    public double HeightAboveBaseLine { get; set; }

    /// <summary>
    /// Component (like <c>TextAnnotationComponent</c>) with the <c>Annotation</c>'s content.
    /// </summary>
    // TODO: Implementation of nesting
    public AnnotationContent AnnotationComponent { get; set; }

    /// <summary>
    /// Creates and completely initializes a <c>SurfaceAnnotation</c>.
    /// </summary>
    /// <param name="associatedSurface"><c>Surface</c> used for anchoring and the <c>Annotation</c> is associated with.</param>
    /// <param name="annotationAnchorPointIndex">Index of one polygon point of the <c>AssociatedSurface</c>. The referenced point and its successor are used to for anchoring the <c>Annotation</c> between these adjacent points.</param>
    /// <param name="relativePositionBetweenBasePoints">Height above the baseline spanned by two adjacent polygon points. Distance of the anchoring of the <c>SurfaceAnnotation</c> perpendicular to the base line spanned by two points of the <c>AssociatedSurface</c>'s polygon and perpendicular to the <c>Surface</c>'s normal vector.</param>
    /// <param name="heightAboveBaseLine">Component (like <c>TextAnnotationComponent</c>) with the <c>Annotation</c>'s content.</param>
    /// <param name="annotationComponent">Content of the <c>SurfaceAnnotation</c> to be displayed.</param>
    /// <param name="annotationProperties">Genral properties of an <c>Annotation</c>.</param>
    public SurfaceAnnotation(Surface associatedSurface, int annotationAnchorPointIndex, double relativePositionBetweenBasePoints, double heightAboveBaseLine, AnnotationContent annotationComponent, AnnotationProperties annotationProperties) : base(annotationProperties) =>
        (AssociatedSurface, AnnotationAnchorPointIndex, RelativePositionBetweenBasePoints, HeightAboveBaseLine, AnnotationComponent)
        = (associatedSurface, annotationAnchorPointIndex, relativePositionBetweenBasePoints, heightAboveBaseLine, annotationComponent);

    public override string ToString()
    {
        return "SurfaceAnnotation associated to surface " + AssociatedSurface.CityGMLID
            + base.ToString()
            + AnnotationComponent.ToString()
            + "\nwith AnnotationAnchorPointIndex " + AnnotationAnchorPointIndex
            + "\nwith RelativePositionBetweenBasePoints " + RelativePositionBetweenBasePoints
            + "\nwith HeightAboveBaseLine " + HeightAboveBaseLine
            + "\n\n";
    }
}



public class WorldCoordinateAnnotation : Annotation
{
    /// <summary>
    /// Core component for anchoraging the SurfaceAnnotation
    /// </summary>
    public double3 AnchorUTMCoordinates { get; set; }

    /// <summary>
    /// Component (like <c>TextAnnotationComponent</c>) with the <c>Annotation</c>'s content.
    /// </summary>
    // TODO: Implementation of nesting
    public AnnotationContent AnnotationComponent { get; set; }

    /// <summary>
    /// Creates and completely initializes a <c>WorldCoordinateAnnotation</c>.
    /// </summary>
    /// <param name="anchorUTMCoordinates">UTM coordinates used for anchoring the <c>Annotation</c>.</param>
    /// <param name="annotationComponent">Content of the <c>WorldCoordinateAnnotation</c> to be displayed.</param>
    /// <param name="annotationProperties">Genral properties of an <c>Annotation</c>.</param>
    public WorldCoordinateAnnotation(double3 anchorUTMCoordinates, AnnotationContent annotationComponent, AnnotationProperties annotationProperties) : base(annotationProperties) =>
        (AnchorUTMCoordinates, AnnotationComponent, AnnotationProperties) = (anchorUTMCoordinates, annotationComponent, annotationProperties);

    public override string ToString()
    {
        return "WorldCoordinateAnnotation associated to coordinate " + AnchorUTMCoordinates.ToString()
            + base.ToString()
            + AnnotationComponent.ToString()
            + "\n\n";
    }
}
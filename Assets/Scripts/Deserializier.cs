using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using Unity.Mathematics;
using UnityEngine;

public static class Deserializer
{
    /// <summary>
    /// Deserializes <c>Building</c>s (with its exterior geometries and selected properties) and <c>Annotation</c>s async and returns the results after all files are processed.
    /// </summary>
    /// <param name="cityGMLFileNames">File names of the CityGML files</param>
    /// <param name="cityGMLFileStreams">CityGML files</param>
    /// <returns><c>Building</c>s, <c>BuildingAnnotation</c>s, <c>SurfaceAnnotation</c>s and <c>WorldCoordinateAnnotation</c>s wihtin the CityGML files</returns>
    public static async Task<(List<Building>, List<BuildingAnnotation>, List<SurfaceAnnotation>, List<WorldCoordinateAnnotation>, BoundingBox)[]> GetBuildingsAndAnnotationsAsync(string[] cityGMLFileNames, StringReader[] cityGMLFileStreams)
    {
        List<Task<(List<Building>, List<BuildingAnnotation>, List<SurfaceAnnotation>, List<WorldCoordinateAnnotation>, BoundingBox)>> deserializationTasks
            = new List<Task<(List<Building>, List<BuildingAnnotation>, List<SurfaceAnnotation>, List<WorldCoordinateAnnotation>, BoundingBox)>>();

        foreach (StringReader cityGMLFileStream in cityGMLFileStreams)
        {
            try
            {
                deserializationTasks.Add(Task.Run(() => Deserializer.Deserialize(cityGMLFileNames[Array.IndexOf(cityGMLFileStreams, cityGMLFileStream)], cityGMLFileStream)));
            }
            catch (Exception e)
            {
                Debug.LogError("CityGML " + cityGMLFileNames[Array.IndexOf(cityGMLFileStreams, cityGMLFileStream)] + " cannot be deserialized: " + e);
            }
        }

        return await Task.WhenAll(deserializationTasks);
    }


    /// <summary>
    /// Deserializes <c>Building</c>s (with its exterior geometries and selected properties) and <c>Annotation</c>s.
    /// </summary>
    /// <param name="cityGMLFileName">File name of the CityGML file</param>
    /// <param name="cityGMLFileStream">CityGML file</param>
    /// <returns><c>Building</c>s, <c>BuildingAnnotation</c>s, <c>SurfaceAnnotation</c>s and <c>WorldCoordinateAnnotation</c>s wihtin the CityGML files</returns>
    public static (List<Building>, List<BuildingAnnotation>, List<SurfaceAnnotation>, List<WorldCoordinateAnnotation>, BoundingBox) Deserialize(string cityGMLFileName, StringReader cityGMLFileStream)
    {
        //Debug.Log(MyTimer.GetSecondsSiceStartAsString() + ": " + cityGMLFileName + " deserialization started.");

        List<Building> initializedBuildings = new List<Building>();
        List<BuildingAnnotation> initializedBuildingAnnotations = new List<BuildingAnnotation>();
        List<SurfaceAnnotation> initializedSurfaceAnnotations = new List<SurfaceAnnotation>();
        List<WorldCoordinateAnnotation> initializedWorldCoordinateAnnotations = new List<WorldCoordinateAnnotation>();

        BoundingBox cityGMLBoundingBox = null;

        //Debug.Log("CityGML successfully imported from Resource folder: " + cityGMLFile.name);

        /// Create XMLReader Object to parse the CityGML-File
        using (XmlReader reader = XmlReader.Create(cityGMLFileStream))
        {
            /// Read BoudningBox of the CityGML-file:
            if (reader.ReadToFollowing("gml:Envelope"))
            {
                XmlReader boundingBoxReader = reader.ReadSubtree();

                try
                {
                    if (!boundingBoxReader.ReadToFollowing("gml:lowerCorner"))
                    {
                        throw new ArgumentException("Tag <gml:lowerCorner> could not be found.");
                    }

                    string lowerCornerCoordinateString = boundingBoxReader.ReadElementContentAsString();
                    double3 lowerCornerPoint = Deserializer.ParseCoordinateString(lowerCornerCoordinateString);

                    if (!boundingBoxReader.ReadToFollowing("gml:upperCorner"))
                    {
                        throw new ArgumentException("Tag <gml:upperCorner> could not be found.");
                    }

                    string upperCornerCoordinateString = boundingBoxReader.ReadElementContentAsString();
                    double3 upperCornerPoint = Deserializer.ParseCoordinateString(upperCornerCoordinateString);

                    cityGMLBoundingBox = new BoundingBox(lowerCornerPoint, upperCornerPoint);
                }
                catch (ArgumentException e)
                {
                    throw new ArgumentException("File does not contain a boundig box:\n" + e);
                }

                boundingBoxReader.Close();
            }


            /// Read the CityMembers (Buildings and WorldCoordinateAnnotations:
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "bldg:Building":
                            XmlReader buildingReader = reader.ReadSubtree();

                            (List<Building> newBuildings, List<BuildingAnnotation> newBuildingAnnotations, List<SurfaceAnnotation> newSurfaceAnnotations)
                                = Deserializer.GetBuildingWithPartsAndAnnotations(buildingReader);

                            initializedBuildings.AddRange(newBuildings);
                            initializedBuildingAnnotations.AddRange(newBuildingAnnotations);
                            initializedSurfaceAnnotations.AddRange(newSurfaceAnnotations);

                            buildingReader.Close();
                            break;

                        case "annotation:WorldCoordinateAnnotation":
                            AnnotationProperties annotationProperties = Deserializer.GetAnnotationProperties(reader);
                            XmlReader worldCoordinateAnnotationReader = reader.ReadSubtree();
                            worldCoordinateAnnotationReader.ReadToFollowing("gml:pos");
                            (double3 umlCoordinates, AnnotationContent annotationComponents) = Deserializer.GetWorldCoordinateAnnotationContentAndCoordinates(worldCoordinateAnnotationReader);
                            Debug.Log(annotationProperties.PointingDirection.ToString());
                            WorldCoordinateAnnotation worldCoordinateAnnotation = new WorldCoordinateAnnotation(umlCoordinates, annotationComponents, annotationProperties);

                            initializedWorldCoordinateAnnotations.Add(worldCoordinateAnnotation);

                            worldCoordinateAnnotationReader.Close();
                            break;
                    }
                }
            }

            //Debug.Log(MyTimer.GetSecondsSiceStartAsString() + ": " + cityGMLFileName + " deserialization completed.");

            reader.Close();
            cityGMLFileStream.Close();

            return (initializedBuildings, initializedBuildingAnnotations, initializedSurfaceAnnotations, initializedWorldCoordinateAnnotations, cityGMLBoundingBox);
        }
    }


    /// <summary>
    /// Returns <c>Building</c>s and assiciated <c>BuildingAnnotation</c>s and <c>SurfaceAnnotation</c>s within the passage to be deserialized.
    /// The XML node is closed after it has been processed in this method.
    /// </summary>
    /// <param name="buildingReader">Reader to process all subnodes within its XML node</param>
    /// <returns><c>Building</c> objects for each part of a building, and assicuated <c>BuildingAnnotation</c>s and associated <c>SurfaceAnnotation</c>s</returns>
    private static (List<Building>, List<BuildingAnnotation>, List<SurfaceAnnotation>) GetBuildingWithPartsAndAnnotations(XmlReader buildingReader)
    {
        List<Building> buildingWithParts = new List<Building>();
        List<BuildingAnnotation> buildingAnnotations = new List<BuildingAnnotation>();
        List<SurfaceAnnotation> surfaceAnnotations = new List<SurfaceAnnotation>();

        /// Go to building or buildingpart start element
        while (buildingReader.Read() && !(buildingReader.NodeType == XmlNodeType.Element) && !(buildingReader.Name == "bldg:Building" || buildingReader.Name == "bldg:BuildingPart")) ;

        /// Reading the needed building properties
        string buildingID = buildingReader.GetAttribute("gml:id");
        Building newBuilding = new Building(buildingID);

        buildingReader.ReadToFollowing("bldg:measuredHeight");
        if (!float.TryParse(buildingReader.ReadElementContentAsString(), NumberStyles.Any, CultureInfo.InvariantCulture, out float measuredHeight))
        {
            Debug.LogError("Building does not contain a measuredHeight");
            return (null, null, null);
        }

        newBuilding.MeasuredHeight = measuredHeight;

    /// Determine the level of detail of the current building
    SurfaceAndBuildingDetection:

        buildingReader.Read();
        switch (buildingReader.Name)
        {
            case "annotation:BuildingAnnotation":
                AnnotationProperties annotationProperties = Deserializer.GetAnnotationProperties(buildingReader);

                XmlReader buildingAnnotationReader = buildingReader.ReadSubtree();

                BuildingAnnotation buildingAnnotation = Deserializer.GetBuildingAnnotation(buildingAnnotationReader, newBuilding, annotationProperties);
                buildingAnnotations.Add(buildingAnnotation);

                buildingAnnotationReader.Close();

                buildingReader.Read();


                goto SurfaceAndBuildingDetection;


            /// LOD1: Surfaces are directly stored in the "bldg:lod1Solid"-tag.
            /// The ground surface polygon must be geometrically determined.
            case "bldg:lod1Solid":
                /// For LOD1 buildings, the ground surface is not declared using a separate day. 
                /// Thus, the base area is identified by determining the surface with the lowest average height of all spanning vertices. 

                XmlReader LOD1SurfacesReader = buildingReader.ReadSubtree();

                while (LOD1SurfacesReader.ReadToFollowing("gml:surfaceMember"))
                {
                    XmlReader surfaceMemberReader = LOD1SurfacesReader.ReadSubtree();
                    (Surface deserializedSurface, List<SurfaceAnnotation> deserializedSurfaceAnnotations) = Deserializer.GetSurfaceAndSurfaceAnnotations(surfaceMemberReader, SurfaceType.UNDEFINED);
                    surfaceMemberReader.Close();

                    string uniqueSurfaceID = (deserializedSurface.CityGMLID != null) ? deserializedSurface.CityGMLID : newBuilding.CityGMLID + "--" + (newBuilding.ExteriorSurfaces.Count + 1).ToString("0000");
                    newBuilding.AddSurface(uniqueSurfaceID, deserializedSurface);
                    surfaceAnnotations.AddRange(deserializedSurfaceAnnotations);
                }

                LOD1SurfacesReader.Close();
                break;

            /// LOD2: the "bldg:lod2Solid"-tag contains references to all building surfaces. 
            /// The surfaces themselves are defined individually in the following tags.
            /// The ground surface is marked with the "GroundSurface"-tag. 
            case "bldg:lod2Solid":
                /// Navigate to first point of the groundsurface using a new reader instance with the child notes of the current building node.
                /// Process only the subtree to ensure that just GroundSurface points are considered.
                /// If there is no GroundSurface node in the current building node: skip this building.

                while (buildingReader.Read())
                {
                    if (buildingReader.NodeType != XmlNodeType.Element)
                    {
                        continue;
                    }

                    if (buildingReader.Name == "bldg:consistsOfBuildingPart")
                    {
                        goto BuildingFinischedContinueWithBuildingParts;
                    }

                    if (buildingReader.Name != "bldg:boundedBy")
                    {
                        continue;
                    }

                    /// current tag: <bldg:boundedBy>

                    XmlReader boundedByReader = buildingReader.ReadSubtree();

                    /// current tag: reight before <bldg:boundedBy>

                    /// Determining SurfaceType
                    SurfaceType surfaceType = SurfaceType.UNDEFINED;

                    boundedByReader.Read();
                    boundedByReader.Read();
                    boundedByReader.Read();

                    /// current tag: SurfaceType tag

                    switch (boundedByReader.Name)
                    {
                        case "bldg:GroundSurface":
                            surfaceType = SurfaceType.GroundSurface;
                            break;
                        case "bldg:WallSurface":
                            surfaceType = SurfaceType.WallSurface;
                            break;

                        case "bldg:RoofSurface":
                            surfaceType = SurfaceType.RoofSurface;
                            break;
                    }

                    buildingReader.ReadToFollowing("gml:surfaceMember");
                    XmlReader surfaceMemberReader = buildingReader.ReadSubtree();

                    (Surface deserializedSurface, List<SurfaceAnnotation> deserializedSurfaceAnnotations) = Deserializer.GetSurfaceAndSurfaceAnnotations(surfaceMemberReader, surfaceType);

                    surfaceMemberReader.Close();
                    boundedByReader.Close();

                    string uniqueSurfaceID = (deserializedSurface.CityGMLID != null) ? deserializedSurface.CityGMLID : newBuilding.CityGMLID + "--" + (newBuilding.ExteriorSurfaces.Count + 1).ToString("0000");
                    newBuilding.AddSurface(uniqueSurfaceID, deserializedSurface);
                    surfaceAnnotations.AddRange(deserializedSurfaceAnnotations);
                }
                break;

            default: throw new ArgumentException("Unknown LOD: the LOD-tag could not be found. (Building: " + newBuilding.CityGMLID + ")");
        }

    BuildingFinischedContinueWithBuildingParts:
        buildingWithParts.Add(newBuilding);
        /// Check for Buildingsparts

        do
        {
            if (buildingReader.NodeType == XmlNodeType.Element && buildingReader.Name == "bldg:consistsOfBuildingPart")
            {
                (List<Building> newBuildings, List<BuildingAnnotation> newBuildingAnnotations, List<SurfaceAnnotation> newSurfaceAnnotations)
                            = Deserializer.GetBuildingWithPartsAndAnnotations(buildingReader);

                buildingWithParts.AddRange(newBuildings);
                buildingAnnotations.AddRange(newBuildingAnnotations);
                surfaceAnnotations.AddRange(newSurfaceAnnotations);
            }
        }
        while (buildingReader.Read());

        buildingReader.Close();

        return (buildingWithParts, buildingAnnotations, surfaceAnnotations);
    }


    /// <summary>
    /// Returns <c>Surface</c>s and assiciated <c>SurfaceAnnotation</c>s within the passage to be deserialized.
    /// </summary>
    /// <param name="surfaceReader">Reader to process all subnodes within its XML node</param>
    /// <param name="surfaceType"></param>
    /// <returns>One Surface with its associated <c>SurfaceAnnotation</c>s</returns>
    private static (Surface, List<SurfaceAnnotation>) GetSurfaceAndSurfaceAnnotations(XmlReader surfaceReader, SurfaceType surfaceType)
    {
        // Determining Surface-CityGMLID
        string surfaceCityGMLID = null;
        surfaceReader.ReadToFollowing("gml:Polygon");

        if (surfaceReader.HasAttributes)
        {
            surfaceCityGMLID = surfaceReader.GetAttribute("gml:id");
        }

        Surface newSurface = new Surface(surfaceCityGMLID, surfaceType);

        /// Determining SurfacePolygonPoints
        (Surface surfaceWithPolygonPointsAndType, List<SurfaceAnnotation> surfaceAnnotations) = Deserializer.GetPolygonPointsAndSurfaceAnnotations(surfaceReader, newSurface);

        return (surfaceWithPolygonPointsAndType, surfaceAnnotations);
    }


    /// <summary>
    /// Returns <c>Surface</c> and assiciated <c>SurfaceAnnotation</c>s within the passage to be deserialized.
    /// </summary>
    /// <param name="polygonPointsReader">XmlReader as subtree of the "bldg:Building"-tag with the reader position set to the "gml:surfaceMeber"-tag</param>
    /// <returns><c>Surface</c> object and assicuated <c>SurfaceAnnotation</c>s</returns>
    private static (Surface, List<SurfaceAnnotation>) GetPolygonPointsAndSurfaceAnnotations(XmlReader polygonPointsReader, Surface surface)
    {
        surface.Polygon.Clear();
        List<SurfaceAnnotation> surfaceAnnotations = new List<SurfaceAnnotation>();

        int surfacePolygonPointIndex = -1;

        while (polygonPointsReader.Read())
        {
            if (polygonPointsReader.NodeType == XmlNodeType.Element)
            {
                switch (polygonPointsReader.Name)
                {
                    case "gml:pos":
                        try
                        {
                            string pointCoordinates = polygonPointsReader.ReadElementContentAsString();
                            double3 parsedPoint = Deserializer.ParseCoordinateString(pointCoordinates);

                            if (!surface.Polygon.Exists(existingPoint => (existingPoint.x == parsedPoint.x && existingPoint.y == parsedPoint.y && existingPoint.z == parsedPoint.z)))
                            {
                                surface.Polygon.Add(parsedPoint);
                                surfacePolygonPointIndex++;
                            }
                        }
                        catch (ArgumentException e)
                        {
                            Debug.LogError(e);
                        }
                        break;

                    case "annotation:SurfaceAnnotation":
                        AnnotationProperties annotationProperties = Deserializer.GetAnnotationProperties(polygonPointsReader);
                        
                        XmlReader surfaceAnnotationReader = polygonPointsReader.ReadSubtree();


                        if (!surfaceAnnotationReader.ReadToFollowing("annotation:RelativePositionBetweenBasePoints"))
                        {
                            throw new ArgumentException("Method requires XML tree beginning with the tag <annotation:RelativePositionBetweenBasePoints>.");
                        }

                        string relativePositionBetweenBasePointsAsString = surfaceAnnotationReader.ReadElementContentAsString();
                        double.TryParse(relativePositionBetweenBasePointsAsString, NumberStyles.Any, CultureInfo.InvariantCulture, out double relativePositionBetweenBasePoints);


                        if (!surfaceAnnotationReader.ReadToFollowing("annotation:HeightAboveBaseLine"))
                        {
                            throw new ArgumentException("Method requires XML tree beginning with the tag <annotation:HeightAboveBaseLine>.");
                        }

                        string heightAboveBaseLineAsString = surfaceAnnotationReader.ReadElementContentAsString();
                        double.TryParse(heightAboveBaseLineAsString, NumberStyles.Any, CultureInfo.InvariantCulture, out double heightAboveBaseLine);


                        AnnotationContent annotationComponents = Deserializer.GetSimpleTextAnnotation(surfaceAnnotationReader);
                        int annotationAnchorPointIndexes = surfacePolygonPointIndex;
                        SurfaceAnnotation surfaceAnnotation = new SurfaceAnnotation(surface, annotationAnchorPointIndexes, relativePositionBetweenBasePoints, heightAboveBaseLine, annotationComponents, annotationProperties);

                        surfaceAnnotations.Add(surfaceAnnotation);

                        surfaceAnnotationReader.Close();

                        // Read to next opening tag
                        polygonPointsReader.Read();

                        break;
                }
            }
        }

        polygonPointsReader.Close();

        return (surface, surfaceAnnotations);
    }


    /// <summary>
    /// Extracts a 3D coordinate from a single string
    /// </summary>
    /// <param name="coordinates">3D coordinate as a string</param>
    /// <returns></returns>
    public static double3 ParseCoordinateString(string coordinates)
    {
        /// Splitting the coordinate string the format "x y z" (e.g. "33311699.707 599549.332 23.705") with the space as separator
        string[] CoordValues = coordinates.Split(' ');

        /// Parsing the splited string values with the coordintes into doubles:
        if (
            double.TryParse(CoordValues[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double x) &&
            double.TryParse(CoordValues[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double y) &&
            double.TryParse(CoordValues[2], NumberStyles.Any, CultureInfo.InvariantCulture, out double z)
            )
        {
            return new double3(x, y, z);
        }
        else
        {
            throw new ArgumentException("Coordinates could not be parsed: " + coordinates);
        }
    }



    #region Annotations and Components


    /// <summary>
    /// Reads until one <c>AnnotationProperties</c> within the passage is found and returns it.
    /// <remarks>The XMLReader is NOT closed.</remarks>
    /// </summary>
    /// <param name="annotationReader">Reader to process all subnodes within its XML node</param>
    /// <returns><c>AnnotationProperties</c> object</returns>
    private static AnnotationProperties GetAnnotationProperties(XmlReader annotationReader)
    {
        string scaleWithCameraDistanceString = annotationReader.GetAttribute("ScaleWithCameraDistance");
        string scaleBySelectionString = annotationReader.GetAttribute("ScaleBySelection");
        string pointingDirectionString = annotationReader.GetAttribute("PointingDirection");

        bool.TryParse(scaleWithCameraDistanceString, out bool scaleWithCameraDistance);
        bool.TryParse(scaleBySelectionString, out bool ScaleBySelection);

        float3 pointingDirection = (float3)Deserializer.ParseCoordinateString(pointingDirectionString);

        return new AnnotationProperties(scaleWithCameraDistance, ScaleBySelection, pointingDirection);
    }


    /// <summary>
    /// Reads until one <c>BuildingAnnotation</c> within the passage is found and returns it.
    /// <remarks>The XMLReader is NOT closed after it has been processed in this method.</remarks>
    /// </summary>
    /// <param name="buildingAnnotationReader"></param>
    /// <param name="associatedBuilding"></param>
    /// <param name="annotationProperties"></param>
    /// <returns><c>BuildingAnnotation</c> object</returns>
    private static BuildingAnnotation GetBuildingAnnotation(XmlReader buildingAnnotationReader, Building associatedBuilding, AnnotationProperties annotationProperties)
    {
        TextAnnotationComponent annotationTextComponent = null;

        try
        {
            if (!buildingAnnotationReader.ReadToFollowing("annotation:BuildingAnnotation"))
            {
                throw new ArgumentException("Method requires XML tree beginning with the tag <annotation:BuildingAnnotation>.");
            }

            while (buildingAnnotationReader.Read())
            {
                if (buildingAnnotationReader.NodeType == XmlNodeType.Element)
                {
                    switch (buildingAnnotationReader.Name)
                    {
                        case "annotation:SimpleTextAnnotation":
                            XmlReader simpleTextAnnotationReader = buildingAnnotationReader.ReadSubtree();

                            annotationTextComponent = Deserializer.GetSimpleTextAnnotation(simpleTextAnnotationReader);

                            simpleTextAnnotationReader.Close();
                            break;

                            // Add further annotation content here
                            // anotationComponent = ...
                    }
                }
            }
        }
        catch (Exception e)
        {
            throw new InvalidDataException("Invalid WorldCoordinateAnnotation" + e);
        }

        return new BuildingAnnotation(associatedBuilding, annotationTextComponent, annotationProperties);
    }



    /// <summary>
    /// Reads the <c>WorldCoordinate</c>s ETRS/UTM coordinate and the <c>AnnotationContent</c> (within a WorldCoordinateAnnotation node) and returns it.
    /// <remarks>The XMLReader is NOT closed after it has been processed in this method.</remarks>
    /// </summary>
    /// <param name="worldCoordinateAnnotationReader">Reader of the WorldCoordinateAnnotation node</param>
    /// <returns>ETRS/UTM coordinate with the <c>AnnotationContent</c> object</returns>
    private static (double3, AnnotationContent) GetWorldCoordinateAnnotationContentAndCoordinates(XmlReader worldCoordinateAnnotationReader)
    {
        double3 umlCoordinates;
        AnnotationContent annotationComponent = null;

        try
        {
            if (worldCoordinateAnnotationReader.Name != "gml:pos")
            {
                throw new ArgumentException("Method requires XML tree beginning with the tag <gml:pos>.");
            }
            else
            {
                umlCoordinates = Deserializer.ParseCoordinateString(worldCoordinateAnnotationReader.ReadElementContentAsString());
            }


            while (worldCoordinateAnnotationReader.Read())
            {
                if (worldCoordinateAnnotationReader.NodeType == XmlNodeType.Element)
                {
                    switch (worldCoordinateAnnotationReader.Name)
                    {
                        case "annotation:SimpleTextAnnotation":
                            XmlReader simpleTextAnnotationReader = worldCoordinateAnnotationReader.ReadSubtree();

                            annotationComponent = Deserializer.GetSimpleTextAnnotation(simpleTextAnnotationReader);

                            simpleTextAnnotationReader.Close();
                            break;
                    }
                }
            }
        }
        catch (Exception e)
        {
            throw new InvalidDataException("Invalid WorldCoordinateAnnotation" + e);
        }

        return (umlCoordinates, annotationComponent);
    }







    /// <summary>
    /// Reads mutliple <c>TextAnnotationComponent</c>s within the passage and returns them or null if none was found.
    /// <remarks>The XMLReader is closed after it has been processed in this method.</remarks>
    /// </summary>
    /// <param name="simpleTextAnnotationReader">Reader of the SimpleTextAnnotation node</param>
    /// <returns><c>TextAnnotationComponent</c> object</returns>
    private static TextAnnotationComponent GetSimpleTextAnnotation(XmlReader simpleTextAnnotationReader)
    {
        // TODO: Missing while loop

        TextAnnotationComponent annotationTextComponent;

        if (!simpleTextAnnotationReader.ReadToFollowing("annotation:SimpleTextAnnotation"))
        {
            throw new ArgumentException("Method requires XML tree beginning with the tag <annotation:SimpleTextAnnotation>.");
        }

        if (!simpleTextAnnotationReader.ReadToFollowing("annotation:AnnotationTextComponent"))
        {
            throw new ArgumentException("Missing tag <annotation:AnnotationTextComponent>.");
        }
        else
        {
            XmlReader annotationTextComponentReader = simpleTextAnnotationReader.ReadSubtree();
            annotationTextComponent = Deserializer.GetAnnotationTextComponent(annotationTextComponentReader);
            annotationTextComponentReader.Close();
        }

        return annotationTextComponent;
    }


    /// <summary>
    /// Reads until one <c>TextAnnotationComponent</c> within the passage is found and returns it or null if none was found.
    /// <remarks>The XMLReader is NOT closed after it has been processed in this method.</remarks>
    /// </summary>
    /// <param name="annotationTextComponent">Reader of the AnnotationTextComponent node</param>
    /// <returns><c>TextAnnotationComponent</c> object</returns>
    private static TextAnnotationComponent GetAnnotationTextComponent(XmlReader annotationTextComponent)
    {
        if (!annotationTextComponent.ReadToFollowing("annotation:AnnotationTextComponent"))
        {
            throw new ArgumentException("Method requires XML tree beginning with the tag <annotation:AnnotationTextComponent>.");
        }

        if (!annotationTextComponent.ReadToFollowing("annotation:Text"))
        {
            throw new ArgumentException("Missing tag <annotation:Text>.");
        }
        string annotationText = annotationTextComponent.ReadElementContentAsString();

        if (!annotationTextComponent.ReadToFollowing("annotation:LocalScale"))
        {
            throw new ArgumentException("Mising tag <annotation:LocalScale>.");
        }
        string localScaleAsString = annotationTextComponent.ReadElementContentAsString();
        float.TryParse(localScaleAsString, NumberStyles.Any, CultureInfo.InvariantCulture, out float textSize);

        return new TextAnnotationComponent(annotationText, textSize);
    }


    #endregion


}

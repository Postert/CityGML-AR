using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;


/// <summary>
/// Custom class to determine the duration of starting time.
/// </summary>
public static class MyTimer
{
    /// <summary>
    /// <value>Time at which the app is initialized at runtime.</value>
    /// </summary>
    private static long StartTime;

    /// <summary>
    /// Initializes the start value of the timer when the class is created.
    /// </summary>
    public static void StartTimer()
    {
        StartTime = DateTime.Now.Ticks;
    }

    /// <summary>
    /// Calculates the seconds since the Timer class was instantiated.
    /// </summary>
    /// <returns>Seconds with formatting s.SSS</returns>
    public static float GetSecondsSiceStart()
    {
        float milliseconds = (DateTime.Now.Ticks - StartTime) / TimeSpan.TicksPerMillisecond;
        float seconds = milliseconds / 1000;
        return seconds;
    }

    /// <summary>
    /// Calculates the seconds since the Timer object was instantiated.
    /// </summary>
    /// <returns>Timestamp string with uniform Formatting</returns>
    public static string GetSecondsSiceStartAsString()
    {
        string seconds = "" + MyTimer.GetSecondsSiceStart();
        string formatedSeconds = seconds.PadLeft(5, '0');
        return "TimeStamp: " + formatedSeconds;
    }
}

/// <summary>
/// Detects targets predefined in the ReferenceImageLibrary and triggers deserialization and mesh and annotation generation
/// </summary>
[RequireComponent(typeof(ARTrackedImageManager))]
public class TargetDetector : MonoBehaviour
{
    private ARTrackedImageManager TrackedImageManager;

    private BuildingManager BuildingManager;
    private AnnotationManager AnnotationManager;

    public DatabaseService DatabaseService { get; private set; }

    /// <summary>
    /// <value>Indicates whether the database exists and has been completely initialized.</value>
    /// </summary>
    public bool IsInitialized { get; private set; } = false;

    /// <summary>
    /// <value>Contains the name of the last recognized target.</value>
    /// </summary>
    private string LastDetectedTarget;

    /// <summary>
    /// <value>Contains the targets' names as keys and the associated ETRS/UTM coordinate as the values.</value>
    /// </summary>
    private readonly Dictionary<string, double3> MyTargets = new Dictionary<string, double3>()
    {
        // ToDo: Enter target coordinates here! Note that the dictionary's key value must match the name of the associated target in the ReferenceImageLibrary
        { "Target1", new double3(33310555.001, 5995791.728, 31.356) },
        { "Target2", new double3(33310215.085, 5995811.903, 37.5f) }
    };

    /// <summary>
    /// Initializes the database, the ARfoundation's tracking components and the assiciated BuildingManager and AnnotationManager when the object is created by Unity.
    /// </summary>
    private async void Awake()
    {
        /// Initialization of assiciated objects
        DatabaseService = new DatabaseService(out bool databaseAlreadyExists, out string databasePathWithName);


        TrackedImageManager = GetComponent<ARTrackedImageManager>();

        BuildingManager = GameObject.Find("BuildingManagementGameObject").GetComponent<BuildingManager>();
        BuildingManager.DatabaseService = DatabaseService;

        AnnotationManager = GameObject.Find("AnnotationManagementGameObject").GetComponent<AnnotationManager>();
        AnnotationManager.DatabaseService = DatabaseService;

        MyTimer.StartTimer();
        Debug.Log(MyTimer.GetSecondsSiceStartAsString() + ": Unity initialized after " + Time.realtimeSinceStartup);
        Screen.sleepTimeout = SleepTimeout.NeverSleep;



        //        /// If executed in Unity Editor: Initialize database
        //
        //        bool dbWasPreviouslyInitialized = false;
        //
        //        if (PlayerPrefs.HasKey("isInitialized_BuildingDatabase")) // Player was initialized at some time
        //        {
        //            bool.TryParse(PlayerPrefs.GetString("isInitialized_BuildingDatabase"), out dbWasPreviouslyInitialized);
        //        }
        //
        //        Debug.Log(MyTimer.GetSecondsSiceStartAsString() + ": Database was once initialized: " + dbWasPreviouslyInitialized);
        Debug.Log(MyTimer.GetSecondsSiceStartAsString() + ": Database already exists: " + databaseAlreadyExists + " Database path: " + databasePathWithName);


        //        Debug.Log(MyTimer.GetSecondsSiceStartAsString() + ": Initialization of the database required:" +
        //            ((!databaseAlreadyExists) ? "\nDatabase has not yet been created or was deleted" : (!dbWasPreviouslyInitialized ? "\nDatabase has not been initialized (completely) yet" : "  not required")));


#if UNITY_EDITOR
        /// In case that the database was not completely initialized at the last start of the app or meanwhile manually deleted from the file system, it is recreated with the given CItyGML files.
        if (/*!dbWasPreviouslyInitialized ||*/ !databaseAlreadyExists)
        {
            PlayerPrefs.SetString("isInitialized_BuildingDatabase", "false");

            (List<Building> buildings, List<BuildingAnnotation> buildingAnnotations, List<SurfaceAnnotation> surfaceAnnotations, List<WorldCoordinateAnnotation> worldCoordinateAnnotations) import = await ImportCityGMLFilesFromRessourcesAsync();

            Debug.Log(MyTimer.GetSecondsSiceStartAsString() + ": Starting Initialization of the database");

            List<Building> buildings = import.buildings;
            List<BuildingAnnotation> buildingAnnotations = import.buildingAnnotations;
            List<SurfaceAnnotation> surfaceAnnotations = import.surfaceAnnotations;
            List<WorldCoordinateAnnotation> worldCoordinateAnnotations = import.worldCoordinateAnnotations;

            foreach (BuildingAnnotation buildingAnnotation in buildingAnnotations)
            {
                Debug.Log(buildingAnnotation.ToString());
            }

            foreach (SurfaceAnnotation surfaceAnnotation in surfaceAnnotations)
            {
                Debug.Log(surfaceAnnotation.ToString());
            }

            foreach (WorldCoordinateAnnotation worldCoordinateAnnotation in worldCoordinateAnnotations)
            {
                Debug.Log(worldCoordinateAnnotation.ToString());
            }



            try
            {
                DatabaseService.PrepareTabels();

                DatabaseService.BuildingToDatabase(buildings);
                DatabaseService.BuildingAnnotationToDatabase(buildingAnnotations);
                DatabaseService.SurfaceAnnotationToDatabase(surfaceAnnotations);
                DatabaseService.WorldCoordinateAnnotationToDatabase(worldCoordinateAnnotations);

                PlayerPrefs.SetString("isInitialized_BuildingDatabase", "true");

                Debug.Log(MyTimer.GetSecondsSiceStartAsString() + " Buildings and Annotations stored in database.");
            }
            catch (Exception e)
            {
                Debug.LogError(MyTimer.GetSecondsSiceStartAsString() + ": Initialization of the building database failed: " + e);
            }
        }

        //#else 
        //        if(!dbWasPreviouslyInitialized) throw new FileNotFoundException("Initialize the database by executing the App in the Unity Editor before compiling! Some entries meight be missing in the current database.");
        //        if(!databaseAlreadyExists) throw new FileNotFoundException("Initialize the database by executing the App in the Unity Editor before compiling! Database file is missing.");
#endif


        IsInitialized = true;

        Debug.Log("isInitialized (Awake): " + IsInitialized);
    }


    // TODO: Initialisierung in Scene und diese Methode auslagern
    public void InitializeDatabase()
    {

    }


    /// <summary>
    /// Deserializes all CityGML files within the Roussources/CityGML/ folder
    /// </summary>
    /// <returns>Lists with <c>Building</c>s, <c>BuildingAnnotation</c>s, <c>SurfaceAnnotation</c>s and <c>WorldCoordinateAnnotation</c>s</returns>
    private async Task<(List<Building>, List<BuildingAnnotation>, List<SurfaceAnnotation>, List<WorldCoordinateAnnotation>)> ImportCityGMLFilesFromRessourcesAsync()
    {
        List<Building> buildings = new List<Building>();
        List<BuildingAnnotation> buildingAnnotations = new List<BuildingAnnotation>();
        List<SurfaceAnnotation> surfaceAnnotations = new List<SurfaceAnnotation>();
        List<WorldCoordinateAnnotation> worldCoordinateAnnotations = new List<WorldCoordinateAnnotation>();

        /// Receive all files in the CityGML folder
        TextAsset[] cityGMLFiles = Resources.LoadAll<TextAsset>("CityGML");

        /// Initialize <c>StringReader</c>s file names
        StringReader[] stringReaders = new StringReader[cityGMLFiles.Length];
        string[] cityGMLFileNames = new string[cityGMLFiles.Length];

        for (int i = 0; i < cityGMLFiles.Length; i++)
        {
            stringReaders[i] = new StringReader(cityGMLFiles[i].text);
            cityGMLFileNames[i] = cityGMLFiles[i].name;
        }

        /// Desrialize files within the <completionlist cref="Deserializer"/>
        (List<Building>, List<BuildingAnnotation>, List<SurfaceAnnotation>, List<WorldCoordinateAnnotation>, BoundingBox)[] buildingListTupels
            = await Deserializer.GetBuildingsAndAnnotationsAsync(cityGMLFileNames, stringReaders);

        /// Combine the reveived lists with <c>Building</c>s, <c>BuildingAnnotation</c>s, <c>SurfaceAnnotation</c>s and <c>WorldCoordinateAnnotation</c>s to one for each type
        foreach ((List<Building> newBuildings, List<BuildingAnnotation> newBuildingAnnotations, List<SurfaceAnnotation> newSurfaceAnnotations, List<WorldCoordinateAnnotation> newWorldCoordinateAnnotations, BoundingBox boundingBox) in buildingListTupels)
        {
            buildings.AddRange(newBuildings);
            buildingAnnotations.AddRange(newBuildingAnnotations);
            surfaceAnnotations.AddRange(newSurfaceAnnotations);
            worldCoordinateAnnotations.AddRange(newWorldCoordinateAnnotations);
            //Debug.Log(MyTimer.GetSecondsSiceStartAsString() + ": " + cityGMLFiles[Array.IndexOf(buildingListTupels, (newBuildings, boundingBox))].name + " deserialised:\n" + boundingBox.ToString());
        }

        return (buildings, buildingAnnotations, surfaceAnnotations, worldCoordinateAnnotations);
    }


    // TODO: Bezeichnung der Targets ändern
#if UNITY_EDITOR
    /// <summary>
    /// Simulates detection of target 
    /// </summary>
    private void Start()
    {

        if (IsInitialized)
        {
            try
            {
                // TODO: LOD1 Test

                Dictionary<string, Building> buildingsWithinBoundingBox;
                buildingsWithinBoundingBox = BuildingManager.CreateBuildingMeshAroundAnchorPoint(MyTargets["Target2"]);
                AnnotationManager.CreateAnnotationsAroundAnchorPoint(MyTargets["Target2"], buildingsWithinBoundingBox);
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError(MyTimer.GetSecondsSiceStartAsString() + "Target detection simulation not available: " + e);
            }
        }
        else
        {
            Debug.Log(MyTimer.GetSecondsSiceStartAsString() + ": Database has not initialized yet. No querying possible.");
        }
    }
#endif

    private void OnEnable()
    {
        TrackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    private void OnDisable()
    {
        TrackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        Debug.Log("isInitialized (OnTrackedImagesChanged): " + IsInitialized);

        //        if (isInitialized)
        //        {
        foreach (ARTrackedImage trackedImage in eventArgs.added)
        {
            //Debug.Log(MyTimer.GetSecondsSiceStartAsString() + ": new Target detected");
            RepositionCityGMLObjects(trackedImage);
        }

        foreach (ARTrackedImage trackedImage in eventArgs.updated)
        {
            RepositionCityGMLObjects(trackedImage);
        }
        //        }
        //        else
        //        {
        //            throw new InvalidOperationException("Database not initialized");
        //        }
    }



    
    private void RepositionCityGMLObjects(ARTrackedImage trackedImage)
    {
        if (trackedImage.trackingState == TrackingState.Tracking)
        {
            string trackedTargetName = trackedImage.referenceImage.name;

            if (!MyTargets.ContainsKey(trackedTargetName))
            {
                throw new KeyNotFoundException("Target dictionary does not contain the target " + trackedImage.referenceImage.name);
            }
            else
            {
                if (!trackedTargetName.Equals(LastDetectedTarget))
                {
                    float3 targetUnityCoordinates = new float3(trackedImage.transform.position.x, trackedImage.transform.position.y, 0);

                    try
                    {
                        Dictionary<string, Building> buildingsWithinBoundingBox;
                        buildingsWithinBoundingBox = BuildingManager.CreateBuildingMeshAroundAnchorPoint(MyTargets[trackedTargetName]);
                        AnnotationManager.CreateAnnotationsAroundAnchorPoint(MyTargets[trackedTargetName], buildingsWithinBoundingBox);
                    }
                    catch (InvalidOperationException e)
                    {
                        Debug.LogError("Could not query database:\n" + e);
                    }
                    catch (ArgumentException e)
                    {
                        Debug.LogError("Building Mesh cannot be created:\n" + e);
                    }
                    LastDetectedTarget = trackedTargetName;
                }
            }

            BuildingManager.UpdateMeshPositionInUnity(trackedImage.transform.position, trackedImage.transform.rotation);
            AnnotationManager.UpdateAnnotationsPositionInUnity(trackedImage.transform.position, trackedImage.transform.rotation);
        }
    }
}
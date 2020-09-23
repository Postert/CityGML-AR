# CityGML-AR

The app uses targets to provide localization. Print all targets needed and add them to the target database **ReferenceImageLibrary** in the **Resources** folder. For correct functionality, specify the size of each printed target in the target database as well. Also, set the field of view of the virtual camera according to the hardware camera ones in the camera settings of Unity3D. The application uses ARFoundation, which utilizes *ARKit* (*iOS*) or *ARCore* (*Android*) depending on the target platform. Keep in mind that the app is only capable of running on devices that support one of these frameworks. 

## Getting Started
A detailed tutorial with an example data set is currently under construction. 

## Requirements
* Unity3D 2020.1.6f1 (with Android and iOS Build Support)
* Visual Studio Code or Visual Studio 2019
* ARCore (Android) or ARKit (iOS) compatible end device for testing


## Built With

* [Unity3D DepthMask](http://wiki.unity3d.com/index.php?title=DepthMask)
* [sqlite-unity-plugin](https://github.com/rizasif/sqlite-unity-plugin)


## License

[CC BY-NC-SA](https://creativecommons.org/licenses/by-nc-sa/4.0/)

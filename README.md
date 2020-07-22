# URack-Unity

URack is a project that combines the VCV Rack software modular synthesizer with the Unity game engine.
You can learn more about URack [here](https://eidetic.net.au/urack-docs/).

This repository hosts the Unity SDK.
For the VCV Rack (front-end) SDK, visit [URack-VCV](https://github.com/eidetic-av/URack-VCV).

# Quick start

To get set-up for development within Unity:

1. Create (or open) a Unity project that uses the Universal Render Pipeline.
2. Import this package into your project by adding this git repository to the Unity Package Manager.

To create a new module:

1. Create a new C# script in your Assets directory.
2. Import `Eidetic.URack`.
3. Extend the script from `UModule` instead of `MonoBehvaiour`.
4. Create a new GameObject with the same name as your script, appended with 'Prefab'.
  * For example, if your UModule script is named 'MyModule', the prefab should be called 'MyModulePrefab'.
  
The prefab will then be added to the scene when the corresponding module is spawned in VCV.

For more detailed information on this SDK, the URack SDK for VCV, and a guide on exporting your modules to share with the world, visit the [URack website](https://eidetic.net.au/urack-docs/).

## Acknowledgements

* Thanks [pardeike](https://github.com/pardeike) for the [Harmony](https://github.com/pardeike/Harmony) library, which is used to patch methods and properties at runtime.
It is licensed under the MIT license.

* The OSC message encoding and parsing is based on [this implementation](https://github.com/keijiro/unity-osc) by [keijiro](https://github.com/keijiro).
It is licensed under the MIT license.

* Packing and unpacking URack plugins uses the [Archiver-Unity](https://github.com/LightBuzz/Archiver-Unity) project by [Vangos Pternias](http://pterneas.com/) from [LightBuzz](http://lightbuzz.com/).
It is licensed under the Apache License 2.0.

## License

This code is licensed under GPLv3.0.

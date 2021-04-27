# URack-Unity

URack is a project that combines the VCV Rack software modular synthesizer with the Unity game engine.
You can learn more about URack [here](https://eidetic.net.au/urack/).

This repository hosts the Unity SDK.
For the VCV Rack (front-end) SDK, visit [URack-VCV](https://github.com/eidetic-av/URack-VCV).

# Quick start

To get set-up for development within Unity:

1. Create (or open) a Unity project that uses the High Definition Render Pipeline.
2. Import this package into your project by adding this git repository to the Unity Package Manager.
3. Change your player settings to use the .NET 4.x runtime.

To create a new module:

1. Create a new C# script in your Assets directory.
2. Import `Eidetic.URack`.
3. Extend the script from `UModule` instead of `MonoBehvaiour`.
  
A gameobject with the script will be added to the scene when the corresponding module is spawned in VCV.

For more detailed information on this SDK and the URack SDK for VCV, visit the [URack website](https://eidetic.net.au/urack/).

## Acknowledgements

* Thanks [pardeike](https://github.com/pardeike) for the [Harmony](https://github.com/pardeike/Harmony) library, which is used to patch methods and properties at runtime.
It is licensed under the MIT license.

* The OSC message encoding and parsing is based on [this implementation](https://github.com/keijiro/unity-osc) by [keijiro](https://github.com/keijiro).
It is licensed under the MIT license.

* Packing and unpacking URack plugins uses the [Archiver-Unity](https://github.com/LightBuzz/Archiver-Unity) project by [Vangos Pternias](http://pterneas.com/) from [LightBuzz](http://lightbuzz.com/).
It is licensed under the Apache License 2.0.

## License

This code is licensed under GPLv3.0.

![](https://github.com/unity-technologies/com.unity.editor.tasks/workflows/Build,%20Test,%20Pack/badge.svg)

# About the Tasks package

Unity.Editor.Tasks is a TPL-based (Task Parallel Library, or System.Threading.Tasks) task management library.

This repository is a subset of the functionality in [Git for Unity](https://github.com/Unity-Technologies/Git-for-Unity) repository, specifically the `Threading`, `OutputProcessors`, `Tasks`, `Process` and `IO` directories, as well as various helper classes found in [](https://github.com/Unity-Technologies/Git-for-Unity/tree/master/src/com.unity.git.api/Api).

It's been split up for easier testing and consumption by the Git package and any other packages, libraries or apps that wish to use it.



## The History

This library was originally written because Unity's old Mono C# profile/compilers did not support TPL and async/await, the Git client really needs to run on controlled background threads with some sort of exclusive locking mechanism, without the uncertainty of explicit async/await calls, and I really didn't have the time or the inclination to teach modern .NET developers how to code for an ancient Mono version.

The next best thing was to code modern .NET and use a version of the TPL library backported to .NET 3.5 (the highest that Unity's old mono supports) to have it running in Unity 5.6 and up. The nice thing about modern .NET is that it's pretty much all syntactic sugar. Ancient Mono versions can run the code just fine, they just can't compile it.

These days, Unity supports modern .NET and can compile all this code just fine, so this library no longer ships with .NET 3.5 support, but it still maintains its separation from Unity - the projects don't reference Unity, and any Unity integration code in this library is behind a `#if UNITY_EDITOR` define, so you can safely consume the nuget packages in any .NET environment, and Unity-specific functionality will only be available when you consume this library as a package in a Unity project.

## License

**[MIT](LICENSE)**

Copyright (c) 2019 Unity Technologies
Copyright (c) 2016-2019 Andreia Gaita
Copyright (c) 2016-2018 GitHub

# TeacherAPI

## Create your own Teacher for Baldi!

## Installation Tutorial
WIP
<!--- 
Using NuGet, add your package in your csproj. Right click on Dependencies > Manage NuGet Packages and install the last version.
![image](https://github.com/Sakyce/TeacherAPI/assets/55030860/262c5c14-4b3d-4cd3-949c-cdb760cb8ca0)

You can also add this as a PackageReference if you can't figure it out in your csproj file 
```xml--->
<!--- <PackageReference Include="Sakyce.TeacherAPI" Version="0.*" />
<PackageReference Include="Sakyce.TeacherAPI.Analyzers" Version="*" /> <!-- Not required but recommended -->
<!--- ```--->

<!--- If you want to already start with something, you can fork this [Example Teacher](https://github.com/Sakyce/TeacherExample) and create your own Teacher from it. (Very recommended!)
--->
## Reporting issues

> [!IMPORTANT]  
> Try to reproduce the error without the others mods installed to make sure that it is caused by TeacherAPI and not an unrelated mod! We don't want to debug your big modpack.

> [!WARNING]  
> We don't provide support for poor compatibility with proprietary/closed-source/poorly coded mods like Sugaku Modpack.
> We also don't provide support for outdated mods or mods marked as imcompatible.

If you have a Github Account, report the issue as usual. Although, we require you to do these steps first:

1. Make sure that you have enabled those settings in BepInEx.cfg in your installation. Restart your game to apply changes:
```toml
[Logging.Disk]
WriteUnityLog = true

[Logging.Console]
Enabled = true
```

2. Attach a screenshot the full console window with the logs in it. ![image](https://github.com/Sakyce/TeacherAPI/assets/55030860/1c016ff1-8f36-4669-adb5-96e5d5d5598c)

3. Attach a screenshot of the plugins folder like this: ![image](https://github.com/Sakyce/TeacherAPI/assets/55030860/874de35e-e3c1-424a-9d3f-06e1de870214)

4. Attach the LogOutput file from `BepInEx/LogOutput.log`. If you think the issue is from somewhere else, avoid posting first. Make sure that an error like this will actually appear in the file: (please note that the error doesn't come from TeacherAPI or its associated mods in this example). **Don't send any screenshot, send the file directly!** 
```
[Error  : Unity Log] ArgumentException: The Object you want to instantiate is null.
Stack trace:
Object UnityEngine.Object.Instantiate(Object original, Transform parent, bool instantiateInWorldSpace)
CursorController UnityEngine.Object.Instantiate<CursorController>(CursorController original, Transform parent, bool worldPositionStays)
CursorController UnityEngine.Object.Instantiate<CursorController>(CursorController original, Transform parent)
void CursorInitiator.Inititate()
void CursorInitiator.OnEnable()
UnityEngine.GameObject:AddComponent()
PineDebug.PineDebugManager:InitUI()
PineDebug.PineDebugManager:Initialize()
PineDebug.BasePlugin:Postload()
MTM101BaldAPI.MTM101BaldiDevAPI:OnSceneUnload()
MTM101BaldAPI.<ReloadScenes>d__19:MoveNext()
UnityEngine.SetupCoroutine:InvokeMoveNext(IEnumerator, IntPtr)
```

5. **Explain us how to reproduce the steps, what do you see, what it's supposed to happen.**
> [!TIP]  
> Spare us the work of searching which mod is incompatible by removing every mods, then add mods one by one until it breaks.
 
6. If you don't follow these steps correctly, your issue will be closed as not completed without any explaination.

<!---If you don't have Github, you can report it in the [issues section of the official continuation's Gamebanana submission](https://gamebanana.com/mods/issues/545498), still do the same steps as mentionned earlier or Baldi will get angry.

> [!NOTE]
> Your report on Discord can still be rejected if it badly follows the instructions. We will link you to this page as a warning.

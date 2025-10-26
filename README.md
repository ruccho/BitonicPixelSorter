# BitonicPixelSorter

GPU-accelerated pixel sorter with bitonic sorting for Unity.

The screenshot below shows it running on an NVIDIA GeForce RTX 4070 and it keeps over 1000 FPS at FHD resolution.

<img width="1920" height="1032" alt="Image" src="https://github.com/user-attachments/assets/8330a931-a737-4239-a60a-4bf2bf637961" />

## Installation

Use UPM git dependencies.
1. Open Package Manager and click `+` > `Add package from git URL...`
2. Enter `https://github.com/ruccho/BitonicPixelSorter.git?path=/Packages/com.ruccho.bitonicpixelsorter#release`

3. (Optional) To use a RendererFeature for UniversalRP, also install `https://github.com/ruccho/BitonicPixelSorter.git?path=/Packages/com.ruccho.bitonicpixelsorter.urp#release`


## BitonicPixelSorter Component

![image](https://user-images.githubusercontent.com/16096562/125492519-6a363ad6-87b3-451b-a6a3-37b859821db5.png)
|Property|Type|Description|
|-|-|-|
|Use As Image Effect|`bool`|It works as an image effect when attached to the camera. This is only active when you are using builtin render pipeline.|
|Shader|`ComputeShader`|Set `BoitonicPixelSorter.compute`.|
|Direction|`bool`|Switches sorting direction between horizontal / vertical.|
|Ascending|`bool`|Switches ordering.|
|Threshold Min|`float`|Lower threshold of the brightness.|
|Threshold Max|`float`|Upper threshold of the brightness.|

### Use from code

```csharp
var sorter = GetComponent<BitonicPixelSorter>();

//BitonicPixelSorter.Execute(Texture src, RenderTexture dst)
sorter.Execute(sourceTexture, destinationTexture);
```

## Use RendererFeature for UniversalRP

In your renderer asset, add `Bitonic Pixel Sorting Feature` to the renderer feature list.

## References

https://github.com/hiroakioishi/UnityGPUBitonicSort

https://www.inf.hs-flensburg.de/lang/algorithmen/sortieren/bitonic/oddn.htm

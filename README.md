# ⚠️ Unity 6.3 or later: This package is no longer necessary

Unity 6.3 officially supports hiding the development build watermark by setting [`UnityEngine.Rendering.Watermark.showDeveloperWatermark`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rendering.Watermark-showDeveloperWatermark.html) to `false`.

This package is only needed for Unity 6.2 and earlier.

---

Unity Remove Dev Watermark
===

Simply importing this package will remove the "Development Build" from the lower right corner of the screen.  
This has a number of advantages,
- Development Build allows you to easily shoot PVs and take screenshots for the app store.
- No UI overlap with the game's UI.
- (Anything else?)

![sample](https://github.com/kyubuns/UnityRemoveDevWatermark/assets/961165/21325692-6fbc-4eba-998d-d8db412b6572)

The following platforms are currently supported

- Windows (BuildTarget.StandaloneWindows, BuildTarget.StandaloneWindows64)
- macOS (BuildTarget.StandaloneOSX)
- iOS (BuildTarget.iOS)
- Android (BuildTarget.Android)

I hope this feature will be officially supported by Unity.  
But [this request thread](https://forum.unity.com/threads/remove-development-build-watermark-without-giving-up-the-build-option.944772/) has been neglected for more than 6 years!


## Installation

### UnityPackageManager

`https://github.com/kyubuns/UnityRemoveDevWatermark.git?path=Assets/RemoveDevWatermark`

## Target Environment

- Unity2021.3 or later

## License

MIT License (see [LICENSE](LICENSE))

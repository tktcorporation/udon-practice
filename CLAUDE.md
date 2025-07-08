# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a VRChat world development project using Unity 2022.3.22f1 and VRChat SDK 3.8.2. The project uses UdonSharp for scripting VRChat-compatible behaviors.

## Environment Setup

### Prerequisites
1. **Unity Hub**: Install Unity 2022.3.22f1 via Unity Hub
2. **VRChat Creator Companion (VCC)**: For managing VRChat SDK packages
3. **mise**: For managing development tools and dependencies

### Tool Configuration

#### mise (.mise.toml)
The project uses [mise](https://mise.jdx.dev/) for consistent development environment:
- **.NET SDK 6.0.424**: Required for Unity 2022.3 C# development
- **Node.js 22.17.0 LTS**: For tooling and scripts
- **openupm-cli**: For managing OpenUPM packages

Install mise and run `mise install` in the project root to set up these tools.

#### Package Management

1. **Unity Package Manager (UPM)**
   - Core Unity packages are defined in `/Packages/manifest.json`
   - OpenUPM registry configured for third-party packages
   - Currently includes NuGetForUnity from OpenUPM

2. **VRChat Package Manager (VPM)**
   - VRChat SDK packages managed via `/Packages/vpm-manifest.json`
   - Includes com.vrchat.worlds 3.8.2 and dependencies
   - Use VRChat Creator Companion for VPM package updates

3. **NuGet for Unity**
   - Configured via `/Assets/NuGet.config`
   - Packages installed to `/Assets/Packages/`
   - Currently no NuGet packages in use (empty packages.config)

### First-time Setup
```bash
# 1. Install mise (if not already installed)
curl https://mise.jdx.dev/install.sh | sh

# 2. Install development tools
mise install

# 3. Open project in Unity Hub with Unity 2022.3.22f1

# 4. VRChat SDK will be automatically resolved via VPM
```

## Key Commands

### Unity Development
- **Open in Unity**: Use Unity Hub to open the project with Unity 2022.3.22f1
- **Play Mode Testing**: Use Unity's Play mode with VRChat ClientSim for local testing
- **Build VRChat World**: File → Build Settings → Build (requires VRChat SDK panel configuration)

### Git Operations
- The repository has a comprehensive `.gitignore` for Unity/VRChat development
- Unity meta files are tracked (required for Unity projects)

## Architecture & Structure

### Core Directories
- `/Assets/` - All project assets and scripts
- `/Assets/Scenes/` - Unity scenes (main scene: VRCDefaultWorldScene.unity)
- `/Assets/UdonSharp/UtilityScripts/` - UdonSharp behavior scripts
- `/Assets/SerializedUdonPrograms/` - Compiled Udon programs (auto-generated)

### UdonSharp Scripts Pattern
All scripts follow this structure:
```csharp
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace UdonSharp.Examples.Utilities
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ScriptName : UdonSharpBehaviour
    {
        // Implementation
    }
}
```

### Key Script Types
- **PlayerModSetter.cs**: Modifies player movement (jump, speed, gravity)
- **InteractToggle.cs**: Interactive object toggling
- **GlobalToggleObject.cs**: Network-synchronized object states
- **MasterToggleObject.cs**: Master-only controls

## VRChat-Specific Considerations

1. **Networking**: Use appropriate `BehaviourSyncMode` for networked behaviors
2. **Player API**: Access player data through `VRCPlayerApi`
3. **Interactions**: Use `Interact()` method for player interactions
4. **Ownership**: Handle ownership for networked objects with `Networking.SetOwner()`

## Development Notes

- Always test multiplayer functionality with ClientSim
- Performance is critical for VR - optimize draw calls and polygon counts
- Use Unity Profiler for performance analysis
- Follow VRChat's community guidelines for world content
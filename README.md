# KeysPerSecond (Reactive Keyboard Overlay)

A lightweight, ultra-low-latency Reactive Keyboard Overlay desktop application written in **C# (.NET 8)** using **WPF** and **Win32 Raw Input API**. Specifically designed for high-speed rhythm gaming (*osu!*, *osu!mania*, *Quaver*, *Etterna*) and streaming with OBS/Discord.

---

## 🚀 Key Features

- **Win32 Raw Input (`WM_INPUT`) Engine:**
  - Bypasses slow `WH_KEYBOARD_LL` low-level hooks.
  - Uses `RegisterRawInputDevices` with `RIDEV_INPUTSINK` to capture background keyboard hardware events even when high-framerate fullscreen games hold foreground focus.
  - Receives messages via a dedicated hidden `HwndSource` message listener.
- **Zero-Allocation Hot Path:**
  - Uses `stackalloc Span<byte>` inside the `WM_INPUT` message loop to read raw input data without heap allocations.
  - Zero-boxing `O(1)` byte array lookup table (`_lookupTable[vkey]`) for dispatching key state changes.
  - Typematic repeat filtering: distinguishes physical key-down leading edges from OS repeat packets so slider holds don't artificially inflate your KPS.
- **Mathematically Accurate 1000ms Sliding Window KPS:**
  - High-performance circular ring buffer (`KpsSlidingWindow`) tracks `Stopwatch.GetTimestamp()` ticks.
  - Live KPS decays smoothly to `0` when idle, synchronized with WPF hardware rendering via `CompositionTarget.Rendering`.
- **True Click-Through Overlay Window:**
  - Frameless, transparent WPF window (`AllowsTransparency="True"`, `Background="Transparent"`).
  - P/Invoke `SetWindowLongPtr` applies `WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE` so mouse clicks pass straight through into games without stealing focus.
- **Reposition / Drag Mode:**
  - Press **`Ctrl + Shift + F10`** anytime to toggle click-through off.
  - In Edit Mode, a neon draggable border and control toolbar appear. Click and drag anywhere to reposition.
  - Automatically saves window coordinates (`Left`, `Top`) into `config.json`.
  - Press **`Ctrl + Shift + R`** to reset session counters, KPS, and peak Max KPS.
- **Hardware-Accelerated Glow & Tactile Switch Visuals:**
  - GPU-rendered glassmorphism styling, vibrant neon accent colors, and tactile key depression animations (`ScaleTransform` bounce and glow burst on press).

---

## 📁 Project Architecture

```
KeysPerSecond/
├── KeysPerSecond/
│   ├── Native/
│   │   └── NativeMethods.cs            # Win32 P/Invoke signatures, structs & constants (64/32-bit safe)
│   ├── Models/
│   │   ├── AppConfig.cs                # Configuration models (Window, Style, TrackedKeys)
│   │   └── KeyBindingConfig.cs         # Per-key binding model (Key, Label, VirtualKey, AccentColor)
│   ├── Services/
│   │   ├── KpsSlidingWindow.cs         # Zero-allocation circular ring buffer for 1000ms window
│   │   ├── KeyToVkHelper.cs            # Human-readable key name to Win32 Virtual Key mapper
│   │   ├── ConfigService.cs            # JSON config loader, serializer & normalizer
│   │   └── RawInputService.cs          # Raw Input registration, WM_INPUT dispatch & hotkey tracker
│   ├── ViewModels/
│   │   ├── ObservableObject.cs         # Lightweight INotifyPropertyChanged base
│   │   ├── KeyItemViewModel.cs         # Per-key reactive visual state & press counters
│   │   └── MainViewModel.cs            # Session state: CurrentKps, MaxKps, TotalPresses, EditMode
│   ├── Converters/
│   │   └── CommonConverters.cs         # XAML converters (KpsToWidth, Orientation, Brushes)
│   ├── MainWindow.xaml                 # Modern HUD overlay view with neon glow & key cards
│   ├── MainWindow.xaml.cs              # Window lifecycle, Win32 exStyles & render hook
│   └── config.json                     # Default runtime configuration
├── KeysPerSecond.Tests/
│   ├── KpsSlidingWindowTests.cs        # Exact sliding window timing & burst unit tests
│   ├── KeyToVkHelperTests.cs           # Virtual Key parsing tests
│   ├── ConfigServiceTests.cs           # Serialization & normalization tests
│   ├── NativeStructLayoutTests.cs      # Win32 struct memory layout verification
│   ├── MainViewModelTests.cs           # State updates & reset tests
│   ├── RawInputServiceTests.cs         # Input dispatch, repeat filtering & hotkey tests
│   └── WindowLifecycleTests.cs         # STA thread window initialization verification
└── KeysPerSecond.sln
```

---

## ⚙️ Configuration (`config.json`)

The application loads `config.json` automatically on startup. If missing, it will generate a default configuration:

```json
{
  "Window": {
    "Left": 200.0,
    "Top": 200.0,
    "Scale": 1.0,
    "Topmost": true,
    "IsLocked": true
  },
  "Style": {
    "AccentColor": "#00E5FF",
    "PressedColor": "#FF007F",
    "BackgroundColor": "#12131C",
    "BackgroundOpacity": 0.88,
    "BorderColor": "#2A2E45",
    "TextColor": "#FFFFFF",
    "KeyWidth": 68.0,
    "KeyHeight": 68.0,
    "KeySpacing": 10.0,
    "CornerRadius": 14.0,
    "ShowIndividualCounters": true,
    "ShowKpsCounter": true,
    "ShowMaxKps": true,
    "ShowTotalCounter": true,
    "Orientation": "Horizontal"
  },
  "ToggleEditModeKey": "Ctrl+Shift+F10",
  "ResetStatsKey": "Ctrl+Shift+R",
  "TrackedKeys": [
    { "Key": "Z", "Label": "Z", "VirtualKey": 90, "AccentColor": "#00E5FF" },
    { "Key": "X", "Label": "X", "VirtualKey": 88, "AccentColor": "#00E5FF" },
    { "Key": "C", "Label": "C", "VirtualKey": 67, "AccentColor": "#FF007F" },
    { "Key": "V", "Label": "V", "VirtualKey": 86, "AccentColor": "#FF007F" }
  ]
}
```

### Key Mapping Examples
- **osu! Standard (2 Keys):**
  ```json
  "TrackedKeys": [
    { "Key": "Z", "Label": "Z", "AccentColor": "#00E5FF" },
    { "Key": "X", "Label": "X", "AccentColor": "#FF007F" }
  ]
  ```
- **osu!mania 7K (S, D, F, Space, J, K, L):**
  ```json
  "TrackedKeys": [
    { "Key": "S" }, { "Key": "D" }, { "Key": "F" },
    { "Key": "Space", "Label": "SPC", "Width": 90 },
    { "Key": "J" }, { "Key": "K" }, { "Key": "L" }
  ]
  ```

---

## 🏃 Quick Start

### Prerequisites
- Windows 10 / 11 (x64 / x86)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or higher

### Build & Run
```powershell
# Clone and enter workspace
cd d:\KeysPerSecond

# Run the overlay
dotnet run --project KeysPerSecond
```

### Run Tests
```powershell
dotnet test
```

---

## ⌨️ Hotkeys & Controls

| Shortcut | Action | Description |
|---|---|---|
| `Ctrl + Shift + F10` | **Toggle Edit / Drag Mode** | Toggles click-through off, displaying the draggable frame and control toolbar. Press again to lock into click-through mode. |
| `Ctrl + Shift + R` | **Reset Metrics** | Resets KPS, Max KPS, and individual key stroke counters. |
| `Left Click + Drag` *(in Edit Mode)* | **Reposition Window** | Move the overlay anywhere on your screens. The new coordinates are automatically saved to `config.json`. |

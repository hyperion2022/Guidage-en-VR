Projet de fin d'études: Aide de guidage en réalité virtuelle pour les spectateurs extérieurs
============================================================================================

### Developers: Mihai ANCA, Eddie GERBAIS-NIEF, Hugo LABASTIE

### Client: Edwige CHAUVERGNE

## User Guide

This project implements a screen pointing direction detection method using your pointer. It is based on the hand detection of a Kinect device coupled with the HandPoseBarracuda project.

It detects when you are pointing at your screen with your index finger and allows you to highlight objects and place pings/tags in the scene.

You can introduce a 3D model of your tracked body in the scene in real time and break the isolation from other users by using hand gestures.

An installation tutorial (in French) is available here: https://www.youtube.com/watch?v=Vo7gqL1Ylao&list=PL2p0aggZPpCXgwYPFILJMbGsMjDVhTx7n

3 demo videos are also available here:
- https://youtu.be/3mrPiO7Izbg?si=TqAnH0tzmxjRZ8YI
- https://youtu.be/3LOeghUUq1E?si=M4hZ5L9VA3i7DYoC
- https://youtu.be/51O50HOYqTQ?si=LVh63zBdTX8y6qf0

### Prerequisites

- Unity version 2022.3.19f1 or later
- Kinect v2 (SDK to install: https://www.microsoft.com/en-us/download/details.aspx?id=44561)

------------------------
### Prefab: KinectHandle

Manages the Kinect.

| Properties  | Type   |                                             |
|-------------|--------|---------------------------------------------|
| Enable Logs | `bool` | Kinect state will be printed to the console |

---------------------------
### Scene: Calibration Step

This scene can be launched and will perfom a calibration step. The calibration is saved in a .json file. The calibration is there to determine where the screen is positioned in space. As long as the Kinect and the screen are not moved relative to each other, the saved calibration will remain valid.

The instructions are given as text. The Kinect has to be connected. The validation button can be activated with the space bar or with the right mouse button, even when the mouse is not over it.

---------------------------------
### Interface: BodyPointsProvider

The KinectHandle prefab inherits from an abstract class called BodyPointsProvider. The KinectHandle is not the only one inheriting from it. Any object that produces real-time body points tracking may implement it (or inherit from it).


| Members     |                     |                                                        |
|-------------|---------------------|--------------------------------------------------------|
| Method      | `GetBodyPoint`      | takes `BodyPoint`, returns `(PointState, Vector3)`     |
| Enumeration | `BodyPoint`         | List of all body points: `Head`, `LeftWrist`, `RightIndex`, etc... |
| Enumeration | `PointState`        | `Tracked`, `Inferred`, `NotTracked`, `NotProvided`     |
| Event       | `BodyPointsChanged` | This event is raised every time the points positions or states change |

--------------------------------
### Prefab: BodyPointsVisualizer

Reads body points from a BodyPointsProvider and animates a 3D model.

| Properties           | Type   |                                                    |
|----------------------|--------|----------------------------------------------------|
| BodyPointsProvider | `BodyPointsProvider` | An object that produces body points  |

--------------------------
### Prefab: ScreenPointing

Loads the saved calibration from the .json file. Then, from a BodyPointsProvider, determines where the user is pointing on the screen with its index finger. It also considers the mouse as a means of pointing.

| Properties            | Type     |                                                        |
|-----------------------|----------|--------------------------------------------------------|
| TargetCamera          | `Camera` | Which camera is the point of view of the pointing user |
| CalibrationFilePath   | `String` | File path to the saved calibration                     |
| SmoothFactor          | `float`  | A factor between `0.` and `1.` of how much to smoothen the pointing position over time |

| Members     |                    | type           |                            |
|-------------|--------------------|----------------|----------------------------|
| Getter      | `pointing.mode`    | `PointingMode` |                            |
| Getter      | `pointing.atPixel` | `Vector2`      | Screen position in pixels  |
| Getter      | `pointing.atNorm`  | `Vector2`      | Normalized screen position |
| Enumeration | `PointingState`    |                | `None`, `Body`, `Mouse`    |

This prefab comes with several components that make use of the pointing:

#### CursorFeedback

Gives a pointing feedback by showing a cursor where the pointing position is inferred on the screen.

#### ObjectHighlighter

Highlights the pointed object in the scene, just by hovering over it or by clicking on it.

| Properties | Type     |                                                                      |
|------------|----------|----------------------------------------------------------------------|
| KeyCodes   | `List<KeyCode>` | Which key press triggers object selection |
| Hovering   | `bool`   | While an object is only pointed at, it is highlighted                |
| SizeLimit  | `float`  | The highlight will ignore any object with a size exceeding the limit |

#### PingManager

Places tags (or pings) in the scene where the user is pointing. Note that in order for the pings to be seen from another camera, it is necessary to also add the PingLayer prefab (see below).

| Properties | Type            |                                                 |
|------------|-----------------|-------------------------------------------------|
| KeyCodes   | `List<KeyCode>` | Which key press triggers ping placement/removal |

---------------------
### Prefab: PingLayer

Shows the pings placed by a Ping Manager to a camera.

| Properties    | Type          |                                                         |
|---------------|---------------|---------------------------------------------------------|
| PingManager   | `PingManager` | The PingManager from which we want the pings to appear  |
| TargetCamera  | `Camera`      | The camera from which the pings have to appear          |

------------------------------
### Prefab: BodyPointsRecorder

Given a BodyPointsProvider, it records the body points while the scene is playing and saves them in a .json file when it stops.

| Properties           | Type                 |                                              |
|----------------------|----------------------|----------------------------------------------|
| BodyPointsProvider   | `BodyPointsProvider` | An object that produces body points          |
| CapturesPerSecond    | `float`              | The frequency of capture                     |
| OutputFilePath       | `String`             | File path where to write the recorded points |

------------------------------
### Prefab: BodyPointsReplayer

Implements BodyPointsProvider. Produces body points by reading previously saved ones from the given .json file.

| Properties           | Type                 |                                             |
|----------------------|----------------------|---------------------------------------------|
| InputFilePath        | `String`             | File path where to read the recorded points |

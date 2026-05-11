# 2D Networked Co-Op Platformer

A 2D multiplayer platformer built in Unity to demonstrate real-time networking concepts. This project utilizes a Listen-Server architecture and Unity Relay to connect two players securely over the internet without the need for manual port forwarding. Players must work together, manage gravity inversion, and collect coins to advance through the levels.

## Prerequisites
* **Unity Editor:** Version 6000.4.3f1 (Unity 6) or later.
* **Packages Used:** Unity Netcode for GameObjects (NGO), Multiplayer Tools.

## Getting Started

To explore the source code or run the project locally:

1. Clone this repository to your local machine:
   `git clone [Your Repository URL Here]`
2. Open Unity Hub, click **Add**, and select the cloned folder.
3. Open the project using the required Unity version.
4. In the Project window, navigate to `Assets > Scenes` and open the `MainMenu` scene.

## Testing Multiplayer Locally

Because this is a networked game, pressing "Play" in the Unity Editor will only launch a single instance. To test the client-server interaction on one machine:

**Build and Run another instance**
1. Go to `File > Build Settings`.
2. Ensure all scenes are added to the "Scenes in Build" list.
3. Click **Build**, select an empty folder, and let Unity compile the `.exe`.
4. Run the newly built `.exe` (this will act as Player 1 / Host).
5. Press "Play" in the Unity Editor (this will act as Player 2 / Client).

## Controls
* **W / S:** Jump / Fall down
* **A / D:** Move Left / Right
* **Spacebar:** Invert Gravity (Note: Your W/S inputs will flip too!)
* **R:** Manual Respawn

## In Game Goals
* Avoid hazards
* Collect Coins to advance to the next level

## Technical Architecture

* **Networking Framework:** Unity Netcode for GameObjects (NGO)
* **Topology:** Listen-Server Model (Player 1 acts as both the authoritative Server and a local Client).
* **Connection Routing:** Unity Relay Services (Handles NAT punch-through to bypass local firewalls).
* **State Synchronization:** Handled via Network Variables (e.g., syncing exact X/Y coordinates, level timers, and death counts).
* **Action Requests:** Handled via Remote Procedure Calls (RPCs). Clients send `ServerRPCs` to request movements/actions, and the Server sends `ClientRPCs` to broadcast momentary events like death visuals or coin collection.

## Asset Credits
* **Night Background:** [Insert Creator Name] via OpenGameArt.org
* **Seamless HD Landscape:** [Insert Creator Name] via OpenGameArt.org
* **Tiles and Sprites:** Hyptosis via OpenGameArt.org
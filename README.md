# Group28 Database Connectivity Prototype

## Capstone Title
SAVORPINOY: A 2D INTERACTIVE CULINARY SIMULATOR FOR PRESERVING CALABARZON TRADITIONAL FOOD HERITAGE

## Prototype Feature
Main Menu UI Navigation and Batangas Stage: Interactive Ingredient Assembly & Preparation Sequence for "Adobo sa Dilaw"

## Group Members
1. Markus Johann L. Catacutan
2. Alano, Jairus Reive
3. Gaile Dichoso

## Description
This prototype demonstrates client-server database connectivity. The player begins on the customized SavorPinoy Main Menu using authentic graphic assets, transitions to the Batangas culinary stage upon clicking PLAY, selects authentic traditional ingredients (Luyang Dilaw, Garlic, Meat), calculates culinary scores, saves performance records to Firebase Realtime Database, and retrieves high scores to display on the leaderboard.

## Tools Used
- Unity Editor (2D Core)
- C#
- Firebase Realtime Database
- Figma (UI/Sprite Design)
- GitHub Desktop

## Database Used
Firebase Realtime Database

## Data Saved
- player_name
- score
- dish_name
- level
- remarks
- created_at

## How to Run
1. Clone the repository and open the project in Unity Editor.
2. Verify that the Android platform build target is selected.
3. Open `Assets/Scenes/MainMenu.unity`.
4. Press Play.
5. Click the custom PLAY image button to navigate to the cooking stage.
6. Enter a player nickname.
7. Select authentic ingredients and click "Cook Dish".
8. Click "Save Score" to push the record to Firebase.
9. Click "Load Leaderboard" to retrieve and view saved database entries.

## Database Flow
The Unity client tracks user choices in memory, computes a performance score, and pushes a serialized JSON record to the Firebase Realtime Database under the `/player_scores` node. On request, Unity queries the node by score, receives a DataSnapshot, parses it, and dynamically renders the leaderboard on the interface.

## Repository Access
The instructor GitHub account gracheleliza was added as collaborator.

## Known Limitations
Classroom prototype scope only. Advanced mechanics (knife slicing physics, jeepney travel map hub, and daily nutritional habit tracking) are slated for future iterations.

## References
- Firebase Unity SDK Documentation
- UPHSL 1stBIT4120L Technical Guide for Activity 5

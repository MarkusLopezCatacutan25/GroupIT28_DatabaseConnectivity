using UnityEngine;

[CreateAssetMenu(menuName="SavorPinoy/Firebase Settings")]
public sealed class FirebaseSettings : ScriptableObject
{
    public string databaseUrl="https://savorpinoy-database-daf6f-default-rtdb.asia-southeast1.firebasedatabase.app/";
    [Tooltip("Editor-only. Never routes a shipped player to localhost.")]
    public bool useLocalEmulators;
    public string emulatorProjectId="demo-savorpinoy-vertical-slice";
}

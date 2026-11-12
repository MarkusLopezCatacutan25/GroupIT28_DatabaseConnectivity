using System;
using UnityEngine;

// In-memory access ticket issued only after the visible sign-in flow succeeds.
public static class ChefAccessSession
{
    private static IChefIdentity identity;
    private static string uid;
    public static string DisplayName { get; private set; }
    public static bool HasAccess=>identity!=null&&!string.IsNullOrEmpty(uid)&&identity.Uid==uid;
    public static void Authorize(IChefIdentity authenticatedIdentity,string displayName)
    {
        if(authenticatedIdentity==null||string.IsNullOrEmpty(authenticatedIdentity.Uid)||!AttemptContract.ValidName(displayName))throw new InvalidOperationException("A confirmed identity is required before entering the game.");
        identity=authenticatedIdentity;uid=identity.Uid;DisplayName=displayName;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Clear(){identity=null;uid=null;DisplayName=null;}
}

using Unity.Netcode.Components;
using UnityEngine;

[DisallowMultipleComponent]
public class ClientNetworkTransform : NetworkTransform
{
    // Baris sakti ini berfungsi mencabut hak otoritas dari Server 
    // dan memberikannya kepada masing-masing Client
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
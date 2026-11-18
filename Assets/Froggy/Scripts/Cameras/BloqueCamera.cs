using UnityEngine;

namespace Froggy.Cameras
{
    /// <summary>
    /// Marque ce qui arrête CameraOrbitale (ses colliders et ceux de ses enfants) : le sol et les murs d'une arène, pas
    /// le petit décor, que la caméra traverse comme dans WoW. Ne fait rien d'autre.
    /// </summary>
    [DisallowMultipleComponent]
    public class BloqueCamera : MonoBehaviour
    {
    }
}

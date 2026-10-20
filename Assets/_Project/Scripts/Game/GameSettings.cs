using UnityEngine;

namespace EyesOff.Game
{
    /// <summary>
    /// All tunable game values in one asset, so designers change numbers without touching code or the scene.
    /// Create via Assets > Create > EyesOff > Game Settings.
    /// </summary>
    [CreateAssetMenu(menuName = "EyesOff/Game Settings", fileName = "GameSettings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Phases (seconds)")]
        [Tooltip("Shooter is frozen, room stays dark.")]
        public float panicDuration = 3f;
        [Tooltip("Lights are on.")]
        public float lightDuration = 5f;
        [Tooltip("Whole match length; the result screen is shown afterwards.")]
        public float matchDuration = 120f;

        [Header("Outline vision (metres from the camera)")]
        [Tooltip("Closer than this: outline at full strength.")]
        public float outlineFullDistance = 2f;
        [Tooltip("Farther than this: no outline.")]
        public float outlineMaxDistance = 5f;

        [Header("Lighting")]
        public Color litAmbientColor = new(0.35f, 0.35f, 0.4f);
    }
}

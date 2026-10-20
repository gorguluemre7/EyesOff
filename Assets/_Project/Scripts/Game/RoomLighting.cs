using UnityEngine;

namespace EyesOff.Game
{
    /// <summary>
    /// Switches the room between "dark" (no lights, black ambient, proximity outlines on)
    /// and "lit" (lights on, outlines off). Purely visual: every client runs it locally
    /// when the phase changes.
    /// </summary>
    public class RoomLighting : MonoBehaviour
    {
        private static readonly int OutlineStrength = Shader.PropertyToID("_OutlineStrength");
        private static readonly int OutlineNear = Shader.PropertyToID("_OutlineNear");
        private static readonly int OutlineFar = Shader.PropertyToID("_OutlineFar");

        [SerializeField] private GameSettings settings;
        [SerializeField] private Light[] roomLights;
        [Tooltip("State used before the game starts (e.g. in the menu).")]
        [SerializeField] private bool startLit = true;

        public bool IsLit { get; private set; }

        private void Awake()
        {
            Shader.SetGlobalFloat(OutlineNear, settings.outlineFullDistance);
            Shader.SetGlobalFloat(OutlineFar, settings.outlineMaxDistance);
            SetLit(startLit);
        }

        public void SetLit(bool lit)
        {
            IsLit = lit;

            foreach (Light roomLight in roomLights)
                roomLight.enabled = lit;

            // Requires Lighting window > Environment > Environment Lighting Source = Color.
            RenderSettings.ambientLight = lit ? settings.litAmbientColor : Color.black;

            Shader.SetGlobalFloat(OutlineStrength, lit ? 0f : 1f);
        }

        // Right-click the component header in Play mode to test without the phase flow.
        [ContextMenu("Toggle Lit")]
        private void ToggleLit() => SetLit(!IsLit);

        private void OnDestroy()
        {
            // Global shader values outlive play mode in the editor; don't leave outlines on.
            Shader.SetGlobalFloat(OutlineStrength, 0f);
        }
    }
}

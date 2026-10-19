using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EyesOff.Player
{
    /// <summary>
    /// Marker placed in the scene. Each player picks the point whose index matches its client id
    /// (host = 0, client = 1), so both machines agree on who spawns where.
    /// </summary>
    public class SpawnPoint : MonoBehaviour
    {
        private static readonly List<SpawnPoint> All = new();

        [SerializeField] private int index;

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        public static SpawnPoint Get(ulong clientId)
        {
            if (All.Count == 0)
                return null;

            int wanted = (int)(clientId % (ulong)All.Count);
            return All.FirstOrDefault(p => p.index == wanted) ?? All[0];
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position + Vector3.up, 0.5f);
            Gizmos.DrawRay(transform.position + Vector3.up, transform.forward);
        }
    }
}

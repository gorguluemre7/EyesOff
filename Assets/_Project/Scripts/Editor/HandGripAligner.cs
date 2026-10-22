using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace EyesOff.EditorTools
{
    /// <summary>
    /// Copies the hand's current rotation onto its IK grip target, so that switching
    /// Target Rotation Weight to 1 keeps the hand where it is instead of snapping or flipping.
    ///
    /// Usage (in Play mode, Target Rotation Weight = 0): select RightHandGrip or LeftHandGrip
    /// and run EyesOff > Align Grip To Hand. Then set the weight to 1 and fine-tune the grip.
    /// </summary>
    public static class HandGripAligner
    {
        [MenuItem("EyesOff/Align Grip To Hand")]
        private static void Align()
        {
            Transform grip = Selection.activeTransform;
            if (grip == null)
            {
                Debug.LogWarning("Select a hand grip (IK target) first.");
                return;
            }

            // Find the Two Bone IK constraint that uses this grip as its target.
            foreach (TwoBoneIKConstraint ik in Object.FindObjectsByType<TwoBoneIKConstraint>(FindObjectsSortMode.None))
            {
                if (ik.data.target != grip)
                    continue;

                Undo.RecordObject(grip, "Align Grip To Hand");
                grip.rotation = ik.data.tip.rotation;
                Debug.Log($"{grip.name} now matches the rotation of {ik.data.tip.name}.");
                return;
            }

            Debug.LogWarning($"No Two Bone IK constraint uses {grip.name} as its target.");
        }
    }
}

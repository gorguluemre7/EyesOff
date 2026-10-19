using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EyesOff.EditorTools
{
    /// <summary>
    /// The character pack builds a character from separate parts (body, head, hair, clothes...),
    /// and every part carries its own copy of the skeleton. An Animator can only drive one skeleton,
    /// so this tool rebinds every part to the body's skeleton and deletes the duplicate skeletons.
    ///
    /// Usage: select the character root (e.g. the "100" prefab instance) and run EyesOff > Merge Character Skeleton.
    /// </summary>
    public static class CharacterSkeletonMerger
    {
        private const string BodyKeyword = "Body";

        [MenuItem("EyesOff/Merge Character Skeleton")]
        private static void Merge()
        {
            GameObject root = Selection.activeGameObject;
            if (root == null)
            {
                Debug.LogWarning("Select the character root in the Hierarchy first.");
                return;
            }

            // Prefab instances don't allow deleting their children, so unpack first.
            if (PrefabUtility.IsPartOfPrefabInstance(root))
            {
                PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(root),
                    PrefabUnpackMode.Completely, InteractionMode.UserAction);
            }

            SkinnedMeshRenderer[] renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            SkinnedMeshRenderer body = renderers.FirstOrDefault(r => r.name.Contains(BodyKeyword));
            if (body == null)
            {
                Debug.LogError($"No SkinnedMeshRenderer with '{BodyKeyword}' in its name under {root.name}.");
                return;
            }

            Transform mainArmature = TopMostBelow(RootBoneOf(body), PartRootOf(body.transform, root.transform));

            // Collect everything before changing the hierarchy.
            var oldArmatures = renderers
                .Where(r => r != body)
                .ToDictionary(r => r, r => TopMostBelow(RootBoneOf(r), PartRootOf(r.transform, root.transform)));
            Transform[] partRoots = root.transform.Cast<Transform>().ToArray();

            // Put the body skeleton directly under the root so the hierarchy matches the body FBX
            // (Root/Armature/Hips...), which is what the body's humanoid Avatar expects.
            Undo.SetTransformParent(mainArmature, root.transform, "Merge Character Skeleton");

            var bones = new Dictionary<string, Transform>();
            foreach (Transform bone in mainArmature.GetComponentsInChildren<Transform>(true))
                bones.TryAdd(bone.name, bone);

            foreach (SkinnedMeshRenderer r in renderers)
            {
                if (r != body && oldArmatures[r] != mainArmature)
                {
                    AdoptExtraBones(oldArmatures[r], bones);

                    Undo.RecordObject(r, "Merge Character Skeleton");
                    r.bones = r.bones.Select(b => bones[b.name]).ToArray();
                    r.rootBone = bones[RootBoneOf(r).name];

                    Undo.DestroyObjectImmediate(oldArmatures[r].gameObject);
                }

                Undo.SetTransformParent(r.transform, root.transform, "Merge Character Skeleton");
            }

            // The part roots are empty now.
            foreach (Transform part in partRoots)
            {
                if (part != null && part != mainArmature && part.GetComponent<SkinnedMeshRenderer>() == null
                    && part.childCount == 0)
                {
                    Undo.DestroyObjectImmediate(part.gameObject);
                }
            }

            Debug.Log($"Merged {renderers.Length} meshes onto the '{mainArmature.name}' skeleton of {root.name}.");
        }

        /// <summary>Bones that only exist in a part (e.g. hair bones) are moved onto the main skeleton.</summary>
        private static void AdoptExtraBones(Transform oldArmature, Dictionary<string, Transform> bones)
        {
            // GetComponentsInChildren returns parents before children.
            foreach (Transform bone in oldArmature.GetComponentsInChildren<Transform>(true))
            {
                if (bone == oldArmature || bones.ContainsKey(bone.name))
                    continue;

                if (bones.TryGetValue(bone.parent.name, out Transform newParent) && bone.parent != newParent)
                    Undo.SetTransformParent(bone, newParent, "Merge Character Skeleton");

                bones[bone.name] = bone;
            }
        }

        private static Transform RootBoneOf(SkinnedMeshRenderer r) => r.rootBone != null ? r.rootBone : r.bones[0];

        /// <summary>The direct child of <paramref name="root"/> that contains <paramref name="t"/>.</summary>
        private static Transform PartRootOf(Transform t, Transform root)
        {
            while (t.parent != null && t.parent != root)
                t = t.parent;
            return t;
        }

        /// <summary>The ancestor of <paramref name="t"/> (or itself) whose parent is <paramref name="parent"/>.</summary>
        private static Transform TopMostBelow(Transform t, Transform parent)
        {
            while (t.parent != null && t.parent != parent)
                t = t.parent;
            return t;
        }
    }
}

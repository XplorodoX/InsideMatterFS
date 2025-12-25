using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

namespace VR.Hands
{
    public class HandAnimationFromController : MonoBehaviour
    {
        [Header("Inputs")]
        [SerializeField]
        XRInputValueReader<float> m_TriggerInput = new XRInputValueReader<float>("Trigger");

        [SerializeField]
        XRInputValueReader<float> m_GripInput = new XRInputValueReader<float>("Grip");

        [Header("Settings")]
        [SerializeField]
        float m_ClosedFingerRotation = 70f;
        [SerializeField]
        float m_AnimationSpeed = 10f;

        [Header("Bones - Index (Trigger)")]
        public List<Transform> indexFingerJoints = new List<Transform>();

        [Header("Bones - Grip Fingers")]
        public List<Transform> middleFingerJoints = new List<Transform>();
        public List<Transform> ringFingerJoints = new List<Transform>();
        public List<Transform> pinkyFingerJoints = new List<Transform>();
        public List<Transform> thumbJoints = new List<Transform>();

        // Store initial rotations
        private Dictionary<Transform, Quaternion> m_InitialRotations = new Dictionary<Transform, Quaternion>();

        private void Start()
        {
            StoreInitialRotations(indexFingerJoints);
            StoreInitialRotations(middleFingerJoints);
            StoreInitialRotations(ringFingerJoints);
            StoreInitialRotations(pinkyFingerJoints);
            StoreInitialRotations(thumbJoints);
        }

        private void StoreInitialRotations(List<Transform> joints)
        {
            foreach (var joint in joints)
            {
                if (joint != null && !m_InitialRotations.ContainsKey(joint))
                    m_InitialRotations[joint] = joint.localRotation;
            }
        }

        private void OnEnable()
        {
            m_TriggerInput?.EnableDirectActionIfModeUsed();
            m_GripInput?.EnableDirectActionIfModeUsed();
        }

        private void OnDisable()
        {
            m_TriggerInput?.DisableDirectActionIfModeUsed();
            m_GripInput?.DisableDirectActionIfModeUsed();
        }

        private void Update()
        {
            float triggerValue = m_TriggerInput != null ? m_TriggerInput.ReadValue() : 0f;
            float gripValue = m_GripInput != null ? m_GripInput.ReadValue() : 0f;

            // Animate Index with Trigger
            AnimateFinger(indexFingerJoints, triggerValue);

            // Animate others with Grip
            AnimateFinger(middleFingerJoints, gripValue);
            AnimateFinger(ringFingerJoints, gripValue);
            AnimateFinger(pinkyFingerJoints, gripValue);
            
            // Simple Thumb animation (gripping partially closes thumb)
            AnimateFinger(thumbJoints, gripValue * 0.5f);
        }

        private void AnimateFinger(List<Transform> joints, float value)
        {
            foreach (var joint in joints)
            {
                if (joint == null) continue;

                if (m_InitialRotations.TryGetValue(joint, out Quaternion initialRot))
                {
                    // Rotate around X local axis usually for fingers
                    // Adjust axis if models differ (XR Hands usually Z or X depending on basis)
                    // Let's assume Z-forward, X-Right, Y-Up. Curl is usually X.
                    // If it looks wrong, we might need to invert or change axis.
                    
                    Quaternion targetRot = initialRot * Quaternion.Euler(value * m_ClosedFingerRotation, 0, 0); 
                    joint.localRotation = Quaternion.Slerp(joint.localRotation, targetRot, Time.deltaTime * m_AnimationSpeed);
                }
            }
        }

        [ContextMenu("Auto Setup Bones")]
        public void AutoSetupBones()
        {
            indexFingerJoints.Clear();
            middleFingerJoints.Clear();
            ringFingerJoints.Clear();
            pinkyFingerJoints.Clear();
            thumbJoints.Clear();

            // Recursive search
            FindJointsRecursive(transform);
        }

        private void FindJointsRecursive(Transform current)
        {
            string name = current.name.ToLower();

            if (name.Contains("index")) AddJoint(indexFingerJoints, current);
            else if (name.Contains("middle")) AddJoint(middleFingerJoints, current);
            else if (name.Contains("ring")) AddJoint(ringFingerJoints, current);
            else if (name.Contains("pinky") || name.Contains("little")) AddJoint(pinkyFingerJoints, current);
            else if (name.Contains("thumb")) AddJoint(thumbJoints, current);

            foreach (Transform child in current)
            {
                FindJointsRecursive(child);
            }
        }

        private void AddJoint(List<Transform> list, Transform t)
        {
             // Usually we act on Proximal, Intermediate, Distal. 
             // We skip Metacarpals for curling if possible, or include them lightly.
             // XR Hands names: IndexProximal, IndexIntermediate, IndexDistal.
             if (t.name.Contains("Proximal") || t.name.Contains("Intermediate") || t.name.Contains("Distal"))
             {
                 list.Add(t);
             }
        }
    }
}

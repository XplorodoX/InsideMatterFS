using UnityEngine;
using System.Collections.Generic;

namespace InsideMatter.Molecule
{
    /// <summary>
    /// Handles the visual representation of a bond (Single, Double, Triple).
    /// Manages the instantiation and positioning of cylinder meshes.
    /// </summary>
    public class BondVisual : MonoBehaviour
    {
        private List<GameObject> cylinders = new List<GameObject>();
        private Material currentMaterial;
        
        /// <summary>
        /// Updates the visual representation based on bond type and dimensions.
        /// </summary>
        public void UpdateVisuals(BondType type, float length, float thickness, Material material)
        {
            int requiredCylinders = GetCylinderCount(type);
            EnsureCylinderCount(requiredCylinders, material);
            
            // Farbe basierend auf Bindungstyp holen
            Color bondColor = Color.white;
            if (MoleculeManager.Instance != null)
            {
                bondColor = MoleculeManager.Instance.GetBondColor(type);
            }
            
            // Set dimensions and positions
            float cylinderScaleY = length / 2f; // Unity cylinder is 2 units high
            float offsetAmount = thickness * 1.5f; // Spacing between multiple bonds
            
            for (int i = 0; i < cylinders.Count; i++)
            {
                GameObject cyl = cylinders[i];
                cyl.transform.localScale = new Vector3(thickness, cylinderScaleY, thickness);
                cyl.transform.localPosition = GetLocalPosition(type, i, offsetAmount);
                cyl.transform.localRotation = Quaternion.identity;
                
                // Farbe anwenden
                Renderer r = cyl.GetComponent<Renderer>();
                if (r != null)
                {
                    MaterialPropertyBlock props = new MaterialPropertyBlock();
                    props.SetColor("_BaseColor", bondColor);
                    props.SetColor("_Color", bondColor);
                    r.SetPropertyBlock(props);
                }
                
                // Ensure active
                if (!cyl.activeSelf) cyl.SetActive(true);
            }
            
            // Hide unused
            for (int i = requiredCylinders; i < cylinders.Count; i++)
            {
                cylinders[i].SetActive(false);
            }
        }
        
        private int GetCylinderCount(BondType type)
        {
            switch (type)
            {
                case BondType.Double: return 2;
                case BondType.Triple: return 3;
                default: return 1;
            }
        }
        
        private void EnsureCylinderCount(int count, Material material)
        {
            if (currentMaterial != material)
            {
                currentMaterial = material;
                // Re-apply material to existing
                foreach (var cyl in cylinders)
                {
                    Renderer r = cyl.GetComponent<Renderer>();
                    if (r != null) r.material = material;
                }
            }
            
            while (cylinders.Count < count)
            {
                GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cyl.name = $"BondCylinder_{cylinders.Count}";
                cyl.transform.SetParent(transform, false);
                
                // Remove collider from visual parts (the parent will have the main collider)
                DestroyImmediate(cyl.GetComponent<Collider>());
                
                Renderer r = cyl.GetComponent<Renderer>();
                if (r != null && currentMaterial != null) r.material = currentMaterial;
                
                cylinders.Add(cyl);
            }
        }
        
        private Vector3 GetLocalPosition(BondType type, int index, float offset)
        {
            if (type == BondType.Single) return Vector3.zero;
            
            if (type == BondType.Double)
            {
                // Side by side
                if (index == 0) return new Vector3(offset, 0, 0);
                if (index == 1) return new Vector3(-offset, 0, 0);
            }
            
            if (type == BondType.Triple)
            {
                // Triangle
                if (index == 0) return new Vector3(offset, 0, 0);
                if (index == 1) return new Vector3(-offset * 0.5f, 0, offset * 0.866f); // sin(60) ≈ 0.866
                if (index == 2) return new Vector3(-offset * 0.5f, 0, -offset * 0.866f);
            }
            
            return Vector3.zero;
        }
    }
}

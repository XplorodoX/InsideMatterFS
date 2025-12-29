using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using InsideMatter.Molecule;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace InsideMatter.Interaction
{
    /// <summary>
    /// Enables changing bond types (Single/Double/Triple) by pointing and clicking in VR.
    /// Uses the Secondary Action Button.
    /// </summary>
    public class BondInteractor : MonoBehaviour
    {
        [Header("Settings")]
        public float rayDistance = 5.0f;
        public LayerMask bondLayer = ~0; // We rely on Bond having a specific component
        
        [Header("Input")]
        public InputActionProperty secondaryAction; // B/Y button usually

        [Header("Feedback")]
        public Color highlightColor = Color.cyan;
        
        private XRBaseInputInteractor interactor;
        private Bond hoverBond;
        private Material originalMaterial;
        private Renderer hoveredRenderer;
        
        void Awake()
        {
            interactor = GetComponent<XRBaseInputInteractor>();
        }
        
        void OnEnable()
        {
            secondaryAction.action.Enable();
            secondaryAction.action.performed += OnSecondaryButton;
        }
        
        void OnDisable()
        {
            secondaryAction.action.Disable();
            secondaryAction.action.performed -= OnSecondaryButton;
        }
        
        void Update()
        {
            HandleRaycasting();
        }
        
        private void HandleRaycasting()
        {
            // Simple Raycast from controller position/rotation
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, rayDistance, bondLayer))
            {
                // Check if we hit a Bond Visual
                var bondVisual = hit.collider.GetComponent<BondVisual>();
                if (bondVisual != null)
                {
                    // Find the Bond object via MoleculeManager
                    Bond foundBond = FindBondByVisual(hit.collider.gameObject);
                    
                    if (foundBond != null)
                    {
                        if (hoverBond != foundBond)
                        {
                            ClearHover();
                            SetHover(foundBond, hit.collider.GetComponentInParent<Renderer>());
                        }
                        return;
                    }
                }
            }
            
            ClearHover();
        }
        
        private Bond FindBondByVisual(GameObject visual)
        {
            if (MoleculeManager.Instance == null) return null;
            
            foreach (var bond in MoleculeManager.Instance.Bonds)
            {
                if (bond.Visual == visual || (bond.Visual != null && visual.transform.IsChildOf(bond.Visual.transform)))
                {
                    return bond;
                }
            }
            return null;
        }
        
        private void SetHover(Bond bond, Renderer renderer)
        {
            hoverBond = bond;
            
            // Simple visual feedback: Change material color
            // Note: This relies on accessing the renderer of the active cylinders.
            // Since BondVisual manages multiple cylinders, we might just highlight the one we hit or all.
            // For simplicity, we just log "Hover" for now or use haptics.
            
            if (interactor != null)
            {
                interactor.SendHapticImpulse(0.1f, 0.05f);
            }
        }
        
        private void ClearHover()
        {
            hoverBond = null;
        }
        
        private void OnSecondaryButton(InputAction.CallbackContext context)
        {
            if (hoverBond != null && MoleculeManager.Instance != null)
            {
                CycleBondType(hoverBond);
            }
        }
        
        private void CycleBondType(Bond bond)
        {
            BondType nextType = GetNextType(bond.Type);
            
            // Try cycling until we find a valid one or return to Single
            // If Single is invalid (unlikely), we stop.
            
            // Attempt 1: Next Type
            if (MoleculeManager.Instance.CanUpgradeBond(bond, nextType))
            {
                ApplyType(bond, nextType);
                return;
            }
            
            // Attempt 2: Skip to the one after? 
            // Better behavior: If next is invalid (e.g. Triple not allowed), try Single.
            // Cycle: Single -> Double -> Triple -> Single
            
            if (nextType == BondType.Double)
            {
                // Double failed, try Triple?
                // Probably better to just fallback to Single immediately if upgrades fail to keep it simple.
                // But let's check Triple just in case Double failed for some weird reason but Triple works (unlikely in chem).
            }
            
            // Fallback: If we can't upgrade to next, reset to Single (always allowed if it exists, assuming >0 valence).
            if (bond.Type != BondType.Single)
            {
                ApplyType(bond, BondType.Single);
            }
            else
            {
                // We are at Single and wanted to upgrade but couldn't. 
                // Feedback: Error sound/haptic
                if (interactor != null) interactor.SendHapticImpulse(0.8f, 0.2f);
                Debug.Log("Cannot upgrade bond due to valence limits.");
            }
        }
        
        private BondType GetNextType(BondType current)
        {
            switch (current)
            {
                case BondType.Single: return BondType.Double;
                case BondType.Double: return BondType.Triple;
                case BondType.Triple: return BondType.Single;
                default: return BondType.Single;
            }
        }
        
        private void ApplyType(Bond bond, BondType newType)
        {
            bond.SetType(newType);
            if (interactor != null) interactor.SendHapticImpulse(0.5f, 0.1f);
            Debug.Log($"Bond changed to {newType}");
        }
    }
}

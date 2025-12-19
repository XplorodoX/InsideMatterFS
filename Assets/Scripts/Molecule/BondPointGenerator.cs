using UnityEngine;

namespace InsideMatter.Molecule
{
    /// <summary>
    /// Erstellt BondPoints in verschiedenen geometrischen Anordnungen
    /// für verschiedene Valenzen und Molekülgeometrien.
    /// </summary>
    public static class BondPointGenerator
    {
        /// <summary>
        /// Erstellt BondPoints für ein Atom mit der angegebenen Geometrie
        /// </summary>
        public static void GenerateBondPoints(Transform parent, int count, float distance = 0.5f)
        {
            // Alte BondPoints entfernen
            BondPoint[] existing = parent.GetComponentsInChildren<BondPoint>();
            foreach (var bp in existing)
            {
                if (Application.isPlaying)
                    Object.Destroy(bp.gameObject);
                else
                    Object.DestroyImmediate(bp.gameObject);
            }
            
            // Neue BondPoints erstellen basierend auf Anzahl
            switch (count)
            {
                case 1:
                    CreateLinear1(parent, distance);
                    break;
                case 2:
                    CreateLinear2(parent, distance);
                    break;
                case 3:
                    CreateTrigonalPlanar(parent, distance);
                    break;
                case 4:
                    CreateTetrahedral(parent, distance);
                    break;
                case 5:
                    CreateTrigonalBipyramidal(parent, distance);
                    break;
                case 6:
                    CreateOctahedral(parent, distance);
                    break;
                default:
                    UnityEngine.Debug.LogWarning($"Keine Geometrie für {count} BondPoints definiert");
                    break;
            }
        }
        
        /// <summary>
        /// 1 BondPoint (z.B. für Wasserstoff)
        /// </summary>
        private static void CreateLinear1(Transform parent, float distance)
        {
            CreateBondPoint(parent, Vector3.right * distance, 0);
        }
        
        /// <summary>
        /// 2 BondPoints - Linear (z.B. für Sauerstoff)
        /// Winkel: 180°
        /// </summary>
        private static void CreateLinear2(Transform parent, float distance)
        {
            CreateBondPoint(parent, Vector3.right * distance, 0);
            CreateBondPoint(parent, Vector3.left * distance, 1);
        }
        
        /// <summary>
        /// 3 BondPoints - Trigonal Planar (z.B. für Bor)
        /// Winkel: 120°
        /// </summary>
        private static void CreateTrigonalPlanar(Transform parent, float distance)
        {
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 120f * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(
                    Mathf.Cos(angle) * distance,
                    0f,
                    Mathf.Sin(angle) * distance
                );
                CreateBondPoint(parent, pos, i);
            }
        }
        
        /// <summary>
        /// 4 BondPoints - Tetraeder (z.B. für Kohlenstoff)
        /// Winkel: 109.5°
        /// </summary>
        private static void CreateTetrahedral(Transform parent, float distance)
        {
            // Tetraeder-Koordinaten (normalisiert)
            Vector3[] directions = new Vector3[]
            {
                new Vector3(1, 1, 1).normalized,
                new Vector3(-1, -1, 1).normalized,
                new Vector3(-1, 1, -1).normalized,
                new Vector3(1, -1, -1).normalized
            };
            
            for (int i = 0; i < 4; i++)
            {
                CreateBondPoint(parent, directions[i] * distance, i);
            }
        }
        
        /// <summary>
        /// 5 BondPoints - Trigonal Bipyramidal (z.B. für Phosphor)
        /// </summary>
        private static void CreateTrigonalBipyramidal(Transform parent, float distance)
        {
            // Äquatorial (3 in einer Ebene)
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 120f * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(
                    Mathf.Cos(angle) * distance,
                    0f,
                    Mathf.Sin(angle) * distance
                );
                CreateBondPoint(parent, pos, i);
            }
            
            // Axial (2 vertikal)
            CreateBondPoint(parent, Vector3.up * distance, 3);
            CreateBondPoint(parent, Vector3.down * distance, 4);
        }
        
        /// <summary>
        /// 6 BondPoints - Oktaeder (z.B. für Schwefel)
        /// </summary>
        private static void CreateOctahedral(Transform parent, float distance)
        {
            Vector3[] directions = new Vector3[]
            {
                Vector3.right,
                Vector3.left,
                Vector3.up,
                Vector3.down,
                Vector3.forward,
                Vector3.back
            };
            
            for (int i = 0; i < 6; i++)
            {
                CreateBondPoint(parent, directions[i] * distance, i);
            }
        }
        
        /// <summary>
        /// Erstellt einen einzelnen BondPoint
        /// </summary>
        private static void CreateBondPoint(Transform parent, Vector3 localPosition, int index)
        {
            GameObject bpObj = new GameObject($"BondPoint_{index}");
            bpObj.transform.SetParent(parent);
            bpObj.transform.localPosition = localPosition;
            bpObj.transform.localRotation = Quaternion.identity;
            
            // BondPoint Component hinzufügen
            BondPoint bp = bpObj.AddComponent<BondPoint>();
            
            // Sphere Collider hinzufügen (wird automatisch als Trigger konfiguriert)
            SphereCollider collider = bpObj.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.2f;
            
            // Kleines visuelles Gizmo für BondPoint (gelb, halbtransparent)
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Visual";
            visual.transform.SetParent(bpObj.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = Vector3.one * 0.1f;
            
            // Material gelb und transparent machen
            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                // Gelbe Farbe mit leichter Transparenz
                mat.SetColor("_BaseColor", new Color(1f, 1f, 0f, 0.7f));
                mat.SetFloat("_Surface", 1); // 0 = Opaque, 1 = Transparent
                mat.SetFloat("_Blend", 0);   // 0 = Alpha, 1 = Premultiply
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = 3000;
                renderer.material = mat;
            }
            
            // Collider vom Visual entfernen
            var visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(visualCollider);
                else
                    Object.DestroyImmediate(visualCollider);
            }
        }
    }
}

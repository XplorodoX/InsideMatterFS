using UnityEngine;
using System.Collections;

namespace InsideMatter.Effects
{
    /// <summary>
    /// Visuelle und Audio-Effekte für Erfolg und Fehler im Spiel.
    /// </summary>
    public class FeedbackEffects : MonoBehaviour
    {
        public static FeedbackEffects Instance { get; private set; }
        
        [Header("Erfolgs-Effekte")]
        [Tooltip("Partikel-System für Konfetti")]
        public ParticleSystem confettiParticles;
        
        [Tooltip("Partikel-System für grünes Leuchten")]
        public ParticleSystem successGlowParticles;
        
        [Tooltip("Audio-Clip für Erfolg")]
        public AudioClip successSound;
        
        [Header("Fehler-Effekte")]
        [Tooltip("Partikel-System für rotes Pulsieren")]
        public ParticleSystem errorParticles;
        
        [Tooltip("Audio-Clip für Fehler")]
        public AudioClip errorSound;
        
        [Header("Allgemeine Einstellungen")]
        public float effectDuration = 2f;
        
        private AudioSource audioSource;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            // Audio-Source erstellen
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.5f;
            
            // Partikel-Systeme erstellen falls nicht vorhanden
            CreateDefaultParticleSystems();
        }
        
        /// <summary>
        /// Erstellt Standard-Partikel-Systeme
        /// </summary>
        private void CreateDefaultParticleSystems()
        {
            // Konfetti
            if (confettiParticles == null)
            {
                GameObject confettiObj = new GameObject("ConfettiParticles");
                confettiObj.transform.SetParent(transform);
                confettiParticles = confettiObj.AddComponent<ParticleSystem>();
                
                var main = confettiParticles.main;
                main.loop = false;
                main.duration = 2f;
                main.startLifetime = 3f;
                main.startSpeed = 3f;
                main.startSize = 0.05f;
                main.startColor = new ParticleSystem.MinMaxGradient(
                    new Color(1f, 0.8f, 0.2f),
                    new Color(0.2f, 1f, 0.4f)
                );
                main.gravityModifier = 0.5f;
                main.maxParticles = 200;
                
                var emission = confettiParticles.emission;
                emission.rateOverTime = 0;
                emission.SetBursts(new ParticleSystem.Burst[] {
                    new ParticleSystem.Burst(0f, 100)
                });
                
                var shape = confettiParticles.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 45f;
                shape.radius = 0.1f;
                
                var colorOverLifetime = confettiParticles.colorOverLifetime;
                colorOverLifetime.enabled = true;
                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
                );
                colorOverLifetime.color = gradient;
                
                confettiParticles.Stop();
            }
            
            // Erfolgs-Glow
            if (successGlowParticles == null)
            {
                GameObject glowObj = new GameObject("SuccessGlowParticles");
                glowObj.transform.SetParent(transform);
                successGlowParticles = glowObj.AddComponent<ParticleSystem>();
                
                var main = successGlowParticles.main;
                main.loop = false;
                main.duration = 1f;
                main.startLifetime = 1.5f;
                main.startSpeed = 1f;
                main.startSize = 0.2f;
                main.startColor = new Color(0.3f, 1f, 0.4f, 0.7f);
                main.maxParticles = 50;
                
                var emission = successGlowParticles.emission;
                emission.rateOverTime = 0;
                emission.SetBursts(new ParticleSystem.Burst[] {
                    new ParticleSystem.Burst(0f, 30)
                });
                
                var shape = successGlowParticles.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.3f;
                
                successGlowParticles.Stop();
            }
            
            // Fehler-Partikel
            if (errorParticles == null)
            {
                GameObject errorObj = new GameObject("ErrorParticles");
                errorObj.transform.SetParent(transform);
                errorParticles = errorObj.AddComponent<ParticleSystem>();
                
                var main = errorParticles.main;
                main.loop = false;
                main.duration = 0.5f;
                main.startLifetime = 0.8f;
                main.startSpeed = 0.5f;
                main.startSize = 0.15f;
                main.startColor = new Color(1f, 0.3f, 0.2f, 0.8f);
                main.maxParticles = 20;
                
                var emission = errorParticles.emission;
                emission.rateOverTime = 0;
                emission.SetBursts(new ParticleSystem.Burst[] {
                    new ParticleSystem.Burst(0f, 15)
                });
                
                var shape = errorParticles.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.2f;
                
                errorParticles.Stop();
            }
        }
        
        /// <summary>
        /// Spielt Erfolgs-Effekte ab
        /// </summary>
        public void PlaySuccessEffect(Vector3 position)
        {
            // Konfetti
            if (confettiParticles != null)
            {
                confettiParticles.transform.position = position + Vector3.up * 0.3f;
                confettiParticles.Play();
            }
            
            // Glow
            if (successGlowParticles != null)
            {
                successGlowParticles.transform.position = position;
                successGlowParticles.Play();
            }
            
            // Sound
            if (successSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(successSound);
            }
            
            Debug.Log("[FeedbackEffects] Erfolgs-Effekt abgespielt!");
        }
        
        /// <summary>
        /// Spielt Fehler-Effekte ab
        /// </summary>
        public void PlayErrorEffect(Vector3 position)
        {
            if (errorParticles != null)
            {
                errorParticles.transform.position = position;
                errorParticles.Play();
            }
            
            if (errorSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(errorSound);
            }
            
            Debug.Log("[FeedbackEffects] Fehler-Effekt abgespielt!");
        }
        
        /// <summary>
        /// Lässt ein GameObject kurz aufleuchten (Erfolg)
        /// </summary>
        public void FlashSuccess(GameObject target)
        {
            StartCoroutine(FlashColor(target, new Color(0.3f, 1f, 0.4f), 0.5f));
        }
        
        /// <summary>
        /// Lässt ein GameObject kurz rot aufleuchten (Fehler)
        /// </summary>
        public void FlashError(GameObject target)
        {
            StartCoroutine(FlashColor(target, new Color(1f, 0.3f, 0.2f), 0.5f));
        }
        
        private IEnumerator FlashColor(GameObject target, Color flashColor, float duration)
        {
            var renderers = target.GetComponentsInChildren<MeshRenderer>();
            
            // Original-Farben speichern
            Color[] originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].material.HasProperty("_BaseColor"))
                {
                    originalColors[i] = renderers[i].material.GetColor("_BaseColor");
                }
            }
            
            // Flash
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = Mathf.PingPong(elapsed * 4f, 1f);
                
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i].material.HasProperty("_BaseColor"))
                    {
                        renderers[i].material.SetColor("_BaseColor", 
                            Color.Lerp(originalColors[i], flashColor, t));
                    }
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Original wiederherstellen
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].material.HasProperty("_BaseColor"))
                {
                    renderers[i].material.SetColor("_BaseColor", originalColors[i]);
                }
            }
        }
    }
}

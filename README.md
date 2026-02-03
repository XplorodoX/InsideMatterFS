# InsideMatter 🔬

> **Eine immersive VR-Anwendung zum Lernen und Experimentieren mit molekularer Chemie**

![Unity](https://img.shields.io/badge/Unity-2022.3+-black?logo=unity)
![PICO VR](https://img.shields.io/badge/PICO-VR-blue)
![License](https://img.shields.io/badge/License-MIT-green)

## 📖 Übersicht

**InsideMatter** ist eine Virtual-Reality-Anwendung für PICO VR-Headsets, die physikalische und chemische Konzepte greifbar macht. Durch den Ansatz des **"Learning by Doing"** können Nutzer chemische Moleküle in einer interaktiven 3D-Umgebung intuitiv bauen, erkunden und verstehen.

Die Anwendung gliedert sich in zwei zentrale Bereiche:
*   **Lernraum:** Ein interaktives Klassenzimmer, in dem Nutzer durch 32 aufeinanderfolgende Level geführt werden.
*   **Trophäenraum:** Eine Ausstellung der selbst gebauten Moleküle zur Vertiefung und Analyse.

---

## ✨ Features

### 🏫 Immersiver Lernraum
*   **Realistische Umgebung:** Gestaltet wie ein Klassenzimmer mit Tafeln, Pinnwänden und Experimentiertischen.
*   **Interaktives Menü:** Level-Auswahl und Einstellungen direkt an der virtuellen Tafel.
*   **Hilfestellungen:** Pinnwände mit Legenden zum CPK-Farbschema und Controller-Steuerung.

### 🧪 Realistischer Molekülbau
*   **10 Atomtypen** (H, C, N, O, F, P, S, Cl, Br, I) mit korrekten chemischen Eigenschaften und Valenzen.
*   **Bindungssystem:** Unterstützt Einfach-, Doppel- und Dreifachbindungen.
*   **Valenz-Prüfung:** Atome verhindern physikalisch inkorrekte Bindungen (z.B. max. 4 Bindungen für Kohlenstoff).
*   **Snapping-System:** Atome rasten automatisch an gültigen Bindungsstellen ein.
*   **Visuelles Feedback:** "Ghost Lines" zeigen mögliche Bindungen vor dem Einrasten an.

### 🏆 Gamification & Progression
*   **32 Level:** Schrittweise Steigerung der Komplexität – von Wasser ($H_2O$) bis zu komplexeren Säuren.
*   **Validierungszone:** Überprüft gebaute Moleküle automatisch auf strukturelle Richtigkeit.
*   **Trophäenraum:** Erfolgreich gebaute Moleküle werden als freischaltbare Trophäen ausgestellt und können dort im Detail betrachtet werden.

### � Intuitive VR-Interaktion
*   **Grabbing:** Natürliches Greifen und Bewegen von Atomen.
*   **Haptisches Feedback:** Vibration bei erfolgreichen Interaktionen.
*   **Zweihändige Bedienung:** Bindungen können durch Auseinanderziehen mit beiden Händen getrennt werden.

---

## 🛠 Technologie-Stack

| Komponente | Technologie |
|------------|-------------|
| **Engine** | Unity 2022.3 LTS |
| **VR Framework** | XR Interaction Toolkit |
| **Plattform** | PICO XR SDK (Android) |
| **Scripting** | C# |
| **Modellierung** | Blender (Geometry Nodes) |

---

## 🚀 Installation

### Voraussetzungen
- Unity 2022.3 LTS oder neuer
- PICO Developer SDK
- PICO VR-Headset (PICO 4, PICO Neo 3)

### Setup

1. **Repository klonen**
   ```bash
   git clone https://github.com/XplorodoX/InsideMatterFS.git
   cd InsideMatterFS
   ```

2. **Projekt in Unity öffnen**
   - Unity Hub öffnen
   - "Open" → Projektordner auswählen
   - Unity importiert automatisch alle Abhängigkeiten.

3. **Build erstellen**
   - `File` → `Build Settings`
   - Platform auf **Android** switchen
   - "Build and Run" mit verbundenem PICO-Headset ausführen.

---

## 🎮 Steuerung

| Aktion | Controller-Eingabe |
|--------|-------------------|
| **Atom greifen/halten** | Grip-Taste (gedrückt halten) |
| **Atom loslassen** | Grip-Taste loslassen |
| **Bindungstyp ändern** | Taste B (rechts) / Y (links) |
| **Bindung trennen** | Beide Atome greifen & auseinanderziehen |
| **Teleportieren** | Joystick nach vorne drücken |

---

## 🧬 Verfügbare Elemente

Die Farbgebung orientiert sich am **CPK-Modell**:

| Element | Symbol | Valenz | Farbe |
|---------|--------|--------|-------|
| Wasserstoff | H | 1 | ⚪ Weiß |
| Kohlenstoff | C | 4 | ⚫ Grau/Schwarz |
| Stickstoff | N | 3 | 🔵 Blau |
| Sauerstoff | O | 2 | 🔴 Rot |
| Fluor | F | 1 | 🟢 Gelb-Grün |
| Phosphor | P | 3/5 | 🟠 Orange |
| Schwefel | S | 2/4/6 | 🟡 Gelb |
| Chlor | Cl | 1 | 🟢 Hellgrün |
| Brom | Br | 1 | 🟤 Braun |
| Iod | I | 1 | 🟣 Violett |

---

## 👥 Team

Entwickelt im Rahmen eines Projekts an der **Hochschule Aalen**.

*   **Florian Merlau**
*   **Markus**
*   **Lukas**

## 📄 Lizenz

Dieses Projekt ist unter der MIT-Lizenz verfügbar. Weitere Details in der [LICENSE](LICENSE) Datei.

# InsideMatter 🔬

> **Eine immersive VR-Anwendung zum Lernen und Experimentieren mit molekularer Chemie**

![Unity](https://img.shields.io/badge/Unity-2022.3+-black?logo=unity)
![PICO VR](https://img.shields.io/badge/PICO-VR-blue)
![License](https://img.shields.io/badge/License-MIT-green)

## 📖 Übersicht

**InsideMatter** ist eine Virtual-Reality-Anwendung für PICO VR-Headsets, die es Nutzern ermöglicht, chemische Moleküle in einer interaktiven 3D-Umgebung zu bauen und zu erkunden. Die Anwendung kombiniert spielerisches Lernen mit wissenschaftlicher Genauigkeit und eignet sich ideal für Bildungszwecke.

## ✨ Features

### 🧪 Molekülbau
- **10 verschiedene Atomtypen** mit realistischen chemischen Eigenschaften
- **Einfach-, Doppel- und Dreifachbindungen** zwischen Atomen
- **Valenz-basiertes Bindungssystem** - Atome respektieren ihre maximale Bindungskapazität
- **Visuelle Bindungsvorschau** beim Verbinden von Atomen
- **Automatisches Atom-Respawning** für kontinuierliches Experimentieren

### 🎮 VR-Interaktion
- **Intuitive Grab-Mechanik** - Atome mit VR-Controllern greifen und bewegen
- **Hand-Tracking-Unterstützung** via Unity XR Hands
- **Starre Molekülbewegung** - verbundene Atome bewegen sich als Einheit
- **Bond-Trennung** durch gleichzeitiges Greifen und Ziehen

### 🧩 Puzzle-Modus
- **Level-basierte Herausforderungen** - baue spezifische Moleküle
- **Validierungszone** zur Überprüfung der Molekülstruktur
- **Fortschrittssystem** mit mehreren Schwierigkeitsstufen
- **Visuelles Feedback** für korrekte und inkorrekte Lösungen

### 🎨 Benutzeroberfläche
- **VR-natives Menüsystem** auf virtueller Tafel
- **Level-Auswahl** mit übersichtlicher Navigation
- **Whiteboard-Integration** für Aufgabenbeschreibungen

## 🛠 Technologie-Stack

| Komponente | Technologie |
|------------|-------------|
| **Engine** | Unity 2022.3+ |
| **VR-SDK** | PICO XR SDK |
| **Interaktion** | Unity XR Interaction Toolkit |
| **Hand-Tracking** | Unity XR Hands |
| **Programmiersprache** | C# |
| **3D-Modelle** | Blender |

## 📁 Projektstruktur

```
InsideMatterFS-1/
├── Assets/
│   └── Scripts/
│       ├── Molecule/          # Kernlogik für Atome & Bindungen
│       │   ├── Atom.cs
│       │   ├── Bond.cs
│       │   ├── BondPoint.cs
│       │   └── MoleculeManager.cs
│       ├── Puzzle/            # Puzzle-Spielmodus
│       │   ├── PuzzleGameManager.cs
│       │   ├── ValidationZone.cs
│       │   └── PuzzleLevel.cs
│       ├── Interaction/       # VR-Interaktionen
│       │   ├── VRAtomGrab.cs
│       │   └── BondInteractor.cs
│       ├── UI/                # Benutzeroberfläche
│       │   ├── MenuManager.cs
│       │   └── WhiteboardController.cs
│       └── VR/                # VR-spezifische Komponenten
├── Packages/                  # Unity Packages
└── ProjectSettings/           # Unity-Projekteinstellungen
```

## 🚀 Installation

### Voraussetzungen
- Unity 2022.3 LTS oder neuer
- PICO Developer SDK
- PICO VR-Headset (PICO 4, PICO Neo 3, etc.)

### Setup

1. **Repository klonen**
   ```bash
   git clone https://github.com/your-username/InsideMatterFS-1.git
   cd InsideMatterFS-1
   ```

2. **Projekt in Unity öffnen**
   - Unity Hub öffnen
   - "Open" → Projektordner auswählen
   - Unity wird die benötigten Packages automatisch importieren

3. **Build erstellen**
   - `File` → `Build Settings`
   - Platform: Android (für PICO)
   - "Build and Run" mit verbundenem PICO-Headset

## 🎮 Steuerung

| Aktion | Controller-Eingabe |
|--------|-------------------|
| Atom greifen | Grip-Taste gedrückt halten |
| Atom loslassen | Grip-Taste loslassen |
| Bindungstyp wechseln | Trigger-Taste während Vorschau |
| Bindung trennen | Beide Atome greifen und auseinanderziehen |

## 🧬 Unterstützte Atome

| Element | Symbol | Valenz | Farbe |
|---------|--------|--------|-------|
| Wasserstoff | H | 1 | Weiß |
| Kohlenstoff | C | 4 | Grau |
| Stickstoff | N | 3 | Blau |
| Sauerstoff | O | 2 | Rot |
| Fluor | F | 1 | Gelb-Grün |
| Phosphor | P | 3/5 | Orange |
| Schwefel | S | 2/4/6 | Gelb |
| Chlor | Cl | 1 | Grün |
| Brom | Br | 1 | Braun |
| Iod | I | 1 | Violett |

## 👥 Team

Entwickelt an der **Hochschule Aalen**

- Florian Merlau
- Markus
- Lukas

## 📄 Lizenz

Dieses Projekt ist unter der MIT-Lizenz lizenziert - siehe [LICENSE](LICENSE) für Details.

---

<p align="center">
  <b>Tauche ein in die Welt der Moleküle! 🧪🥽</b>
</p>

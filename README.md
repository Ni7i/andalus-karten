# Andalus Karten

Eine schlichte Karteikarten-App für arabische Vokabeln – mein Begleiter für den
Arabischunterricht am Andalus-Institut. Läuft nativ auf **Android (Google Pixel 7a)** und
**macOS** und ist mit **.NET MAUI / C#** gebaut.

## Funktionen

- **Lektionen** und **Essential**-Lektionen als eigene Tabs
- Karteikarten: Karte antippen → Antwort sehen → *Gewusst* oder *Nochmal*
- *Nochmal* bringt die Karte ein paar Karten später in derselben Runde zurück
- **Leitner-System**: gewusste Karten wandern in höhere Boxen und kommen seltener
  (1 → 2 → 4 → 8 → 16 Tage); „fällige“ Karten lassen sich mit einem Tipp wiederholen
- Abfragerichtung Arabisch → Deutsch, Deutsch → Arabisch oder gemischt
- Umschrift und Notizen (z. B. Plural) auf der Rückseite
- Wortliste pro Lektion mit Lernstand (●●●○○)
- Hell-/Dunkelmodus, Arabisch rechts-nach-links mit großer Schrift
- Fortschritt bleibt lokal auf dem Gerät, keine Konten, kein Internet nötig
- Vokabeldatei kann in den Einstellungen ausgetauscht werden (JSON-Import)

## Installation

### Pixel 7a (Android)

1. Unter **Actions → Build → letzter Lauf** das Artefakt *AndalusKarten-Android* herunterladen
   (oder bei einem Release `AndalusKarten.apk`).
2. Die APK auf dem Pixel öffnen und „Installation aus unbekannten Quellen“ für die
   Dateien-App bzw. den Browser erlauben.
3. Updates einfach über die bestehende App installieren – der Lernfortschritt bleibt.

> Die APK wird mit einem festen Sideload-Schlüssel (`build/sideload.keystore`) signiert,
> damit Updates sich über die alte Version installieren lassen. Für eine Veröffentlichung
> im Play Store müsste ein eigener, geheimer Schlüssel verwendet werden.

### macOS

**Variante A – fertige App aus GitHub Actions**

1. Artefakt *AndalusKarten-macOS* herunterladen und entpacken.
2. `Andalus Karten.app` in den Ordner *Programme* ziehen.
3. Da die App nicht notarisiert ist, einmalig im Terminal:
   ```sh
   xattr -dr com.apple.quarantine "/Applications/Andalus Karten.app"
   ```

**Variante B – lokal bauen** (benötigt Xcode und das .NET 10 SDK)

```sh
dotnet workload install maui-maccatalyst
dotnet build src/AndalusKarten.App -f net10.0-maccatalyst -t:Run
```

## Inhalt

Die Vokabeln stammen aus *Kashf al-Mufradāt* (Andalus Institute), Lektionen 1–70:

- **70 Lektionen** mit ~4 500 Karten: Wortschatz (mit Plural), Verben (mit Präsens und
  Maṣdar), Ausdrücke und Wörter im Kontext (Beispielsatz auf der Rückseite).
  Die Bedeutungen sind – wie im Buch – auf Englisch.
- **14 Essential-Lektionen** „Kernwortschatz“ je 5 Lektionen: nur Wörter und Verben mit
  direkter Bedeutung, ohne Beispielsätze. Sie teilen den Lernfortschritt mit den Lektionen.

Neu erzeugen aus dem PDF (die drei Teildateien in einem Ordner):

```sh
python3 tools/pdf_dump.py /pfad/zu/den/pdfs/   # → raw.json
python3 tools/pdf_build.py src/AndalusKarten.App/Resources/Raw/vocabulary.json
```

## Vokabeln pflegen

Alle Wörter stehen in
[`src/AndalusKarten.App/Resources/Raw/vocabulary.json`](src/AndalusKarten.App/Resources/Raw/vocabulary.json):

```json
{
  "lessons": [
    {
      "id": "L01", "number": 1, "title": "Begrüßung", "essential": false,
      "words": [
        { "id": "L01-001", "ar": "مَرْحَبًا", "de": "Hallo", "tr": "marḥaban", "note": "" }
      ]
    }
  ]
}
```

- `essential: true` legt die Lektion im Tab **Essential** ab.
- Die `id` eines Wortes ist der Schlüssel für den Lernfortschritt – beim Korrigieren eines
  Wortes die ID bitte beibehalten.
- Aus einer Tabelle (CSV/TSV mit den Spalten `kind, number, title, ar, de, tr, note`)
  erzeugt `tools/csv_to_vocab.py` die JSON-Datei:
  ```sh
  python3 tools/csv_to_vocab.py woerter.csv src/AndalusKarten.App/Resources/Raw/vocabulary.json
  ```
- Ohne neuen Build: In der App unter **Einstellungen → Vokabeldatei importieren** eine
  `vocabulary.json` auswählen.

## Aufbau

```
src/AndalusKarten.Core          Domänenlogik ohne UI-Abhängigkeit (Leitner, Lernrunde,
                                Fortschritt, JSON-Parser mit Source-Generator)
src/AndalusKarten.App           .NET MAUI App (Shell, MVVM mit CommunityToolkit.Mvvm)
tests/AndalusKarten.Core.Tests  xUnit-Tests für die Core-Logik
tools/                          Hilfsskripte für die Vokabelpflege
.github/workflows/build.yml     Tests, Android-APK und macOS-App bei jedem Push;
                                Tags `v*` erzeugen ein GitHub-Release
```

Entwicklung ohne Android-/Apple-SDK (z. B. unter Linux) – schneller Compile-Check der App:

```sh
dotnet test tests/AndalusKarten.Core.Tests
dotnet build src/AndalusKarten.App -p:CompileCheck=true
```

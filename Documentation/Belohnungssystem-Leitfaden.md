# Fahrtbelohnungen: Einstieg und Zusammenarbeit mit ChatGPT/Codex

Für den Kollegen, der das Belohnungssystem weiterentwickelt. Stand: 8. September 2026.
Die Vorschläge in diesem Dokument sind **noch nicht implementiert**. Der aktuelle
Fahrpreis und die Auszahlung bleiben unverändert. Autopreise und Kaufreihenfolge
gehören nicht zu diesem Arbeitsauftrag.

## 1. Windows einrichten und das Projekt öffnen

### Codex installieren

Die aktuelle offizielle Windows-Anleitung führt Codex über die ChatGPT-Desktop-App.
Installiere sie über den dort verlinkten Microsoft Store, melde dich mit deinem
ChatGPT-Konto an und öffne den lokalen Codex-Arbeitsbereich. Alternativ kannst du
in PowerShell installieren:

```powershell
winget install --id 9PLM9XGG6VKS -s msstore
winget install --id Git.Git -e
```

Öffne PowerShell nach der Git-Installation erneut. Für dieses Unity-Projekt eignet
sich ein normaler Windows-Ordner; WSL ist nicht nötig. Füge später den gesamten
Projektordner in Codex über „Add new project“ bzw. `Ctrl+O` hinzu.
Quelle: [Offizielle Windows-Anleitung von OpenAI](https://learn.chatgpt.com/docs/windows/windows-app).
Falls Bezeichnungen in deiner App abweichen, verwende den Einstieg der verlinkten Anleitung.

### Repository holen
```powershell
New-Item -ItemType Directory -Force C:\Dev
Set-Location C:\Dev
git clone https://github.com/PhilipSynowiec/FahrpreisFieber.git
Set-Location FahrpreisFieber
git status
git switch -c codex/fahrtbelohnungen
```
In Codex **C:\Dev\FahrpreisFieber** als Projekt wählen.

### Unity öffnen

1. Unity Hub installieren und anmelden.
2. **Unity 6000.2.12f1** installieren. Diese Version steht in
   `ProjectSettings/ProjectVersion.txt`; nicht ungefragt auf eine andere Version upgraden.
3. Im Hub unter Projects ein vorhandenes Projekt vom Datenträger hinzufügen:
   **C:\Dev\FahrpreisFieber**. Das ist der Ordner mit `Assets`, `Packages` und
   `ProjectSettings`, nicht nur der Assets-Unterordner.
4. Mit dieser Editor-Version öffnen und den ersten Import abwarten.
5. `Assets/Scenes/MainMenu.unity` öffnen, Play drücken und eine Fahrt ausprobieren.
   Die eigentliche Spielszene ist `Assets/Scenes/Game.unity`.
6. Bei roten Console-Fehlern die vollständige erste Fehlermeldung an Codex geben,
   bevor du am Balancing weiterarbeitest. Für Gerätetests das passende Build-Modul
   im Hub hinzufügen, beispielsweise Android Build Support.

Unity Hub verwaltet Editor-Installationen und Projekte:
[Unity-Installationsanleitung](https://docs.unity3d.com/6000.1/Documentation/Manual/GettingStartedInstallingUnity.html).
Die konkrete Editor-Version oben stammt aus diesem Repository.

## 2. Das Fahrtbelohnungssystem verstehen, ändern und vergleichen

### Wie es derzeit funktioniert

Ein wartender Fahrgast erhält bei seiner Erzeugung ein festes Angebot. Grundlage
ist die kürzeste Straßenroute im Datenmodell, nicht die Luftlinie und nicht die
später tatsächlich gefahrene Strecke. Das verhindert, dass Umwege den Fahrpreis erhöhen.

Mit `s` als Anzahl der Straßenschritte und `k` als Kachelgröße gilt:

```text
D = max(k, (s + 1) × k)                        Strecke in Welteinheiten
v = zufällige Zielgeschwindigkeit             aus RatingSpeedModel
T = clamp(D / v, 10, 240)                     Zeitlimit in Sekunden
P = round(D × 1,2 + (D / T) × 8)              angebotene Münzen
```

Die beiden Preisfaktoren stehen am `PassengerSpawner` in der Game-Szene.
`TripRewardCalculator` enthält die Berechnung. Die Rundung verwendet Unitys
`Mathf.RoundToInt` (halbe Werte werden zur nächsten geraden Ganzzahl gerundet).
Das Zeitlimit beginnt bei Annahme des Auftrags. Anfahrt zum wartenden Fahrgast
zählt nicht zur Frist, aber natürlich zur tatsächlich investierten Spielzeit.

Bei Ankunft gilt derzeit **alles oder nichts**:

```text
Ziel erreicht und Fahrtdauer <= T: Auszahlung = P
Zu spät:                         Auszahlung = 0
```

Die Abgabe muss nahe genug am Ziel und bei höchstens 4 Welteinheiten/s erfolgen.
Die aktuelle Szene verwendet 3,5 Welteinheiten Zielradius. Auch die Aufnahme ist
bei bis zu 4 Welteinheiten/s erlaubt. Die Timeranzeige ist kein Ersatz für die
exakte interne Zeitprüfung. Bereits leichtes Überschreiten kann die Auszahlung kosten.

Bewertungen sind vom Geld getrennt. Mit `r = (T − Fahrtdauer) / T`:

| Verbleibender Anteil r | Sterne |
|---|---:|
| mindestens 25 % | 5 |
| mindestens 10 %, weniger als 25 % | 4 |
| mindestens 0 %, weniger als 10 % | 3 |
| mindestens −25 %, weniger als 0 % | 2 |
| weniger als −25 % | 1 |

Der Text kommt aus einer passenden Offline-Gruppe mit 200 Texten je Sternzahl.
Mehr Sterne bringen aktuell **kein zusätzliches Trinkgeld**.

Beispiel: D = 300 und v = 6 ergeben T = 50 Sekunden und P = 408 Münzen.

| Fahrtdauer | Sterne | Auszahlung |
|---|---:|---:|
| 30 s | 5 | 408 |
| 45 s | 4 | 408 |
| 49 s | 3 | 408 |
| 50 s | 3 | 408 |
| 55 s | 2 | 0 |
| 70 s | 1 | 0 |

### Adaptive Schwierigkeit und Speicherung

Die Zielgeschwindigkeit wird normalverteilt gezogen: anfänglicher Mittelwert 6,
Standardabweichung 1,5, Ergebnis begrenzt auf 1,5 bis 20. Nach einer abgeschlossenen
Fahrt verändert sich der Mittelwert:

```text
neuer Mittelwert = clamp(alter Mittelwert + 2 × clamp(r, −0,5, 0,5), 3, 15)
```

Frühe Ankunft macht zukünftige Angebote im Mittel anspruchsvoller; späte Ankunft
macht sie leichter. Bereits erzeugte Angebote ändern sich dadurch nicht.
Wichtig für Experimente: Schwierigkeitsanpassung verändert zugleich Frist und
Geschwindigkeitsanteil des Preises. Deshalb zunächst konstant halten, wenn du
nur eine neue Auszahlungsformel vergleichen willst.

Dauerhafter Fortschritt liegt lokal in Unity `PlayerPrefs`, außerhalb des Repositories:

| Wert | Schlüssel | Ohne vorhandenen Spielstand |
|---|---|---|
| Münzen | `coins` | **0** |
| Besitz | `garage.owned.<id>` | nur kostenloses Startauto |
| gewähltes Auto | `garage.selected` | Startauto |
| Kameraperspektive | `camera.view` | erhöhte Ansicht |
| adaptive Zielgeschwindigkeit | `trips.difficulty.mean` | Szenen-Startwert 6 |

Münzänderungen, Käufe, Auswahl und Schwierigkeit werden sofort mit
`PlayerPrefs.Save()` gespeichert. Ein Neustart oder Szenenwechsel setzt den
Fortschritt nicht zurück. Bestehende Spielstände werden weiterverwendet; Tests
oder Editor-Guthaben werden nicht mit dem App-Build ausgeliefert.
Ein neues lokales Profil beginnt bei 0. Eine vom Betriebssystem wiederhergestellte
Datensicherung ist ein vorhandener Spielstand; das ist von einer Installation
ohne gespeicherte App-Daten zu unterscheiden. Es gibt hier keinen Cloud-Account-Sync.
Laufende Aufträge, Fahrzeugposition und Restzeit werden derzeit noch nicht
wiederhergestellt; die gewünschte Fortsetzung laufender Fahrten ist noch abzustimmen.

### Zuständigkeiten im Code

Alle Pfade sind relativ zum Repository:

| Datei | Aufgabe / Ansatzpunkt |
|---|---|
| `Assets/Scripts/Gameplay/Taxi/TripRewardCalculator.cs` | Reine Berechnung von Frist, Angebot und Ergebnis; zentraler Ort für Auszahlungsvarianten |
| `Assets/Scripts/Gameplay/Taxi/PassengerSpawner.cs` | Straßenstrecke ermitteln, Angebot erzeugen; Preisfaktoren im Inspector |
| `Assets/Scripts/Gameplay/Taxi/PassengerJob.cs` | Festes Angebot einer konkreten Fahrt |
| `Assets/Scripts/Gameplay/Taxi/TripManager.cs` | Fahrt starten/beenden; Ergebnis berechnen lassen und Münzen einmal gutschreiben |
| `Assets/Scripts/Gameplay/Taxi/RatingSpeedModel.cs` | Geschwindigkeitsverteilung, Anpassung und Speicherung der Schwierigkeit |
| `Assets/Scripts/Gameplay/Reviews/ReviewRating.cs` | Sterneschwellen |
| `Assets/Scripts/Core/CurrencyManager.cs` | Guthaben speichern und Änderungen an die Oberfläche melden |
| `Tests/RewardChecks.cs` | Formel-, Grenzwert- und Schwierigkeitsspeichertests |
| `Tests/CurrencyChecks.cs` | Gespeichertes Guthaben und sofortige Anzeige prüfen |

Die Berechnung darf keine Münzen direkt schreiben. `TripManager` übernimmt die
Gutschrift; `CurrencyManager` ist die Geldverwaltung. So lassen sich Formeln mit
festen Eingaben testen, ohne eine Stadt aufzubauen oder einen Spielstand anzufassen.

### Vorschläge ausschließlich für Fahrtbelohnungen

Die folgenden Zahlen sind **Versuchswerte zur Diskussion**, keine beschlossenen Regeln.

**A. Grundvergütung plus Pünktlichkeitsbonus.** Zum Beispiel 70 % des Angebots bei
erfolgreicher Abgabe, weitere 30 % bei Pünktlichkeit. Bei 408 Münzen und Rundung erst
am Ende wären das 408 pünktlich bzw. 286 spät. Das entschärft den Sprung auf null,
kann aber Zeitdruck unwichtig machen. Sehr große Verspätungen gesondert betrachten.

**B. Sanfter Abzug nach Fristablauf.** Zum Beispiel voller Preis bis zur Frist,
danach linear auf null bis 150 % der Frist:
`Payout = round(P × clamp(1 − 2 × max(0, elapsed/T − 1), 0, 1))`.
Bei 110 % der Frist bleiben 80 % des Preises. Ziel muss trotzdem erreicht sein.
Vorteil: kein extremer Verlust durch eine Zehntelsekunde; Nachteil: Frist wirkt
weniger verbindlich. Anzeige und Angebot müssen die Regel später verständlich machen.

**C. Trinkgeld passend zu Sternen.** Beispiel: 3 Sterne ohne Bonus, 4 Sterne +10 %,
5 Sterne +20 %. Zuerst festlegen, auf welchen Grundbetrag der Bonus angewandt wird
und wie verspätete Fahrten behandelt werden. Erst am Ende runden. So belohnt das
System gute Fahrten ausdrücklich; harte Sterneschwellen erzeugen aber neue Sprünge.

**D. Fristen besser an die Strecke anpassen.** Kurze Fahrten brauchen relativ viel
Zeit für Aufnahme, Kurven und Abgabe. Eine feste Zeitreserve plus streckenabhängiger
Fahrzeit könnte fairer sein. Später können Kurven/Kreuzungen der berechneten Route
einfließen. Keine zusätzliche Belohnung für tatsächlich gefahrene Umwege vergeben.

**E. Schwierigkeit vorsichtiger anpassen.** Eine geglättete Leistung über mehrere
Fahrten reduziert Schwankungen. Prüfen, ob schnelles Fahren ungewollt mit sofort
unverhältnismäßig engeren Fristen bestraft wird. Bei Tests außerdem langsamstes und
schnellstes vorhandenes Auto verwenden, ohne deren Eigenschaften zu verändern.

A, B und C nicht ungeprüft kombinieren: Das könnte die durchschnittliche Auszahlung
stark erhöhen. Zunächst eine Variante mit konstanten Fristen messen.

### Reproduzierbare Tests

1. Ausgangsstand, Stadt-Seed, Auto, Schwierigkeitsmittelwert und Variante notieren.
2. Dieselben kurzen, mittleren und langen Strecken verwenden. Zufall bei Angebot
   und Schwierigkeit für Vergleichstests durch feste Eingaben ersetzen, nicht
   dauerhaft aus dem Spiel entfernen.
3. Pro Fall Distanz, Frist, Dauer, Sterne, Auszahlung und gesamte Spielzeit mit
   Anfahrt notieren. `Münzen pro Minute = Auszahlung / gesamte Sekunden × 60`.
4. Je Variante mehrere echte Fahrten durchführen: Formeltests allein messen weder
   Frust noch Spaß. Pünktlichkeitsquote und Häufigkeit von Nullauszahlungen vergleichen.
5. Grenzfälle testen: exakt Fristende, knapp davor/danach, Sterneschwellen, kleinste
   und größte Frist, kein Ziel erreicht, doppelte Abgabe, Guthaben nahe `int.MaxValue`.
6. Speicherung prüfen: neues Testprofil 0; Fahrt abschließen; Anwendung vollständig
   schließen und öffnen; Guthaben, Auto, Kamera und Schwierigkeit vergleichen.
7. Auf dem Zielgerät zusätzlich App beenden/neustarten und eine Installation ohne
   vorherige App-Daten testen. Editor-Tests ersetzen diese Plattformprüfung nicht.

Automatische Prüfungen nur in einer **separaten Projektkopie** mit Product Name
`FahrpreisFieberQA` ausführen. Die Prüfungen schreiben und löschen Test-Spielstände.
Nie `PlayerPrefs.DeleteAll()` als normale Spielinitialisierung verwenden: Das würde
bei jedem Start Fortschritt zerstören. Details zu den vorhandenen Prüfungen stehen
in `Tests/README.md`.

Beispiel für eine vorbereitete Windows-Testkopie mit RewardChecks.cs unter Assets/Editor:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.2.12f1\Editor\Unity.exe' `
  -batchmode -projectPath 'C:\Dev\FahrpreisFieberQA' `
  -executeMethod RewardQA.Validate -logFile 'C:\Dev\reward-qa.log'
```

Den Editorpfad bei abweichender Installation anpassen. Die Testmethode beendet
Unity selbst. Erfolg steht als `REWARD_QA_PASS` im Log. Den laufenden Editor nicht
auf demselben Projekt gleichzeitig per Batch starten.

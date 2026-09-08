# Validation

Run Unity checks only in a temporary project copy using Unity 6000.2.12f1. Copy
the chosen check into that copy's Assets/Editor, then invoke its executeMethod
with -batchmode. Checks change scene state and exit Unity. Set the copy's
productName to FahrpreisFieberQA; Play Mode checks write test PlayerPrefs.

- UrbanLayoutChecks.cs: standalone console check, compiled alongside CityLayout.cs
  and CityLots.cs. Tests seven seeds for connected streets, loops, shortest routes,
  parcel determinism, street frontage, and nonoverlapping compact footprints, occupied-cell masks, and connected irregular wings.
- UrbanChecks.cs: UrbanQA.Validate. Checks authored camera options, saved mode,
  all camera views, smaller models/collider, and parcel streaming reference counts.
- RevisionAssetChecks.cs: RevisionQA.Validate. Covers scene references, UI,
  passengers, streaming queues, minimap, review cards, and car/passenger proportions.
- GarageChecks.cs: GarageQA.Validate. Checks purchases, ownership, selected speed,
  wheel/body motion, and five-tier offline reviews.
- HandlingChecks.cs: HandlingQA.Validate. Checks arcade driving, controlled drift,
  external torque suppression, reverse, rolling passenger actions, and routing.
- CurrencyChecks.cs: CurrencyQA.Validate. Checks saved balance and immediate HUD updates.

The superseded maze-only and duplicate city asset checks have been removed.
Inspect generated images in /tmp/fahrpreis-*.png. Test phone touch ergonomics,
first-person comfort, and performance during longer drives on the target device.

RefinementChecks.cs (`RefinementQA.Validate`) adds constant-input circle fitting,
left/right symmetry, timestep comparison, approved price checks, and nonrectangular
mesh/collider checks, then runs UrbanQA.Validate. Copy UrbanChecks.cs alongside it.

RewardChecks.cs (`RewardQA.Validate`) verifies a fresh zero balance, quote and
deadline rules, arrival/payment boundaries, and difficulty reload. It refuses to
run unless productName is FahrpreisFieberQA and clears that test profile.

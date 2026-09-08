# Garage and passenger reviews

Click the car in MainMenu to open the garage. Purchases immediately select the car;
owned cars can be selected without another charge. The free City Cab is selected
for new players. Ownership, selected car, and coins persist through PlayerPrefs.
The obsolete taxi model has been removed; both scenes use the new lineup.

| Car | Top speed (world units/s) | Coins |
| --- | ---: | ---: |
| City Cab | 10 | Free |
| Mini | 12 | 2,000 |
| Wagon | 14 | 7,000 |
| Pickup | 16 | 22,000 |
| Muscle | 18 | 65,000 |
| Sport | 20 | 180,000 |

Edit `Assets/Prefabs/Cars/CarCatalog.asset` to adjust the approved lineup. All six
model prefabs and scene instances are authored and editable, as is the store UI
under MainMenu's Canvas/CarStore (inactive until opened). No UI or car geometry is
constructed at runtime. CarVisualMotion spins wheels from signed vehicle speed
and adds damped body pitch/roll; the physics collider does not tilt with the body.
The existing steering and pickup/drop-off controls are retained.

Reviews use elapsed time relative to the deadline: at most 75% gives 5 stars,
over 75% through 90% gives 4, over 90% through 100% gives 3, over 100% through 125%
gives 2, and over 125% gives 1. The same ReviewRating calculation controls both
review selection and displayed stars. Each of the five banks contains 200
predefined texts, with no immediate repeat within a bank. Payout rules are unchanged.

Validation uses `Tests/GarageChecks.cs` in an isolated project copy with a distinct
productName, FahrpreisFieberQA. It writes test PlayerPrefs, enters Play Mode,
and exits Unity. Never run this test in the working editor or with real preferences.
Invoke with `-executeMethod GarageQA.Validate`; it tests all purchases, insufficient
funds, reselection, scene persistence, gameplay speed, wheel/body motion, rating
boundaries, star counts, and all 1,000 unique review texts. It also renders the menu,
store, and gameplay to `/tmp/fahrpreis-*.png`.

The current trip payout formulas, Windows onboarding, and reward experiments are documented in `Belohnungssystem-Leitfaden.md` (German).

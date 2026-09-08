# Handling and navigation

The taxi retains the existing two-sided touch input. A bicycle steering model derives turn radius from wheelbase, steering angle,
and speed. An arcade lateral-acceleration limit of 96 keeps fast turns compact (roughly 3.3 units radius at speed 10 and 4.5 at speed 20). Velocity is integrated
at the middle of the arc. Arcade motion owns yaw while
Rigidbody collisions still resolve translation. Lateral momentum is damped and
capped at 2 world units/second, preventing uncontrolled sliding. Both sides brake
first, then reverse after a 0.4-second stopped hold. Gameplay top speed remains 10.
Pickup and drop-off each allow planar speed up to and including 4; neither freezes
the car. All tuning values are serialized on the car in Game.unity.

The six blocky taxi models use a uniform 0.8 prefab scale on all three axes.
The minimap uses logical city tile data and an antialiased circular UI shader.
A* finds the shortest four-neighbor road path using distance as cost; navigation
checks for changed start/destination tiles every 0.1 seconds and clears after trips.
The yellow path recalculates after deviations, independent of streamed objects.

Game and MainMenu each contain a CurrencyManager for direct scene play; the
persistent singleton keeps one live balance. UI binders subscribe to balance
changes and refresh on enabling. Game menu/coin panels use the menu palette.

Validation: Unity asset/render checks, simulated arcade motion and routing checks,
plus independent breadth-first shortest-route comparisons across seven seeds.
Phone touch feel and performance still need device playtesting.

For current street generation, shared building parcels, car scaling, and camera modes, see `City-and-cameras.md`.

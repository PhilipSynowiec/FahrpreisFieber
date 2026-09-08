# City layout, buildings, and camera views

Street generation uses a seeded, weighted graph of jittered junctions. Neighborhood
connection density varies spatially. A single Kruskal traversal both connects the
network and retains locally inexpensive alternative edges; no uniform node-degree
rule or disconnected-road repair pass is applied. Shared boundary portals make
independently generated regions connect. Some areas are denser and others quieter.

Building footprints grow from street frontage into compact connected shapes,
including L/T-like wings. Their bounds are at most four tiles per side, with
slenderness constrained to prevent long strips. Each footprint touches a road and
belongs to one building. Interior land without frontage becomes courtyard/green space.
Parcel ownership is in the logical data used by the minimap and passenger spawning.
One shared building visual is reference-counted across its visible tiles, so it
survives partial unloading and disappears when its final tile unloads. The single exterior mesh follows occupied cells, including concave corners, with
windows only on exterior walls. Colliders match occupied cells rather than filling
the bounding rectangle. Sidewalk setbacks remain constant. Logical caches are bounded.

All six car models and the gameplay collider are uniformly 20% smaller. Wheel
animation accounts for the model scale. Speeds, prices, and touch input are unchanged.

The menu offers ELEVATED, DRIVER, and CHASE. Selection is saved under camera.view.
Elevated preserves the existing pose. Driver follows the driving position and
heading, hides the player's exterior model, and does not add a cockpit. Chase
follows behind the car with smoothing and pulls forward when buildings obstruct it.

Removed unused code includes legacy trip-result rating/average-speed fields,
PassengerJob's unused saved speed, the unused trip coordinate helper, and the
unused tile-spawn event and tile-data accessor. The old top-down camera flag is
replaced by the three explicit modes.

Validation: UrbanLayoutChecks checks connectivity, alternate routes, shortest
paths, deterministic parcels, nonoverlap, and frontage across seven seeds.
UrbanChecks runs Unity scene, parcel streaming, uniform scale, menu selection,
saved camera mode, and view visibility checks in an isolated project copy.
Device driving feel and first-person motion comfort still need hands-on playtesting.

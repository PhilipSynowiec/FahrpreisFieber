# Current art assets

Edit the authored scenes and prefabs directly in Unity. The old one-time city/UI
migration scripts and their stale geometry manifests have been removed.

City assets have a 10 m footprint and share the URP materials in Art/Materials/City.
The three passenger prefabs have editable body parts and PassengerMotion animation.
The six car prefabs are in Prefabs/Cars, with wheel pivots and CarVisualMotion.
The unused imported taxi model and its texture/material have been removed.

UI is authored in MainMenu, Game, and OfferWidget. It uses yellow/navy raised
buttons and Electronic Highway Sign text; reviews use a light left-aligned card
with Roboto Bold and the speed-based 1–5-star scale. OfflineReviewService selects
from 1,000 predefined quotes in Gameplay/Reviews.

The minimap draws logical roads and navigation using analytic circular clipping.
The destination marker in the world is a rotating voxel arrow.

AuthorGarage.cs is an opt-in editor authoring source kept outside Assets. The
current scenes/prefabs are the authoritative assets; Build refuses to overwrite
an existing catalog.

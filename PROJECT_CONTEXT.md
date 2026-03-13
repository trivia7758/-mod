# Project Context (Last updated: 2026-02-03)

## Current focus
- Integrating SRPG battle prototype (Godot 4) into this project and wiring it to story flow.
- Replace auto-slicing map approach with manual TileSet + TileMap workflow.
- Fix battle input/selection/undo behaviors and UI interaction.

## Key recent changes
- Story system moved to DSL: data/story.dsl parsed by scripts/StoryDSL.gd; executed by scripts/StoryScene.gd.
- StoryScene.gd now runs DSL events (move/face/talk/wait/bg/location/morality/choice/hide_dialogue), handles input through DialogueBox.gd, and changes to scenes/Battle.tscn at end.
- Story sprites use ssets/sprites/story/test_r1_ck.png and 	est_r2_ck.png (front/back) sliced into frames; talk indicator positioned above head.
- Morality bar uses text  中立值 (was previously dark/grey; check UI styles if needed).

## Battle system (prototype)
Scripts under scripts/battle/:
- attle_controller.gd: state machine, input, selection, move, action menu, attack/undo/cancel. UI is created at runtime (CanvasLayer -> UI -> ActionMenu/Info). Input fixed for mobile-style click-only interactions. Added undo: clicking outside menu or cancel returns unit to original tile.
- grid_map.gd: terrain grid (logical map) with ebuild(new_size).
- pathfinding.gd: Dijkstra reachable + parent; build_path.
- highlight_layer.gd: draw move/attack/hover; uses origin and 	ile_size.
- unit.gd: BattleUnit with hp/atk/def/mov, move_to tween, hp label.
- enemy_ai.gd: simple AI with occupancy checks.

Scene:
- scenes/Battle.tscn contains: Battle (Node2D) + LevelMap (TileMap) + BattleMap + Pathfinding + Highlight + Units + EnemyAI.
- UI nodes are runtime-created in attle_controller.gd (no static UI in scene).

## Battle input behavior (current)
- Select unit -> move range highlights.
- Click unit again -> ActionMenu (Attack/Wait) without moving.
- Move -> ActionMenu appears near unit.
- Click outside menu -> cancel selection and undo move back to original tile.
- In Attacking: click enemy in range -> attack; click elsewhere -> cancel and undo move.

## Map workflow change
- Removed auto-slicing script approach.
- Manual TileSet + TileMap workflow expected:
  - Create TileSet ssets/tilesets/battle/level0_tileset.tres from ssets/ui/backgrounds/battle/level0.JPG.
  - Tile size: 48x48, margins 0, separation 0.
  - Use TileMap node LevelMap in scenes/Battle.tscn and paint tiles manually.
- attle_controller.gd syncs logical grid size from LevelMap.get_used_rect() and sets origin.

## Known issues / next steps
- Godot warnings about TileSet atlas margins/separation should be positive indicate negative values or invalid atlas settings.
- Errors like Cannot create tile... outside the texture occur when image size isn’t a multiple of 48; need to crop or set atlas region to multiples of 48.
- level_map.gd script was removed; a stub scripts/battle/level_map.gd exists only to clear old cache references.
- User wants: proper battle map setup, stable selection/cancel behavior, and eventually terrain editing.

## Assets / paths
- Map image: ssets/ui/backgrounds/battle/level0.JPG (needs 48x48 multiple).
- Tileset target: ssets/tilesets/battle/level0_tileset.tres.
- Battle maps dir: scenes/battle/maps/ (unused yet).

## Notes
- User prefers mobile-style click-only interactions (no right-click/ESC).
- If Battle still eats clicks, check UI mouse_filter settings; UI is IGNORE, ActionMenu is STOP.
- Any cached errors referencing es://scripts/battle/level_map.gd should be cleared by project reload.

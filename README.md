# Mapper
Mapper is tool for generating heightmaps from real world elevation data. Mapper takes elevation data from Mapbox and converts it to heightmap files compatible with Unity and Unreal. Heightmap files are RAW 16-bit images. Mapper supports tiling so, multiple heightmaps can be stitched together seamlessly.

Mapper is a Windows only WPF app. [MapboxNet](https://github.com/AliFlux/MapboxNet) is used for rendering. You must have a Mapbox API key use this tool.

# Usage
Mapper allows the user to graphically select an area to convert to a heightmap. Drag the selection box over the area you want to map.

![image](https://github.com/vazgriz/Mapper/assets/7607513/6677e968-1ae7-4fa4-9bff-eb14c2033100)

The user can configure several grid settings to control the heightmap output. The current grid settings can be saved as a JSON file.

The `Grid size` parameter controls the size of the selection on the map in kilometers.

`Output size` controls the total dimensions of the heightmap data for all tiles. Common sizes are 1024, 2048, and 4096.

`Tile count` controls the number of tile subdivisions of the heightmap data. `Tile size` displays the size of an individual tile. For example, if `Output size` is 8192 and `Tile count` is 2, then each tile will have size 4096.

`Output size` must be evenly divisible by `Tile count`.

The output file will have the dimensions `Tile size + 1` by `Tile size + 1`. Adding 1 is needed to ensure that neighboring tiles have 1 overlapping row/column.

Click the "Inspect" button to download the elevation data tiles for the selected area. Elevation data is cached at `AppData\Roaming\Vazgriz\Mapper\tilecache`.

When the tiles are downloaded, Mapper displays the minimum and maximum heights of the selected terrain. You can copy these values to the `Custom height min` and `Custom height max` parameter to control the heightmap values of the output. Or you can alter the values to select a different height range to output.

![image](https://github.com/vazgriz/Mapper/assets/7607513/0d959463-0678-4117-a53d-ee36c540b75d)

For example, the height values here are from 1104 meters to 2386 meters. The height difference is 1282 m. The custom height parameters allow you to control how these elevation values are mapped to the 0-65535 heightmap range.

I've selected a custom height range of 1100 m to 2400 m, giving a custom height range of 1300 m. That means the heightmap should be scaled to 1300 m size when imported into a game engine.

`Flip output` inverts the Y value of the output heightmap. If false, then Y+ is north. If true Y- is north. X- and X+ are always west and east respectively.

`Apply water offset` controls whether water features are included in the output heightmap. Certain water features like rivers, lakes, and oceans are marked in the Mapbox data. Mapper can use this data to lower areas on the heightmap covered by water. The depth the terrain is lowered is controlled by `Water offset`. This does not produce accurate height data for underwater areas, but it does mark the location of water features so artists can modify it later.

`Force .zip export` causes a zip archive to be created even if only one tile is output. If multiple tiles are output, a zip archive is always exported. A zip export contains the tiles and the JSON file that generated the image. The JSON file is equivalent to a grid settings save file.

![image](https://github.com/vazgriz/Mapper/assets/7607513/91241b81-0caf-4dfa-83ae-548dac3c6096)

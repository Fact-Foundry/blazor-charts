#!/usr/bin/env python3
"""Convert Natural Earth 110m GeoJSON to C# WorldGeometry.cs with SVG paths."""

import json
import sys

VIEWBOX_W = 1000
VIEWBOX_H = 500

def lon_to_x(lon):
    return (lon + 180) / 360 * VIEWBOX_W

def lat_to_y(lat):
    return (90 - lat) / 180 * VIEWBOX_H

def coords_to_path(rings):
    """Convert a list of coordinate rings to an SVG path string."""
    parts = []
    for ring in rings:
        for i, (lon, lat) in enumerate(ring):
            x = lon_to_x(lon)
            y = lat_to_y(lat)
            cmd = "M" if i == 0 else "L"
            parts.append(f"{cmd}{x:.0f} {y:.0f}")
        parts.append("Z")
    return " ".join(parts)

def geometry_to_path(geom):
    """Handle both Polygon and MultiPolygon geometries."""
    if geom["type"] == "Polygon":
        return coords_to_path(geom["coordinates"])
    elif geom["type"] == "MultiPolygon":
        all_rings = []
        for polygon in geom["coordinates"]:
            all_rings.extend(polygon)
        return coords_to_path(all_rings)
    return ""

def main():
    with open("/tmp/ne_110m_countries.geojson") as f:
        data = json.load(f)

    countries = {}
    names = {}

    for feature in data["features"]:
        props = feature["properties"]
        iso = props.get("ISO_A2", "-99")

        # Skip invalid codes
        if iso in ("-99", "-1", None, ""):
            # Try ISO_A2_EH as fallback
            iso = props.get("ISO_A2_EH", "")
            if iso in ("-99", "-1", None, ""):
                continue

        name = props.get("NAME", props.get("ADMIN", iso))
        path = geometry_to_path(feature["geometry"])

        if not path:
            continue

        # Skip Antarctica
        if iso == "AQ":
            continue

        countries[iso] = path
        names[iso] = name

    # Sort by code
    sorted_codes = sorted(countries.keys())

    # Generate C# file
    print('namespace FactFoundry.Blazor.Charts.Geo;')
    print()
    print('public static class WorldGeometry')
    print('{')
    print('    public static readonly IReadOnlyDictionary<string, string> CountryPaths = new Dictionary<string, string>')
    print('    {')

    for code in sorted_codes:
        path = countries[code]
        # Escape any quotes in path (shouldn't have any, but safe)
        path = path.replace('"', '\\"')
        print(f'        ["{code}"] = "{path}",')

    print('    };')
    print()
    print('    public static readonly IReadOnlyDictionary<string, string> CountryNames = new Dictionary<string, string>')
    print('    {')

    for code in sorted_codes:
        name = names[code].replace('"', '\\"').replace("'", "\\'")
        print(f'        ["{code}"] = "{name}",')

    print('    };')
    print('}')

    print(f"\n// {len(sorted_codes)} countries generated", file=sys.stderr)

if __name__ == "__main__":
    main()

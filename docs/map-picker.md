# Map Picker

The map picker is a form input type that renders a small interactive MapBox map inline in a form,
allowing users to set a geographic location by clicking or dragging a pin. The selected coordinates
are written to two numeric parameters — one for latitude and one for longitude.

## Configuration

Add `input-type="map"` to your latitude and longitude parameters. Both parameters must have
`type="double"`.

```xml
<parameters>
  <add name="Latitude"
       type="double"
       input-type="map"
       prompt="true"
       label="Latitude" />
  <add name="Longitude"
       type="double"
       input-type="map"
       prompt="true"
       label="Longitude" />
</parameters>
```

The system determines which parameter is latitude and which is longitude via `input-capture`.
If your parameter names are `Latitude` and `Longitude` (case-insensitive), `input-capture` is
set automatically. For any other names, set it explicitly:

```xml
<add name="Lat"
     type="double"
     input-type="map"
     input-capture="latitude"
     prompt="true"
     label="Latitude" />
<add name="Lng"
     type="double"
     input-type="map"
     input-capture="longitude"
     prompt="true"
     label="Longitude" />
```

`input-capture` accepts `latitude` or `longitude`.

## How It Renders

The latitude parameter renders:
1. A 300px MapBox map with a draggable red pin and navigation/geolocate controls
2. A numeric text input for latitude below the map

The longitude parameter renders:
1. A numeric text input for longitude (no second map)

The map appears above the latitude input. If you want the map to appear in a specific position
in the form relative to other fields, place the latitude parameter where you want the map to appear.

## User Interactions

| Action | Effect |
|---|---|
| Click anywhere on the map | Moves the pin to that location; updates both inputs |
| Drag the pin | Same as click — updates both inputs on release |
| Type in the latitude input | Flies the map to the new position |
| Type in the longitude input | Flies the map to the new position |
| Use the geolocate button | Centers the map on the device's current location (does not set the pin — click after locating) |

## Existing vs. New Records

- **New record** (lat/lon values are `0` or empty): the map opens at zoom level 2 showing the
  full world. The pin is placed at 0,0. The user clicks to place it.
- **Existing record** (lat/lon values are non-zero): the map opens at zoom level 14 centered on
  the existing coordinates with the pin already placed.

## Requirements

- A valid **MapBox token** must be configured in the site's Transformalize settings.
- The `mapbox-gl` library version `3.6.0` must be registered as a named resource in the module.
  It is — no extra setup is required.

## Hidden vs. Visible Lat/Lon

If you want the numeric inputs hidden from the user (map-only interaction), set `prompt="false"`
and `input="false"` on the lat/lon parameters, and use a separate mechanism to surface them. For
typical edit-form usage, keeping `prompt="true"` gives power users the option to type coordinates
directly.

## Modal Forms

The map picker works in modal forms without any extra configuration. `map.resize()` is called
on map load to handle layout reflow inside the modal iframe.

## Validation

The Transformalize configuration parser validates `input-type="map"` parameters at load time:

- `type` must be `double`
- `input-capture` must be `latitude` or `longitude` (or the parameter name must be `Latitude`/`Longitude`)

Misconfigured parameters produce a clear error in the process log before the form renders.

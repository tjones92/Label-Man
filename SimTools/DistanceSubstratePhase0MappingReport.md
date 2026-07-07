# Directive 4a Phase 0: Region Inventory and 4b Mapping Report

Directive 4a leaves the live six-region taxonomy unchanged. This report inventories the current live region substrate and the regionId consumers that must be reconciled before 4b migrates to the seven-region map.

## Current live regions

All six `MarketRegion` resources are embedded in `chart_manager.tscn`; there are no `.tres` region resources. `majorCities` is unset on every live region. Population values are millions, so the nominal regional market base is `population * 1,000,000` before buying-population, genre, segregation, and format factors.

| regionId | regionName | population | market base | urbanization | income | youth | black pop. | colleges | integration | progressivism | insularity | adoption |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| `midwest` | Midwest | 51.6 | 51,600,000 | 0.70 | 1.05 | 0.35 | 0.08 | 420 | 0.40 | 0.45 | 0.45 | 1.20 |
| `eastcoast` | East Coast | 52.2 | 52,200,000 | 0.70 | 1.15 | 0.32 | 0.10 | 480 | 0.45 | 0.65 | 0.20 | 1.60 |
| `westcoast` | West Coast | 20.3 | 20,300,000 | 0.82 | 1.18 | 0.38 | 0.05 | 170 | 0.50 | 0.65 | 0.25 | 1.50 |
| `deepsouth` | Deep South | 15.0 | 15,000,000 | 0.48 | 0.78 | 0.40 | 0.33 | 130 | 0.05 | 0.15 | 0.75 | 0.80 |
| `rockies` | Rockies | 4.6 | 4,600,000 | 0.58 | 0.95 | 0.39 | 0.01 | 50 | 0.55 | 0.30 | 0.70 | 0.60 |
| `southwest` | Southwest | 14.2 | 14,200,000 | 0.62 | 0.90 | 0.41 | 0.09 | 110 | 0.25 | 0.35 | 0.55 | 1.00 |

## Exported infrastructure fields

`GenrePreference[]` is present for all regions and uses the same schema everywhere: `genre`, `baseAcceptance`, `affinity`, `hasLocalScene`, `yearlyDrift`.

`MediaInfrastructure` fields are `totalRadioStations`, `hasTop40Stations`, `hasRnBStations`, `hasCountryStations`, `hasFMUnderground`, `radioReach`, `payolaSusceptibility`, `tvMarketRank`, `hasLocalMusicShow`, `bandstandReach`, `jukeboxCount`, `concertVenueCount`. There is no radio-difficulty field.

`MusicInfrastructure` fields are `recordingStudioCount`, `studioQuality`, `hasSignatureSound`, `signatureSoundDescription`, `localLabelCount`, `hasMajorLabelPresence`, `talentPool`, `talentDevelopment`, `clubCount`, `theaterCount`, `hasChitlinCircuitVenues`.

`DistributionNetwork` fields are `difficulty`, `recordStoreCount`, `departmentStoreCount`, `inventoryDepth`, `hasIndieDistribution`, `hasOneStopDistributors`. Directive 4a adds a separate per-city `DistributionNetwork` on `MarketCity`; it does not touch these live region-level distribution resources.

## regionId consumers

`Data/AILabel.cs` stores `homeRegion`, `strongRegions`, and `distributionRegions`; `HasDistributionInRegion` also reads `activeDeal.grantedRegions`.

`Data/DistributionDeal.cs` stores `grantedRegions`.

`Systems/AILabelFactory.cs` has three hard-coded six-region sites that must migrate in 4b: `CityToRegion`, the `allRegions` array inside `GetDistributionRegions`, and `GetAdjacentRegion`.

`Systems/ChartManager.cs` owns the live `allRegions` array, initializes `RegionalRecordData` by current `regionId`, iterates every region for launch stock, weekly regional sales, restock, breakout propagation, and chart points, and exposes `GetRegionById` / `GetAllRegions`.

`Systems/ChartSimulator.cs` reads `regionId` for launch factor, initial regional stock, sales, saturation, potential audience, and chart points.

`Systems/AlbumSimulator.cs` shares regional sales through `CalculateRegionalSales`.

`Systems/CompetitorManager.cs` reads region ids for release prewarm/runtime regional data, release stock, distribution deals, album pipeline snapshots, and label distribution coverage.

`Data/RegionalRecordData.cs`, `Data/AlbumProject.cs`, `Data/SimulatedArtist.cs`, `Data/ArtistPublicProfile.cs`, and `Data/JournalisticDescriptor.cs` all carry or consume region ids for runtime display, snapshots, or diagnostics.

`Systems/LabelGenerator.cs` and `Systems/LabelLifecycleManager.cs` also contain region logic, but they are out of scope for 4a because the live path is `AILabelFactory.GenerateAllLabels`.

## 4b target mapping

| current region | 4b target region(s) | 4a city assignments | mismatch notes |
| --- | --- | --- | --- |
| `eastcoast` | East Coast | New York, Boston, Philadelphia, Baltimore, Washington | Clean for roster cities. British/import placeholders and domestic unmapped fallbacks route to the New York hub in 4a. |
| `midwest` | Great Lakes / Great Plains | Chicago, Detroit, Cleveland, Cincinnati / Minneapolis, St. Louis, Kansas City, Omaha | Known split. St. Louis moves to Great Plains for 4b even though current `AILabelFactory.CityToRegion` maps it to `midwest`. Current procedural non-roster cities Indianapolis and Milwaukee also map to `midwest` and need a 4b policy. |
| `westcoast` | West Coast | Los Angeles, San Francisco, Seattle, Portland | Roster maps cleanly. Current procedural non-roster cities Oakland, Hollywood, and Pasadena still map to `westcoast` and fall back to Los Angeles in 4a. |
| `deepsouth` | Deep South | Nashville, Memphis, Atlanta, New Orleans, Miami | Roster maps cleanly. Jackson is domestic-unmapped and falls back to Nashville in 4a. |
| `southwest` | Southwest | Dallas, Houston, San Antonio, Phoenix, Albuquerque | Existing `AILabelFactory.CityToRegion` only names Houston and Dallas; San Antonio, Phoenix, and Albuquerque are new 4a roster cities and need explicit 4b taxonomy wiring before generation can target them. |
| `rockies` | Rockies | Denver, Salt Lake City, Billings | Existing `AILabelFactory.CityToRegion` has no live city strings for this region; 4b must add explicit city generation/mapping if labels are expected to originate there. |

## 4a data-model notes

`MarketCity.mapCoords` uses an equirectangular projection: `x = longitude * cos(38deg) * 50`, `y = latitude * 50`. Distances exported by `distance-matrix.csv` are proportional map units, not miles.

Per-city `recordStoreCount` and `departmentStoreCount` are rough 1960 metro-population scalars: record stores use `metroPopulationMillions * 28 * (0.75 + inventoryDepth * 0.5)`; department stores use `metroPopulationMillions * 7`. These are 4b calibration inputs only.

The 4a fallback policy is implemented as data plumbing only: `hq-match` uses exact/alias city matches, `international` uses the existing East Coast routing precedent and assigns New York, and `domestic-unmapped` assigns the current-region hub.

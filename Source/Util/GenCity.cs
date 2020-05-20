﻿using System.Linq;
using RimWorld;
using RimWorld.BaseGen;
using Verse;
using Verse.AI.Group;

namespace Cities {
    public static class GenCity {

        public static Faction RandomCityFaction(System.Predicate<Faction> filter = null) {
            return (from x in Find.World.factionManager.AllFactionsListForReading
                where !x.def.isPlayer && !x.def.hidden && !TechLevelUtility.IsNeolithicOrWorse(x.def.techLevel) && (filter == null || filter(x))
                select x).RandomElementByWeightWithFallback(f => f.def.settlementGenerationWeight);
        }

        public static TerrainDef RandomFloor(Map map, bool carpets = false) {
            return BaseGenUtility.RandomBasicFloorDef(map.ParentFaction, carpets);
        }

        public static ThingDef RandomWallStuff(Map map, bool expensive = false) {
            return RandomStuff(ThingDefOf.Wall, map, expensive);
        }

        public static ThingDef RandomStuff(ThingDef thing, Map map, bool expensive = false) {
            if (!thing.MadeFromStuff) {
                return null;
            }
            else if (expensive) {
                return GenStuff.RandomStuffByCommonalityFor(thing, map.ParentFaction.def.techLevel);
            }
            else {
                return GenStuff.RandomStuffInexpensiveFor(thing, map.ParentFaction);
            }
        }

        public static Pawn SpawnInhabitant(IntVec3 pos, Map map, LordJob job = null, bool friendlyJob = false, bool randomWorkSpot = false, PawnKindDef kind = null) {
            if (job == null || (!friendlyJob && !map.ParentFaction.HostileTo(Faction.OfPlayer))) {
                var workPos = randomWorkSpot ? CellRect.WholeMap(map).RandomCell : pos;
                job = map.Parent is Citadel
                    ? (LordJob) new LordJob_LiveInCitadel(FindPawnSpot(workPos, map))
                    : new LordJob_LiveInCity(FindPawnSpot(workPos, map));
            }
            return SpawnInhabitant(pos, map, LordMaker.MakeNewLord(map.ParentFaction, job, map));
        }

        public static Pawn SpawnInhabitant(IntVec3 pos, Map map, Lord lord, PawnKindDef kind = null) {
            pos = FindPawnSpot(pos, map);

            var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                kind ?? map.ParentFaction.RandomPawnKind(),
                map.ParentFaction,
                PawnGenerationContext.NonPlayer,
                map.Tile,
                mustBeCapableOfViolence: true,
                forceGenerateNewPawn: true, /////
                inhabitant: true
            ));

            lord?.AddPawn(pawn);
            GenSpawn.Spawn(pawn, pos, map);
            return pawn;
        }

        public static IntVec3 FindPawnSpot(IntVec3 pos, Map map) {
            var maxAttempts = 50;
            while (maxAttempts > 0 && !pos.Walkable(map)) {
                maxAttempts--;
                pos = pos.RandomAdjacentCell8Way().ClampInsideMap(map);
            }
            return pos;
        }

        public static bool IsOwnedByCity(this Thing thing, Map map = null) {
            return (map ?? thing.Map)?.GetComponent<MapComponent_City>()?.cityOwnedThings.Contains(thing) ?? false;
        }

        public static void SetOwnedByCity(this Thing thing, bool owned, Map map/* = null*/) {
            try {
                if (owned) {
                    thing.SetForbidden(true, false);
                }

                // map = map ?? thing.Map;
                if (map?.Parent is City city && !city.Abandoned && !city.Faction.HostileTo(Faction.OfPlayer)) {
                    var cityOwnedThings = map.GetComponent<MapComponent_City>().cityOwnedThings;
                    if (owned) {
                        cityOwnedThings.Add(thing);
                    }
                    else {
                        cityOwnedThings.Remove(thing);
                    }
                }
            }
            catch (System.Exception e) {
                Log.Message("Failed to set city ownership [" + owned + "] on thing: " + thing + " (" + e + ")");
            }
        }
    }
}
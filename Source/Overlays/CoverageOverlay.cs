using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using HarmonyLib;
using TD_Enhancement_Pack.Utilities;

namespace TD_Enhancement_Pack.Overlays
{
	[StaticConstructorOnStartup]
	class CoverageOverlay : BaseOverlay
	{
		//CoverageType class to handle each def
		public static List<CoverageType> types = new List<CoverageType>();
		public static CoverageType activeType;

		static CoverageOverlay()
		{
			types.Clear();
			foreach (Type current in typeof(CoverageType).AllLeafSubclasses())
				types.Add((CoverageType)Activator.CreateInstance(current));
		}

		public CoverageOverlay() : base() { }

		public override bool ShowCell(int index)
		{
			return activeType?.ShowCell(index) ?? false;
		}

		public override Color GetCellExtraColor(int index)
		{
			return activeType?.GetCellExtraColor(index) ?? Color.white;
		}

		public override void Update()
		{
			//Find selected thing or thing to build
			ThingDef def = null;
			if (Find.DesignatorManager.SelectedDesignator is Designator_Place des)
				def = des.PlacingDef as ThingDef;
			if (def == null)
				def = Find.Selector.SingleSelectedThing.GetInnerIfMinified()?.def;
			if (def != null)
			{
				def = GenConstruct.BuiltDefOf(def) as ThingDef;
				foreach (CoverageType cov in types)
					if (cov.MakeActive(def))
					{
						SetDirty();
						if (cov.active) activeType = cov;
						else Clear();
					}

				if (dirty)  //From MakeActive or otherwise
					activeType?.Init();
			}
			else if (activeType != null)
			{
				Clear();
			}

			base.Update();
		}

		public override void Clear()
		{
			if (activeType != null)
			{
				activeType.active = false;
				activeType.Clear();
				activeType = null;
				SetDirty();
			}
		}

		public override void PostDraw()
		{
			activeType?.PostDraw();
		}

		public override bool ShouldAutoDraw() => Mod.settings.autoOverlayCoverage;
		public override IEnumerable<Type> AutoDesignator() => [ typeof(Designator_Place) ];
		public static Texture2D TexIcon = ContentFinder<Texture2D>.Get("UI/Buttons/Dev/Add", true);
		public override Texture2D Icon() => TexIcon;
		public override bool IconEnabled() => true;
		public override string IconTip() => "TD.ToggleCoverage".Translate();
	}

	[HarmonyPatch(typeof(TerrainGrid), "DoTerrainChangedEffects")]
	static class DoTerrainChangedEffects_Coverage_Patch
	{
		public static void Postfix(Map ___map)
		{
			if (___map == Find.CurrentMap)
				BaseOverlay.SetDirty(typeof(CoverageOverlay));//because moisture pumps change terrain
		}
	}
	//Buildings that cover an area with an aura shows total coverage
	public abstract class CoverageType
	{
		public bool active;

		public static Color coveredColor = Color.blue;
		protected static HashSet<IntVec3> covered = new HashSet<IntVec3>();

		public virtual bool ShowCell(int index) =>
			covered.Contains(Find.CurrentMap.cellIndices.IndexToCell(index));

		public virtual Color GetCellExtraColor(int index) => GetCoverageEdgeColor() * 0.5f;
		public virtual Color GetCoverageEdgeColor() => coveredColor;

		public abstract ThingDef[] PlacingDef();
		public virtual ThingDef[] CoverageDef() => PlacingDef();
		public virtual float Radius() => CoverageDef().FirstOrDefault()?.specialDisplayRadius ?? 0f;
		public virtual bool AppliesTo(Thing thing) => true;
		public bool MakeActive(ThingDef def)
		{
			bool nowActive = PlacingDef().Any(placing => def == placing);
			if (nowActive != active)
			{
				active = nowActive;
				return true;
			}
			return false;
		}

		public void Init()
		{
			var centers = new HashSet<IntVec3>(CoverageDef()
				.SelectMany(def => Find.CurrentMap.listerThings.ThingsOfDef(def).Where(AppliesTo)).Select(t => t.Position));

			centers.AddRange(Find.CurrentMap.listerThings.ThingsInGroup(ThingRequestGroup.Blueprint)
				.Where(bp => CoverageDef().Any(coverage => coverage == GenConstruct.BuiltDefOf(bp.def) as ThingDef) && AppliesTo(bp)).Select(t => t.Position).ToList());

			centers.AddRange(Find.CurrentMap.listerThings.ThingsInGroup(ThingRequestGroup.BuildingFrame)
				.Where(frame => CoverageDef().Any(coverage => coverage == GenConstruct.BuiltDefOf(frame.def) as ThingDef) && AppliesTo(frame)).Select(t => t.Position).ToList());

			covered.Clear();

			var radius = Radius();
			foreach (var center in centers)
			{
				var num = GenRadial.NumCellsInRadius(radius);
				for (var i = 0; i < num; i++)
					covered.Add(center + GenRadial.RadialPattern[i]);
			}
		}

		public void Clear()
		{
			covered.Clear();
		}

		public void PostDraw()
		{
			GenDraw.DrawFieldEdges(covered.ToList(), GetCoverageEdgeColor());
		}
	}

	//Things that have coverage
	//Could this list be automated? Things that have a range and that range overlap is not additive? Nah.
	[DefOf]
	public static class MoreThingDefOf
	{
		public static ThingDef MoisturePump;
		public static ThingDef SunLamp;
	}
	public class TradeBeaconType : CoverageType
	{
		public override ThingDef[] PlacingDef() => things;
		public override float Radius() => 7.9f;

		private ThingDef[] things;

		public TradeBeaconType()
		{
			things = DefDatabase<ThingDef>.AllDefs
				.WhereDefGuarded(x => x.PlaceWorkers?.Any(y => y is PlaceWorker_ShowTradeBeaconRadius) ?? false)
				.RemoveBlueprints()
				.ToArray();
		}
	}

	public class ChairType : CoverageType
	{
		readonly ThingDef[] things;
		private readonly float radius;
		public override ThingDef[] PlacingDef() => things;

		public ChairType()
		{
			things = DefDatabase<ThingDef>.AllDefs
				.WhereDefGuarded(x => x.building?.isSittable ?? false)
				.RemoveBlueprints()
				.ToArray();

			// Is this the best way to get this? *shrugs*
			radius = new IngestibleProperties().chairSearchRadius;
		}
		public override float Radius() => radius;
		public override bool AppliesTo(Thing thing)
		{
			var pos = thing.Position;
			return pos.GetThingList(Find.CurrentMap).Any(t => t.def.IsOrBuildsToTable()) ||
			       GenAdj.CardinalDirections.Any(dir => (pos + dir).GetThingList(Find.CurrentMap).Any(t => t.def.IsOrBuildsToTable()));
		}
	}

	public class SunLampType : CoverageType
	{
		public override ThingDef[] PlacingDef() => [MoreThingDefOf.SunLamp, ThingDefOf.HydroponicsBasin];
		public override ThingDef[] CoverageDef() => [MoreThingDefOf.SunLamp];
	}
	public class FirefoamPopperType : CoverageType
	{
		public override ThingDef[] PlacingDef() => [ThingDefOf.FirefoamPopper];
	}
	public class PsychicEmanatorType : CoverageType
	{
		public override ThingDef[] PlacingDef() => [ThingDefOf.PsychicEmanator];
	}

	[DefOf]
	public static class TrapThingDefOf
	{
		public static ThingDef TrapIED_HighExplosive;
		public static ThingDef TrapIED_Incendiary;
		public static ThingDef TrapIED_EMP;
		public static ThingDef TrapIED_Firefoam;
		public static ThingDef TrapIED_AntigrainWarhead;
	}
	public abstract class TrapType : CoverageType
	{
		public override ThingDef[] CoverageDef() => [TrapThingDefOf.TrapIED_HighExplosive];
		public override Color GetCoverageEdgeColor() => Color.red;
	}
	//Okay at this point I should make a class to handle all these at once ohwell
	public class IEDTrapType : TrapType
	{
		public override ThingDef[] PlacingDef() => [TrapThingDefOf.TrapIED_HighExplosive];
	}
	public class FireTrapType : TrapType
	{
		public override ThingDef[] PlacingDef() => [TrapThingDefOf.TrapIED_Incendiary];
	}
	public class EMPTrapType : TrapType
	{
		public override ThingDef[] PlacingDef() => [TrapThingDefOf.TrapIED_EMP];
	}
	public class FirefoamTrapType : TrapType
	{
		public override ThingDef[] PlacingDef() => [TrapThingDefOf.TrapIED_Firefoam];
	}
	public class AntigrainTrapType : TrapType
	{
		public override ThingDef[] PlacingDef() => [TrapThingDefOf.TrapIED_AntigrainWarhead];
	}

	//Moisture pumps show overlay AND coverage
	public class MoisturePumpType : CoverageType
	{
		private ThingDef[] things;

		public MoisturePumpType()
		{
			things = DefDatabase<ThingDef>.AllDefs
				.Where(x => x.comps.Any(x => x is CompProperties_TerrainPumpDry))
				.ToArray();
		}
		public override bool ShowCell(int index) =>
			Find.CurrentMap.terrainGrid.TerrainAt(index)?.driesTo != null ||
			Find.CurrentMap.terrainGrid.UnderTerrainAt(index)?.driesTo != null;

		public override Color GetCellExtraColor(int index) =>
			covered.Contains(Find.CurrentMap.cellIndices.IndexToCell(index))
				? coveredColor : Color.green;

		public override ThingDef[] PlacingDef() => things;
	}

	[DefOf]
	public static class DubsBadHygieneDefOf
	{
		[MayRequire("dubwise.dubsbadhygiene")] public static ThingDef IrrigationSprinkler;
		[MayRequire("dubwise.dubsbadhygiene")] public static ThingDef FireSprinkler;
	}

	public class IrrigationSprinkler : CoverageType
	{
		private ThingDef[] things;
		public IrrigationSprinkler()
		{
			things = [DubsBadHygieneDefOf.IrrigationSprinkler];
			things = things.Where(x => x != null).ToArray();
		}
		public override ThingDef[] PlacingDef() => things;
		public override float Radius() => 6.9f;
	}

	public class FireSprinkler : CoverageType
	{
		private ThingDef[] things;
		public FireSprinkler()
		{
			things = [DubsBadHygieneDefOf.FireSprinkler];
			things = things.Where(x => x != null).ToArray();
		}
		public override ThingDef[] PlacingDef() => things;
		public override float Radius() => 6.9f;
	}

	[DefOf]
	public static class BiotechDefOf
	{
		[MayRequireBiotech] public static ThingDef MechBooster;
	}

	public class MechBooster : CoverageType
	{
		public override ThingDef[] PlacingDef() => ModsConfig.BiotechActive ? [BiotechDefOf.MechBooster] : [];
		public override float Radius() => 9.9f;
	}

	[HarmonyPatch(typeof(ThingGrid), "Register")]
	public static class BuildingDirtierRegister
	{
		public static void Postfix(Thing t, Map ___map)
		{
			if (___map != Find.CurrentMap) return;


			if (CoverageOverlay.activeType != null &&
			    CoverageOverlay.activeType.CoverageDef()
				    .Any(coverage => coverage == GenConstruct.BuiltDefOf(t.def) as ThingDef))
				BaseOverlay.SetDirty(typeof(CoverageOverlay));
		}
	}

	[HarmonyPatch(typeof(ThingGrid), "Deregister")]
	public static class BuildingDirtierDeregister
	{
		public static void Postfix(Thing t, Map ___map)
		{
			BuildingDirtierRegister.Postfix(t, ___map);
		}
	}
}
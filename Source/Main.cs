using Brrainz;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using UnityEngine;
using Verse;
using static HarmonyLib.Code;

namespace Mattworld
{
	public class Mattworld_Main : Mod
	{
		public Mattworld_Main(ModContentPack content) : base(content)
		{
			var harmony = new Harmony("net.pardeike.mattworld");
			harmony.PatchAll();

			CrossPromotion.Install(76561197973010050);
		}
	}

	[HarmonyPatch(typeof(Rand), nameof(Rand.Seed), MethodType.Setter)]
	static class Rand_Seed_Patch
	{
		public static bool Prefix()
		{
			Rand.seed = (uint)(DateTime.Now.Year * 100 + DateTime.Now.Month);
			Rand.iterations = 0U;
			return false;
		}
	}

	[HarmonyPatch(typeof(TileFinder), nameof(TileFinder.RandomStartingTile))]
	static class TileFinder_RandomStartingTile_Patch
	{
		public static bool Prefix(ref int __result)
		{
			__result = TileFinder.RandomSettlementTileFor(Faction.OfPlayer, true, x => 
			{
				var tile = Find.WorldGrid[x];
				if (tile.hilliness < Hilliness.LargeHills)
					return false;
				var t = tile.temperature;
				return t >= -10 && (t <= 5 || t >= 30);
			});
			return false;
		}
	}

	[HarmonyPatch(typeof(TutorSystem), nameof(TutorSystem.AllowAction))]
	static class TutorSystem_AllowAction_Patch
	{
		public static bool Prefix(EventPack ep, ref bool __result)
		{
			if (ep.Tag != "ChooseStoryteller")
				return true;
			__result = false;
			return false;
		}
	}

	[HarmonyPatch]
	static class MainMenuDrawer_NewColony_Patch
	{
		const float planetCoverage = 1f;

		public static MethodBase TargetMethod()
		{
			static bool CreatesPageSelectScenario(KeyValuePair<OpCode, object> pair) => pair.Key == OpCodes.Newobj && pair.Value is ConstructorInfo ctor && ctor.DeclaringType == typeof(Page_SelectScenario);
			var type = AccessTools.Inner(typeof(MainMenuDrawer), "<>c");
			return AccessTools.FirstMethod(type, method => method.Name.StartsWith("<DoMainMenuControls>") && PatchProcessor.ReadMethodBody(method).Any(CreatesPageSelectScenario));
		}

		public static bool Prefix()
		{
			LongEventHandler.QueueLongEvent(() =>
			{
				Rand.Seed = 0;

				Current.ProgramState = ProgramState.Entry;
				Current.Game = new Game
				{
					InitData = new GameInitData(),
					Scenario = DefDatabase<ScenarioDef>.GetNamed("NakedBrutality").scenario
				};
				Find.Scenario.PreConfigure();
				var storytellerDef = DefDatabase<StorytellerDef>.GetNamed("Randy");
				var difficultyDef = DefDatabase<DifficultyDef>.GetNamed("Custom");
				var difficulty = new Difficulty(difficultyDef) { threatScale = 5f };
				Current.Game.storyteller = new Storyteller(storytellerDef, difficultyDef, difficulty);
				Current.Game.World = WorldGenerator.GenerateWorld(planetCoverage, $"Matt-{DateTime.Now.Year}-{DateTime.Now.Month}", OverallRainfall.Normal, OverallTemperature.Normal, OverallPopulation.Normal, null, 0f);
				Find.GameInitData.ChooseRandomStartingTile();
				Find.GameInitData.permadeathChosen = true;
				Find.GameInitData.permadeath = true;
				Find.GameInitData.mapSize = 250;

				if (ModsConfig.IdeologyActive)
				{
					var me = LoadedModManager.GetMod<Mattworld_Main>();
					var path = Path.Combine(me.Content.RootDir, "Resources", "Edopeh.rid");
					GameDataSaveLoader.TryLoadIdeo(path, out var ideo);

					Faction.OfPlayer.ideos.SetPrimary(ideo);
					foreach (Ideo ideo2 in Find.IdeoManager.IdeosListForReading)
						ideo2.initialPlayerIdeo = false;
					ideo.initialPlayerIdeo = true;
					Find.IdeoManager.Add(ideo);

					Find.IdeoManager.RemoveUnusedStartingIdeos();
					Find.Scenario.PostIdeoChosen();
				}

				var pawn = Find.GameInitData.startingAndOptionalPawns.First();
				while (pawn.gender != Gender.Male)
				{
					SpouseRelationUtility.Notify_PawnRegenerated(pawn);
					pawn = StartingPawnUtility.RandomizeInPlace(pawn);
				}
				foreach (var skill in pawn.skills.skills)
				{
					skill.Level /= 2;
					skill.passion = Passion.None;
				}

				var nameTriple = pawn.Name as NameTriple;
				pawn.Name = new NameTriple(nameTriple.First, "Matt", nameTriple.Last);

				Find.GameInitData.PrepForMapGen();
				Find.Scenario.PreMapGenerate();

				PageUtility.InitGameStart();

			}, "GeneratingMap", true, new Action<Exception>(GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap), true);
			return false;
		}
	}

	[HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GenerateTraitsFor))]
	class PawnGenerator_GenerateTraits_Patch
	{
		const float scaleFactor = 0.35f;
		const float goodTraitSuppression = 1f;
		const float badTraitSuppression = 0.25f;

		static bool IsDisplayClassConstructor(CodeInstruction c) => c.opcode == Newobj.opcode && c.operand is ConstructorInfo constructor && constructor.DeclaringType.Name.StartsWith("<>c__DisplayClass");

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen)
		{
			var matcher = new CodeMatcher(instructions, gen)
				.MatchStartForward(
					new CodeMatch(IsDisplayClassConstructor),
					Stloc[name: "displayclass"]
				);

			var local_displayclass = matcher.NamedMatch("displayclass").operand as LocalBuilder;

			matcher = matcher.MatchEndForward(
					new CodeMatch(c => c.IsLdloc(), "weightSelector"),
					new CodeMatch(operand: SymbolExtensions.GetMethodInfo(() => GenCollection.RandomElementByWeight<TraitDef>(default, default))),
					Stfld[name: "newTraitDef"] // we will start inserting just before this code
				);

			var local_weightSelector = matcher.NamedMatch("weightSelector").operand as LocalBuilder;
			var field_newTraitDef = matcher.NamedMatch("newTraitDef").operand as FieldInfo;

			var local_TraitScore = gen.DeclareLocal(typeof(TraitScore));

			matcher = matcher
				.Insert(
					Ldarg_0,
					Ldloc[operand: local_weightSelector],
					CodeInstruction.CallClosure((Pawn pawn, TraitDef originalDef, Func<TraitDef, float> weightFunction) =>
					{
						if (pawn.IsHostileToPlayer())
						{
							var newTraitDef = DefDatabase<TraitDef>.AllDefsListForReading.RandomElementByWeight(weightFunction);
							return new TraitScore(newTraitDef, PawnGenerator.RandomTraitDegree(newTraitDef));
						}

						var s = 20 * scaleFactor;
						var a = s * badTraitSuppression;
						var b = s * goodTraitSuppression;
						return Tools.sortedTraits.RandomElementByWeight(ts =>
						{
							var x = ts.badScore;
							return weightFunction(ts.def) * Mathf.Min(1f / (a * x + 1f), 1f / (b * (1f - x) + 1f));
						});
					}),
					Dup,
					Stloc[operand: local_TraitScore],
					Ldfld[operand: AccessTools.DeclaredField(typeof(TraitScore), nameof(TraitScore.def))]
				)
				.MatchStartForward(
					Ldloc[operand: local_displayclass, name: "start"],
					Ldfld[operand: field_newTraitDef],
					new CodeMatch(operand: SymbolExtensions.GetMethodInfo(() => PawnGenerator.RandomTraitDegree(default)))
				);

			var labels = matcher.NamedMatch("start").labels.ToArray();

			return matcher
				.RemoveInstructions(3)
				.Insert(
					Ldloc[operand: local_TraitScore].WithLabels(labels),
					Ldfld[operand: AccessTools.DeclaredField(typeof(TraitScore), nameof(TraitScore.degree))]
				)
				.InstructionEnumeration();
		}
	}

	[HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GenerateSkills))]
	class PawnGenerator_GenerateSkills_Patch
	{
		public static void Postfix(Pawn pawn)
		{
			if (pawn.IsHostileToPlayer() == false)
			{
				static void Limit(SkillRecord record)
				{
					record.Level /= 2;
					if (record.passion > Passion.Major)
						record.passion = Passion.Minor;
				}

				Limit(pawn.skills.GetSkill(SkillDefOf.Melee));
				Limit(pawn.skills.GetSkill(SkillDefOf.Shooting));
			}
		}
	}
}
using RimWorld;
using Verse;

namespace Mattworld
{
	[DefOf]
	[StaticConstructorOnStartup]
	public static class Defs
	{
		public static TraitDef Beauty;
		public static TraitDef Cannibal;
		public static TraitDef FastLearner;
		public static TraitDef Gourmand;
		public static TraitDef Immunity;
		public static TraitDef Masochist;
		public static TraitDef NaturalMood;
		public static TraitDef Nerves;
		public static TraitDef Neurotic;
		public static TraitDef NightOwl;
		public static TraitDef Nimble;
		public static TraitDef PsychicSensitivity;
		public static TraitDef QuickSleeper;
		public static TraitDef ShootingAccuracy;
		public static TraitDef SlowLearner;
		public static TraitDef SpeedOffset;
		public static TraitDef TooSmart;
		public static TraitDef TorturedArtist;
		public static TraitDef Tough;
	}

	struct TraitScore(TraitDef def, int degree = 0)
	{
		static int scoreIndex = 0;

		public float badScore = scoreIndex++ / (float)Tools.traitCount;
		public TraitDef def = def;
		public int degree = degree;
	}

	static class Tools
	{
		public static int traitCount = 65;

		public static TraitScore[] sortedTraits =
		[
			new TraitScore(Defs.Tough),
			new TraitScore(Defs.FastLearner),
			new TraitScore(TraitDefOf.Industriousness, 2), // Industrious
			new TraitScore(TraitDefOf.Industriousness, 1), // Hard Worker
			new TraitScore(Defs.SpeedOffset, 2), // Jogger
			new TraitScore(Defs.NaturalMood, 2), // Sanguine
			new TraitScore(Defs.NaturalMood, 1), // Optimist
			new TraitScore(Defs.Immunity, 1), // Super-immune
			new TraitScore(Defs.Nerves, 2), // Iron Willed
			new TraitScore(Defs.QuickSleeper),
			new TraitScore(Defs.Nerves, 1), // Steadfast
			new TraitScore(TraitDefOf.Kind),
			new TraitScore(TraitDefOf.Psychopath),
			new TraitScore(Defs.Nimble),
			new TraitScore(Defs.SpeedOffset, 1), // Fast Walker
			new TraitScore(TraitDefOf.GreatMemory),
			new TraitScore(Defs.Beauty, 2), // Beautiful
			new TraitScore(TraitDefOf.Bloodlust),
			new TraitScore(Defs.Masochist),
			new TraitScore(Defs.Beauty, 1), // Pretty
			new TraitScore(TraitDefOf.Transhumanist),
			new TraitScore(Defs.Cannibal),
			new TraitScore(Defs.ShootingAccuracy, -1), // Trigger Happy
			new TraitScore(TraitDefOf.Ascetic),
			new TraitScore(Defs.TooSmart),
			new TraitScore(TraitDefOf.Brawler),
			new TraitScore(Defs.ShootingAccuracy, 1), // Careful Shooter
			new TraitScore(TraitDefOf.Undergrounder),
			new TraitScore(Defs.NightOwl),
			new TraitScore(TraitDefOf.Bisexual),
			new TraitScore(Defs.PsychicSensitivity, 2), // Psychically Hypersensitive
			new TraitScore(Defs.PsychicSensitivity, 1), // Psychically Sensitive
			new TraitScore(Defs.PsychicSensitivity, -2), // Psychically Deaf
			new TraitScore(Defs.PsychicSensitivity, -1), // Psychically Dull
			new TraitScore(TraitDefOf.Gay),
			new TraitScore(Defs.TorturedArtist),
			new TraitScore(TraitDefOf.Asexual),
			new TraitScore(Defs.Neurotic, 1), // Neurotic
			new TraitScore(Defs.Neurotic, 2), // Very Neurotic
			new TraitScore(TraitDefOf.DrugDesire, -1), // Teetotaler
			new TraitScore(Defs.Gourmand),
			new TraitScore(TraitDefOf.Nudist),
			new TraitScore(Defs.Nerves, -1), // Nervous
			new TraitScore(Defs.Beauty, -1), // Ugly
			new TraitScore(TraitDefOf.Abrasive),
			new TraitScore(TraitDefOf.DrugDesire, 1), // Chemical Interest
			new TraitScore(Defs.NaturalMood, -1), // Pessimist
			new TraitScore(Defs.Immunity, -1), // Sickly
			new TraitScore(TraitDefOf.Industriousness, -1), // Lazy
			new TraitScore(TraitDefOf.AnnoyingVoice),
			new TraitScore(TraitDefOf.DislikesMen), // Misandrist
			new TraitScore(Defs.Nerves, -2), // Volatile
			new TraitScore(Defs.SpeedOffset, -1), // Slowpoke
			new TraitScore(TraitDefOf.DrugDesire, 2), // Chemical Fascination
			new TraitScore(TraitDefOf.BodyPurist),
			new TraitScore(TraitDefOf.DislikesWomen), // Misogynist
			new TraitScore(TraitDefOf.CreepyBreathing),
			new TraitScore(Defs.Beauty, -2), // Staggeringly Ugly
			new TraitScore(TraitDefOf.Greedy),
			new TraitScore(TraitDefOf.Wimp),
			new TraitScore(Defs.SlowLearner),
			new TraitScore(TraitDefOf.Industriousness, -2), // Slothful
			new TraitScore(TraitDefOf.Jealous),
			new TraitScore(Defs.NaturalMood, -2), // Depressive
			new TraitScore(TraitDefOf.Pyromaniac),
		];

		public static bool IsHostileToPlayer(this Pawn pawn)
		{
			var playerFaction = Faction.OfPlayerSilentFail;
			if (playerFaction == null)
				return false;
			return pawn.HostileTo(playerFaction);
		}
	}
}

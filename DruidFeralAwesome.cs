using System;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;
using ReBot.API;

namespace ReBot
{
    // WoD Patch 6.0.2
	[Rotation("Druid Feral - JT Mod v1.0.3", "JT", WoWClass.Druid, Specialization.DruidFeral, 5, 25)]
	public class DruidFeral : CombatRotation
	{
		// This is important, some mobs can't get the rake debuff. If this is missing the bot would always try rake...
		AutoResetDelay rakeDelay = new AutoResetDelay(7000);

        [JsonProperty("Instant Self Heal"), Description("If below this health fraction and has aura Predatory Swiftness, self heal will be cast")]
        public double PredatorySwiftnessHeal = 0.7;

        [JsonProperty("Incarnation Auto-Prowl"), Description("If below this health fraction and has aura Incarnation, will cast prowl.")]
        public double IncarnationAutoProwl = 0.19;

        [JsonProperty("Guardian Orb Cast Health"), Description("If below this health fraction, and in Talador, will cast Guardian Orb")]
        public double GuardianOrbHealthFraction = 0.5;

		public DruidFeral()
		{
			GroupBuffs = new[]
			{
				"Mark of the Wild"
			};
			PullSpells = new[]
			{
				"Moonfire"
			};
		}

		public override bool OutOfCombat()
		{
			if (CastSelf("Rejuvenation", () => Me.HealthFraction <= 0.75 && !HasAura("Rejuvenation"))) return true;
            if (CastSelfPreventDouble("Healing Touch", () => Me.HealthFraction <= 0.5 || Me.HealthFraction <= PredatorySwiftnessHeal && HasAura("Predatory Swiftness"))) return true;
			if (CastSelf("Remove Corruption", () => Me.Auras.Any(x => x.IsDebuff && (x.DebuffType == DispellType.Curse || x.DebuffType == DispellType.Poison))))
				return true;

			if (CastSelf("Mark of the Wild", () => !HasAura("Mark of the Wild") && !HasAura("Blessing of Kings"))) return true;
            if (CastSelf("Cat Form", () => Me.MovementSpeed != 0 && !Me.IsSwimming && Me.DisplayId == Me.NativeDisplayId && Me.DistanceTo(API.GetNaviTarget()) > 20)) return true;
		    return false;
		}

		public override void Combat()
		{
            // If energy <= 20 cast berserk


            if (Cast("Mighty Bash", () => Target.CanParticipateInCombat && Target.IsCastingAndInterruptible())) return;
            if (CastSelfPreventDouble("Healing Touch", () => Me.HealthFraction <= PredatorySwiftnessHeal && HasAura("Predatory Swiftness"))) return;

			if (HasAura("Cat Form") || HasAura("Claws of Shirvallah"))
			{
                // Always do these
                Cast("Skull Bash", () => Target.IsCastingAndInterruptible());
                if (HasAura("Incarnation: King of the Jungle") && Me.HealthFraction <= IncarnationAutoProwl && Cast("Prowl")) return;

                if (Cast("Incarnation: King of the Jungle", () => Target.IsElite() || Adds.Count > 1 && Me.HealthFraction <= 0.70
                    && !HasAura("Incarnation: King of the Jungle")))
                    return;

                if (Cast("Survival Instincts", () => Target.IsElite() || Adds.Count > 1 && Me.HealthFraction <= 0.70
                    && !HasAura("Survival Instincts")))
                    return;

                if (Cast("Guardian Orb", () => Target.IsElite() || Adds.Count > 1 && Me.HealthFraction <= GuardianOrbHealthFraction)) return;

                if (Cast("Berserk", () => Me.GetPower(WoWPowerType.Energy) <= 20)) return;

                DoSingleTargetRotation();
            }
            else
			{
				if (CastSelf("Rejuvenation", () => Me.HealthFraction <= 0.75 && !HasAura("Rejuvenation"))) return;
				if (Cast("Moonfire", () => Target.HealthFraction <= 0.1 && !Target.IsElite())) return;
				if (CastSelf("Cat Form", () => Target.IsInCombatRangeAndLoS || Target.CombatRange <= 25)) return;
			}
		}

        private void DoSingleTargetRotation()
        {
            if (Cast("Faerie Swarm", () => Target.IsPlayer && Target.Class == WoWClass.Rogue && !Target.HasAura("Faerie Swarm"))) return;

            if (CastSelf("Rejuvenation", () => Me.HealthFraction <= 0.5 && !HasAura("Rejuvenation"))) return;

            if (CastSelf("Tiger's Fury", () => !HasAura("Berserk")))
                CastSelf("Berserk", () => Me.HpLessThanOrElite(0.6));

            if (Cast("Wild Charge", () => Target.HealthFraction > 0.25 || Me.HealthFraction < 0.4 || Target.IsElite() || Target.IsCastingAndInterruptible())) return;
            if (Cast("Typhoon", () => Target.IsInCombatRange && Me.HealthFraction < 0.5)) return;

            if (Cast("Savage Roar", () => Me.GetPower(WoWPowerType.Energy) >= 25 && !HasAura("Savage Roar") && (Me.ComboPoints >= 3))) return;
            if (Cast("Ferocious Bite", () => Me.GetPower(WoWPowerType.Energy) >= 25 && Me.ComboPoints >= 3 && Target.MaxHealth <= Me.MaxHealth)) return;
            if (Cast("Rip", () => Me.GetPower(WoWPowerType.Energy) >= 30 && !Target.HasAura("Rip", true) && Me.ComboPoints >= 3)) return;
            if (Cast("Ferocious Bite", () => Me.GetPower(WoWPowerType.Energy) >= 25 && Me.ComboPoints >= 3)) return;

            if (Cast("Thrash", () => HasAura("Clearcasting") && !Target.HasAura("Thrash", true))) return;

            if (Cast("Rake", () => Me.GetPower(WoWPowerType.Energy) >= 35 && !Target.HasAura("Rake", true) && rakeDelay.IsReady)) return;

            if (Adds.Count > 2 && Adds.Count(x => x.DistanceSquared < 8 * 8) > 2)
            {
                if (Cast("Swipe", () => Me.GetPower(WoWPowerType.Energy) >= 45 || HasAura("Clearcasting"))) return;
                if (Adds.Count > 10 && Adds.Count(x => x.DistanceSquared < 8 * 8) > 10)
                    return; // only do swipe

            }
            if (Cast("Shred", () => Me.GetPower(WoWPowerType.Energy) >= 40 || HasAura("Clearcasting"))) return;
        }
	}
}